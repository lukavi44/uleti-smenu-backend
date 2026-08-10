using Core.Models.Entities;
using Core.Models.Enums;
using Core.Services;
using CSharpFunctionalExtensions;
using Infrastructure.Persistence.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Services
{
    public class AccountDeletionService : IAccountDeletionService
    {
        public const string RedactedMessageContent = "[deleted]";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IFileService _fileService;
        private readonly ILogger<AccountDeletionService> _logger;

        public AccountDeletionService(
            ApplicationDbContext context,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IFileService fileService,
            ILogger<AccountDeletionService> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _fileService = fileService;
            _logger = logger;
        }

        public async Task<Result> DeleteMyAccountAsync(
            Guid userId,
            string password,
            CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
                return Result.Failure("User is required.");

            if (string.IsNullOrWhiteSpace(password))
                return Result.Failure("Password is required.");

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Result.Failure("User not found.");

            if (user.IsDeleted)
                return Result.Success();

            var passwordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!passwordValid)
                return Result.Failure("Incorrect password.");

            var originalEmail = user.Email;
            var profilePhoto = user.ProfilePhoto;

            IDbContextTransaction? transaction = null;
            if (_context.Database.IsRelational())
                transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (user is Employee employee)
                    await DeleteEmployeePersonalDataAsync(employee, cancellationToken);
                else if (user is Employer employer)
                    await DeleteEmployerPersonalDataAsync(employer, cancellationToken);
                else
                    await DeleteSharedPersonalDataAsync(userId, cancellationToken);

                await AnonymizeContactMessagesAsync(originalEmail, cancellationToken);
                await RedactChatMessagesAsync(userId, cancellationToken);

                var utcNow = DateTime.UtcNow;
                if (user is Employee emp)
                    emp.AnonymizePersonalProfile();
                else if (user is Employer empl)
                    empl.AnonymizePublicProfileForDeletion();

                user.MarkDeletedTombstone(utcNow);

                await RevokeIdentityAccessAsync(user);

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var error = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                    if (transaction != null)
                        await transaction.RollbackAsync(cancellationToken);
                    return Result.Failure(string.IsNullOrWhiteSpace(error) ? "Could not finalize account deletion." : error);
                }

                await _context.SaveChangesAsync(cancellationToken);
                if (transaction != null)
                    await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Account deletion failed for user {UserId}", userId);
                if (transaction != null)
                    await transaction.RollbackAsync(cancellationToken);
                return Result.Failure("Account deletion failed. Please try again.");
            }
            finally
            {
                if (transaction != null)
                    await transaction.DisposeAsync();
            }

            try
            {
                await _signInManager.SignOutAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sign-out after account deletion failed for user {UserId}", userId);
            }

            if (!string.IsNullOrWhiteSpace(profilePhoto))
            {
                try
                {
                    await _fileService.DeleteImageAsync(profilePhoto, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Profile photo cleanup failed for deleted user {UserId}", userId);
                }
            }

            return Result.Success();
        }

        private async Task DeleteEmployeePersonalDataAsync(Employee employee, CancellationToken cancellationToken)
        {
            await DeleteSharedPersonalDataAsync(employee.Id, cancellationToken);

            var favourites = await _context.Favourites
                .Where(favourite => favourite.EmployeeId == employee.Id)
                .ToListAsync(cancellationToken);
            _context.Favourites.RemoveRange(favourites);

            var workExperiences = await _context.WorkExperiences
                .Where(experience => experience.EmployeeId == employee.Id)
                .ToListAsync(cancellationToken);
            _context.WorkExperiences.RemoveRange(workExperiences);

            var removableApplications = await _context.Applications
                .Where(application =>
                    application.UserId == employee.Id &&
                    (application.Status == ApplicationStatusEnum.Applied ||
                     application.Status == ApplicationStatusEnum.Cancelled))
                .ToListAsync(cancellationToken);
            _context.Applications.RemoveRange(removableApplications);
        }

        private async Task DeleteEmployerPersonalDataAsync(Employer employer, CancellationToken cancellationToken)
        {
            await DeleteSharedPersonalDataAsync(employer.Id, cancellationToken);

            var favourites = await _context.Favourites
                .Where(favourite => favourite.EmployerId == employer.Id)
                .ToListAsync(cancellationToken);
            _context.Favourites.RemoveRange(favourites);

            var activePosts = await _context.JobPosts
                .Where(post =>
                    post.EmployerId == employer.Id &&
                    post.Status != JobStatusEnum.Cancelled &&
                    post.Status != JobStatusEnum.Completed &&
                    post.Status != JobStatusEnum.Expired)
                .ToListAsync(cancellationToken);

            foreach (var post in activePosts)
            {
                post.Archive();
                var pending = await _context.Applications
                    .Where(application =>
                        application.JobPostId == post.Id &&
                        application.Status == ApplicationStatusEnum.Applied)
                    .ToListAsync(cancellationToken);
                foreach (var application in pending)
                    application.ExpireDueToInactiveJobPost();
            }

            var locations = await _context.RestaurantLocations
                .Where(location => location.EmployerId == employer.Id)
                .ToListAsync(cancellationToken);
            foreach (var location in locations)
                location.ClearPhoneForDeletion();

            // WalletTransactions / PaymentEvents / Stripe IDs retained for lawyer/accountant review.
        }

        private async Task DeleteSharedPersonalDataAsync(Guid userId, CancellationToken cancellationToken)
        {
            var notifications = await _context.Notifications
                .Where(notification => notification.UserId == userId)
                .ToListAsync(cancellationToken);
            _context.Notifications.RemoveRange(notifications);

            var readStates = await _context.ConversationReadStates
                .Where(state => state.UserId == userId)
                .ToListAsync(cancellationToken);
            _context.ConversationReadStates.RemoveRange(readStates);
        }

        private async Task AnonymizeContactMessagesAsync(string? originalEmail, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(originalEmail))
                return;

            var messages = await _context.ContactMessages
                .Where(message => message.Email == originalEmail)
                .ToListAsync(cancellationToken);

            foreach (var message in messages)
                message.AnonymizeSenderPii();
        }

        private async Task RedactChatMessagesAsync(Guid userId, CancellationToken cancellationToken)
        {
            var messages = await _context.ChatMessages
                .Where(message => message.SenderId == userId)
                .ToListAsync(cancellationToken);

            foreach (var message in messages)
                message.RedactContent();
        }

        private async Task RevokeIdentityAccessAsync(User user)
        {
            await _userManager.UpdateSecurityStampAsync(user);
            await _userManager.RemovePasswordAsync(user);
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

            var logins = await _userManager.GetLoginsAsync(user);
            foreach (var login in logins)
                await _userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Count > 0)
                await _userManager.RemoveFromRolesAsync(user, roles);

            var tokens = _context.Set<IdentityUserToken<Guid>>()
                .Where(token => token.UserId == user.Id);
            _context.Set<IdentityUserToken<Guid>>().RemoveRange(tokens);

            var claims = _context.Set<IdentityUserClaim<Guid>>()
                .Where(claim => claim.UserId == user.Id);
            _context.Set<IdentityUserClaim<Guid>>().RemoveRange(claims);
        }
    }
}

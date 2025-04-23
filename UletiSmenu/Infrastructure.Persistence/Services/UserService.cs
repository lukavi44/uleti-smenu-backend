using Core.Interfaces;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Core.Services;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IUserRepository _userRepository;
        private readonly IApplicationUnitOfWork _applicationUnitOfWork;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, UserManager<User> userManager, SignInManager<User> signInManager,
            IApplicationUnitOfWork applicationUnitOfWork, IEmailService emailService, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationUnitOfWork = applicationUnitOfWork;
            _emailService = emailService;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<Result> RegisterEmployerAsync(Employer employer, string password)
        {
            await _applicationUnitOfWork.BeginTransactionAsync();

            try
            {
                var existingUser = await _userRepository.FindAsync(e => e.Email == employer.Email);
                if (existingUser.Any())
                {
                    _logger.LogWarning($"Attempted to register user with existing email: {employer.Email}");
                    return Result.Failure("Email already exists");
                }

                var identityResult = await _userManager.CreateAsync(employer, password);
                if (!identityResult.Succeeded)
                    return Result.Failure(string.Join(", ", identityResult.Errors.Select(e => e.Description)));

                var roleResult = await _userManager.AddToRoleAsync(employer, UserRolesEnum.Employer.ToString());
                if (!roleResult.Succeeded)
                    return Result.Failure("User created but failed to assign role: " + string.Join(", ", roleResult.Errors.Select(e => e.Description)));

                //var token = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);

                //var confirmationLink = $"{_configuration["Backend:BaseUrl"]}/api/auth/confirm-email?userId={newUser.Id}&token={WebUtility.UrlEncode(token)}";

                //await _emailService.SendEmailAsync(newUser.Email, "Confirm Your Email",
                //    $"Click <a href='{confirmationLink}'>here</a> to confirm your email.");

                await _applicationUnitOfWork.SaveChangesAsync();
                await _applicationUnitOfWork.CommitTransactionAsync();

                return Result.Success("User registered successfully! Please check your email for confirmation.");
            }
            catch (Exception ex)
            {
                await _applicationUnitOfWork.RollbackTransactionAsync();
                return Result.Failure($"Employer registration failed: {ex.Message}");
            }
        }

        public Task<User> CreateUserAsync(User user)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteUserAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllAsync();
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        public Task<Result> RegisterEmployeeAsync(Employee user, string password)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateUserAsync(Guid id, User updatedUser)
        {
            throw new NotImplementedException();
        }

        //public async Task<Result> LoginUserAsync(string email, string password)
        //{
        //    var user = await _userManager.FindByEmailAsync(email);
        //    if (user == null)
        //        return Result.Failure("Invalid email or password.");

        //    var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: false);

        //    if (!result.Succeeded)
        //        return Result.Failure("Invalid email or password.");

        //    return Result.Success("User logged in successfully.");
        //}

        public async Task<Result> LogoutUserAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return Result.Failure("HttpContext not available.");

            await _signInManager.SignOutAsync();
            httpContext.Response.Cookies.Delete(".AspNetCore.Identity.Application");

            return Result.Success("User logged out successfully.");
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(string userRole)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(userRole);
            return usersInRole;
        }

        public async Task<bool> ConfirmEmailAsync(Guid userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            return result.Succeeded;
        }

        public Task<Result> UpdateEmployeeAsync(Guid employeeId, Employee employee)
        {
            throw new NotImplementedException();
        }

        public Task<Result> UpdateEmployerAsync(Guid employerId, Employer employer)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Employer>> GetAllEmployersAsync()
        {
            return await _userRepository.GetAllEmployersAsync();
        }

        public Task<Employer> GetEmployerByCityAsync(string city)
        {
            throw new NotImplementedException();
        }

        public Task<Employer> GetEmployerByNameAsync(string name)
        {
            throw new NotImplementedException();
        }
    }
}

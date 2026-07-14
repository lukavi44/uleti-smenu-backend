using Core.DTOs;
using Core.Helpers;
using Core.Interfaces;
using Core.Models;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Core.Services;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Infrastructure.Persistence.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IUserRepository _userRepository;
        private readonly IRestaurantLocationRepository _restaurantLocationRepository;
        private readonly IJobPostRepository _jobPostRepository;
        private readonly IApplicationUnitOfWork _applicationUnitOfWork;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IBillingService _billingService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, IRestaurantLocationRepository restaurantLocationRepository, IJobPostRepository jobPostRepository, UserManager<User> userManager, SignInManager<User> signInManager,
            IApplicationUnitOfWork applicationUnitOfWork, IEmailService emailService, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IBillingService billingService, IServiceScopeFactory serviceScopeFactory, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _restaurantLocationRepository = restaurantLocationRepository;
            _jobPostRepository = jobPostRepository;
            _userManager = userManager;
            _signInManager = signInManager;
            _billingService = billingService;
            _applicationUnitOfWork = applicationUnitOfWork;
            _emailService = emailService;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _serviceScopeFactory = serviceScopeFactory;
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

                var bonusResult = _billingService.GrantRegistrationBonus(employer);
                if (bonusResult.IsFailure)
                    return Result.Failure(bonusResult.Error);

                var slugResult = await AssignUniquePublicSlugAsync(employer);
                if (slugResult.IsFailure)
                    return Result.Failure(slugResult.Error);

                var identityResult = await _userManager.CreateAsync(employer, password);
                if (!identityResult.Succeeded)
                    return Result.Failure(string.Join(", ", identityResult.Errors.Select(e => e.Description)));

                var roleResult = await _userManager.AddToRoleAsync(employer, UserRolesEnum.Employer.ToString());
                if (!roleResult.Succeeded)
                    return Result.Failure("User created but failed to assign role: " + string.Join(", ", roleResult.Errors.Select(e => e.Description)));

                await _applicationUnitOfWork.SaveChangesAsync();
                await _applicationUnitOfWork.CommitTransactionAsync();

                QueueConfirmationEmail(employer.Id, employer.Email!);

                return Result.Success("User registered successfully! Please check your email for confirmation.");
            }
            catch (Exception ex)
            {
                await _applicationUnitOfWork.RollbackTransactionAsync();
                return Result.Failure($"Employer registration failed: {ex.Message}");
            }
        }

        public async Task<Result> RegisterEmployeeAsync(Employee employee, string password)
        {
            await _applicationUnitOfWork.BeginTransactionAsync();

            try
            {
                var existingUser = await _userRepository.FindAsync(e => e.Email == employee.Email);
                if (existingUser.Any())
                {
                    _logger.LogWarning($"Attempted to register user with existing email: {employee.Email}");
                    return Result.Failure("Email already exists");
                }

                var identityResult = await _userManager.CreateAsync(employee, password);
                if (!identityResult.Succeeded)
                    return Result.Failure(string.Join(", ", identityResult.Errors.Select(e => e.Description)));

                var roleResult = await _userManager.AddToRoleAsync(employee, UserRolesEnum.Employee.ToString());
                if (!roleResult.Succeeded)
                    return Result.Failure("User created but failed to assign role: " + string.Join(", ", roleResult.Errors.Select(e => e.Description)));

                await _applicationUnitOfWork.SaveChangesAsync();
                await _applicationUnitOfWork.CommitTransactionAsync();

                QueueConfirmationEmail(employee.Id, employee.Email!);

                return Result.Success("User registered successfully! Please check your email for confirmation.");
            }
            catch (Exception ex)
            {
                await _applicationUnitOfWork.RollbackTransactionAsync();
                return Result.Failure($"Employee registration failed: {ex.Message}");
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

        public async Task<Result<Employee>> UpdateEmployeeProfileAsync(
            Guid employeeId,
            string firstName,
            string lastName,
            string phoneNumber,
            string? city)
        {
            var employee = await _userRepository.GetByIdAsync<Employee>(employeeId);
            if (employee == null)
                return Result.Failure<Employee>("Employee not found.");

            var updateResult = employee.UpdateProfile(firstName, lastName, phoneNumber, city);
            if (updateResult.IsFailure)
                return Result.Failure<Employee>(updateResult.Error);

            var identityResult = await _userManager.UpdateAsync(employee);
            if (!identityResult.Succeeded)
            {
                return Result.Failure<Employee>(
                    string.Join(", ", identityResult.Errors.Select(error => error.Description)));
            }

            return Result.Success(employee);
        }

        public Task<Result> UpdateEmployerAsync(Guid employerId, Employer employer)
        {
            throw new NotImplementedException();
        }

        public async Task<Result<Employer>> UpdateEmployerProfileAsync(
            Guid employerId,
            string name,
            string phoneNumber,
            string pib,
            string mb,
            string streetName,
            string streetNumber,
            string city,
            string postalCode,
            string country,
            string region)
        {
            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);
            if (employer == null)
                return Result.Failure<Employer>("Employer not found.");

            var updateResult = employer.UpdateProfile(
                name,
                phoneNumber,
                pib,
                mb,
                streetName,
                streetNumber,
                city,
                postalCode,
                country,
                region);

            if (updateResult.IsFailure)
                return Result.Failure<Employer>(updateResult.Error);

            var identityResult = await _userManager.UpdateAsync(employer);
            if (!identityResult.Succeeded)
            {
                return Result.Failure<Employer>(
                    string.Join(", ", identityResult.Errors.Select(error => error.Description)));
            }

            return Result.Success(employer);
        }

        public async Task<List<string>> GetEmployerCitiesAsync()
        {
            var branchCities = await _restaurantLocationRepository.GetDistinctCitiesAsync();
            var employers = await _userRepository.GetAllEmployersAsync();
            var registrationCities = employers
                .Select(employer => employer.Address?.City?.Name)
                .Where(city => !string.IsNullOrWhiteSpace(city))
                .Select(city => city!);

            return branchCities
                .Concat(registrationCities)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(city => city, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<IEnumerable<Employer>> GetEmployersAsync(string? city = null)
        {
            var employers = (await FilterEmployersByCityAsync(await _userRepository.GetAllEmployersAsync(), city)).ToList();

            foreach (var employer in employers)
            {
                await EnsurePublicSlugAsync(employer);
            }

            return employers;
        }

        public async Task<IEnumerable<Employer>> GetAllEmployersAsync(string? city = null)
        {
            return await GetEmployersAsync(city);
        }

        public Task<Employer> GetEmployerByCityAsync(string city)
        {
            throw new NotImplementedException();
        }

        public Task<Employer> GetEmployerByNameAsync(string name)
        {
            throw new NotImplementedException();
        }

        public async Task<string?> GetUserRoleAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault();
        }

        public async Task<Result> ToggleFavouriteEmployerAsync(Guid employeeId, Guid employerId)
        {
            if (employeeId == employerId)
                return Result.Failure("You cannot favourite yourself.");

            var employee = await _userRepository.GetByIdAsync<Employee>(employeeId);
            if (employee == null)
                return Result.Failure("Employee not found.");

            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);
            if (employer == null)
                return Result.Failure("Employer not found.");

            var maybeExisting = await _applicationUnitOfWork.Favourites.GetByIdAsync(employeeId, employerId);

            if (maybeExisting.HasValue)
            {
                await _applicationUnitOfWork.Favourites.RemoveAsync(maybeExisting.Value);
            }
            else
            {
                var favourite = Favourite.Create(employee, employer).Value;
                await _applicationUnitOfWork.Favourites.AddAsync(favourite);
            }

            await _applicationUnitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<IEnumerable<EmployerFavouriteStatusDTO>> GetAllEmployersWithFavouriteStatusAsync(Guid employeeId, string? city = null)
        {
            var favouritedEmployerIds = await _applicationUnitOfWork.Favourites
                .GetEmployerIdsFavouritedByEmployeeAsync(employeeId);

            var employers = await GetEmployersAsync(city);

            return employers.Select(e => new EmployerFavouriteStatusDTO
            {
                EmployerId = e.Id,
                Name = e.Name,
                ProfilePhoto = e.ProfilePhoto,
                PublicSlug = e.PublicSlug,
                IsFavourite = favouritedEmployerIds.Contains(e.Id)
            }).ToList();
        }

        private async Task<IEnumerable<Employer>> FilterEmployersByCityAsync(IEnumerable<Employer> employers, string? city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return employers;

            var normalizedCity = city.Trim();
            var employerIdsWithBranch = (await _restaurantLocationRepository.GetEmployerIdsByCityAsync(normalizedCity)).ToHashSet();

            return employers.Where(employer =>
                employerIdsWithBranch.Contains(employer.Id) ||
                string.Equals(employer.Address?.City?.Name, normalizedCity, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<Result<RestaurantLocation>> CreateEmployerLocationAsync(
            Guid employerId,
            string name,
            string phoneNumber,
            string pib,
            string mb,
            string streetName,
            string streetNumber,
            string city,
            string postalCode,
            string country,
            string region)
        {
            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);
            if (employer == null)
                return Result.Failure<RestaurantLocation>("Employer not found.");

            var createLocationResult = RestaurantLocation.Create(
                Guid.NewGuid(),
                employerId,
                name,
                phoneNumber,
                pib,
                mb,
                streetName,
                streetNumber,
                city,
                postalCode,
                country,
                region);

            if (createLocationResult.IsFailure)
                return Result.Failure<RestaurantLocation>(createLocationResult.Error);

            await _restaurantLocationRepository.AddAsync(createLocationResult.Value);
            await _applicationUnitOfWork.SaveChangesAsync();

            return Result.Success(createLocationResult.Value);
        }

        public async Task<Result<RestaurantLocation>> UpdateEmployerLocationAsync(
            Guid employerId,
            Guid locationId,
            string name,
            string phoneNumber,
            string pib,
            string mb,
            string streetName,
            string streetNumber,
            string city,
            string postalCode,
            string country,
            string region)
        {
            var location = await _restaurantLocationRepository.GetByIdAsync(locationId);
            if (location == null || location.EmployerId != employerId)
                return Result.Failure<RestaurantLocation>("Location not found.");

            var updateResult = location.Update(
                name,
                phoneNumber,
                pib,
                mb,
                streetName,
                streetNumber,
                city,
                postalCode,
                country,
                region);

            if (updateResult.IsFailure)
                return Result.Failure<RestaurantLocation>(updateResult.Error);

            _restaurantLocationRepository.Update(location);
            await _applicationUnitOfWork.SaveChangesAsync();

            return Result.Success(location);
        }

        public async Task<Result> DeleteEmployerLocationAsync(Guid employerId, Guid locationId)
        {
            var location = await _restaurantLocationRepository.GetByIdAsync(locationId);
            if (location == null || location.EmployerId != employerId)
                return Result.Failure("Location not found.");

            var activeJobPostsCount = await _jobPostRepository.CountActiveByRestaurantLocationIdAsync(locationId);
            if (activeJobPostsCount > 0)
                return Result.Failure("Cannot delete a branch that has active job posts. Archive or remove those posts first.");

            _restaurantLocationRepository.Remove(location);
            await _applicationUnitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<IEnumerable<RestaurantLocation>> GetEmployerLocationsAsync(Guid employerId)
        {
            return await _restaurantLocationRepository.GetByEmployerIdAsync(employerId);
        }

        private void QueueConfirmationEmail(Guid userId, string email)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                    var user = await userManager.FindByIdAsync(userId.ToString());
                    if (user == null)
                    {
                        _logger.LogWarning("Confirmation email skipped: user {UserId} not found.", userId);
                        return;
                    }

                    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmationLink = $"{configuration["Backend:BaseUrl"]}/api/v1/User/confirm-email?userId={userId}&token={WebUtility.UrlEncode(token)}";
                    await emailService.SendEmailAsync(email, "Confirm Your Email",
                        $"Click <a href='{confirmationLink}'>here</a> to confirm your email.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Confirmation email was not sent to {Email}.", email);
                }
            });
        }

        private async Task<Result> AssignUniquePublicSlugAsync(Employer employer)
        {
            if (!string.IsNullOrWhiteSpace(employer.PublicSlug))
                return Result.Success();

            var slugSource = string.IsNullOrWhiteSpace(employer.Name)
                ? (employer.Email?.Split('@').FirstOrDefault() ?? employer.Id.ToString("N"))
                : employer.Name;
            var baseSlug = EmployerSlugHelper.Slugify(slugSource);
            if (string.IsNullOrWhiteSpace(baseSlug))
                baseSlug = $"employer-{employer.Id.ToString("N")[..8]}";

            var slug = baseSlug;
            var suffix = 2;

            while (await _userRepository.PublicSlugExistsAsync(slug, employer.Id))
            {
                slug = $"{baseSlug}-{suffix}";
                suffix++;
            }

            var setSlugResult = employer.SetPublicSlug(slug);
            return setSlugResult.IsFailure ? Result.Failure(setSlugResult.Error) : Result.Success();
        }

        private async Task EnsurePublicSlugAsync(Employer employer)
        {
            if (!string.IsNullOrWhiteSpace(employer.PublicSlug))
                return;

            var slugResult = await AssignUniquePublicSlugAsync(employer);
            if (slugResult.IsFailure)
                throw new InvalidOperationException(slugResult.Error);

            await _userManager.UpdateAsync(employer);
            await _applicationUnitOfWork.SaveChangesAsync();
        }

    }
}

using Core.DTOs;
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
        private readonly IApplicationUnitOfWork _applicationUnitOfWork;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, IRestaurantLocationRepository restaurantLocationRepository, UserManager<User> userManager, SignInManager<User> signInManager,
            IApplicationUnitOfWork applicationUnitOfWork, IEmailService emailService, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _restaurantLocationRepository = restaurantLocationRepository;
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

                var defaultLocationResult = RestaurantLocation.Create(
                    Guid.NewGuid(),
                    employer.Id,
                    $"{employer.Name} - Main location",
                    employer.Address.Street.Name,
                    employer.Address.Street.Number,
                    employer.Address.City.Name,
                    employer.Address.City.PostalCode.Value,
                    employer.Address.City.Country.Name,
                    employer.Address.City.Region.Name);
                if (defaultLocationResult.IsFailure)
                    return Result.Failure(defaultLocationResult.Error);

                await _restaurantLocationRepository.AddAsync(defaultLocationResult.Value);

                await _applicationUnitOfWork.SaveChangesAsync();
                await _applicationUnitOfWork.CommitTransactionAsync();

                try
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(employer);
                    var confirmationLink = $"{_configuration["Backend:BaseUrl"]}/api/v1/User/confirm-email?userId={employer.Id}&token={WebUtility.UrlEncode(token)}";
                    await _emailService.SendEmailAsync(employer.Email!, "Confirm Your Email",
                        $"Click <a href='{confirmationLink}'>here</a> to confirm your email.");
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Employer {Email} registered, but confirmation email was not sent.", employer.Email);
                    return Result.Success("User registered successfully, but confirmation email could not be sent.");
                }

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

                try
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(employee);
                    var confirmationLink = $"{_configuration["Backend:BaseUrl"]}/api/v1/User/confirm-email?userId={employee.Id}&token={WebUtility.UrlEncode(token)}";
                    await _emailService.SendEmailAsync(employee.Email!, "Confirm Your Email",
                        $"Click <a href='{confirmationLink}'>here</a> to confirm your email.");
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Employee {Email} registered, but confirmation email was not sent.", employee.Email);
                    return Result.Success("User registered successfully, but confirmation email could not be sent.");
                }

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

        public async Task<IEnumerable<EmployerFavouriteStatusDTO>> GetAllEmployersWithFavouriteStatusAsync(Guid employeeId)
        {
            var favouritedEmployerIds = await _applicationUnitOfWork.Favourites
                .GetEmployerIdsFavouritedByEmployeeAsync(employeeId);

            var employers = await _userRepository.GetAllEmployersAsync();

            return employers.Select(e => new EmployerFavouriteStatusDTO
            {
                EmployerId = e.Id,
                Name = e.Name,
                ProfilePhoto = e.ProfilePhoto,
                IsFavourite = favouritedEmployerIds.Contains(e.Id)
            }).ToList();
        }

        public async Task<Result<RestaurantLocation>> CreateEmployerLocationAsync(
            Guid employerId,
            string name,
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

        public async Task<IEnumerable<RestaurantLocation>> GetEmployerLocationsAsync(Guid employerId)
        {
            return await _restaurantLocationRepository.GetByEmployerIdAsync(employerId);
        }

    }
}

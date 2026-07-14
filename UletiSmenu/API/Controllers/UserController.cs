using API.DTOs;
using AutoMapper;
using Core.Models.Entities;
using Core.Services;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IEmployerProfileService _employerProfileService;
        private readonly IBillingService _billingService;
        private readonly IMapper _mapper;
        private readonly IFileService _fileService;
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public UserController(
            IUserService userService,
            IEmployerProfileService employerProfileService,
            IBillingService billingService,
            IMapper mapper,
            IFileService fileService,
            UserManager<User> userManager,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _userService = userService;
            _employerProfileService = employerProfileService;
            _billingService = billingService;
            _mapper = mapper;
            _fileService = fileService;
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
        }

        [HttpPost("register/employer")]
        public async Task<IActionResult> RegisterEmployer([FromBody] RegisterEmployerDTO registerDto)
        {   
            var employer = _mapper.Map<Employer>(registerDto);

            var employerResult = await _userService.RegisterEmployerAsync(employer, registerDto.Password);
            if (employerResult.IsFailure) return BadRequest(employerResult.Error);

            return Ok("User registered successfully!");
        }

        [HttpPost("register/employee")]
        public async Task<IActionResult> RegisterEmployee([FromBody] RegisterEmployeeDTO registerDto)
        {
            var employee = _mapper.Map<Employee>(registerDto);

            var employeeResult = await _userService.RegisterEmployeeAsync(employee, registerDto.Password);
            if (employeeResult.IsFailure) return BadRequest(employeeResult.Error);

            return Ok("User registered successfully!");
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            var user = await _userManager.FindByEmailAsync(request.Email.Trim());
            if (user == null)
            {
                return Ok(new { message = "If an account exists for this email, a reset link has been sent." });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var frontendBaseUrl = (_configuration["Backend:FrontendBaseUrl"] ?? "/").TrimEnd('/');
            var resetLink =
                $"{frontendBaseUrl}/reset-password?email={Uri.EscapeDataString(user.Email ?? request.Email.Trim())}&token={Uri.EscapeDataString(token)}";

            await _emailService.SendEmailAsync(
                user.Email ?? request.Email.Trim(),
                "Reset your UletiSmenu password",
                $"Click <a href='{resetLink}'>here</a> to reset your password.");

            return Ok(new { message = "If an account exists for this email, a reset link has been sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Token) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Email, token and password are required.");
            }

            var user = await _userManager.FindByEmailAsync(request.Email.Trim());
            if (user == null)
                return BadRequest("Invalid or expired reset link.");

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.Password);
            if (!result.Succeeded)
                return BadRequest(string.Join(", ", result.Errors.Select(error => error.Description)));

            return Ok(new { message = "Password reset successfully." });
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(Guid userId, string token)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return BadRequest("Invalid user.");

            var result = await _userService.ConfirmEmailAsync(userId, token);
            if (result)
            {
                return Ok("Email confirmed successfully.");
            }
            else
            {
                return BadRequest("Error confirming email.");
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var result = await _userService.LogoutUserAsync();

            if (result.IsFailure)
            {
                return BadRequest(result.Error);
            }

            return Ok(result.IsSuccess);
        }


        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("role/{roleName}")]
        public async Task<IActionResult> GetUsersByRole(string roleName, [FromQuery] string? city)
        {
            if (roleName.Equals("employer", StringComparison.OrdinalIgnoreCase))
            {
                var employers = await _userService.GetEmployersAsync(city);
                var response = employers.Select(employer => new EmployerDTO
                {
                    Id = employer.Id,
                    Name = employer.Name,
                    Email = employer.Email ?? string.Empty,
                    PhoneNumber = employer.PhoneNumber ?? string.Empty,
                    ProfilePhoto = employer.ProfilePhoto ?? string.Empty,
                    PublicSlug = employer.PublicSlug,
                });

                return Ok(response);
            }

            var users = await _userService.GetUsersByRoleAsync(roleName);
            return Ok(users);
        }

        //[HttpGet("employers/favourites")]
        //public async Task<IActionResult> GetEmployersWithFavourites()
        //{
        //    var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        //    var result = await _userService.GetAllEmployersWithFavouriteStatusAsync(userId);
        //    return Ok(result);
        //}

        [Authorize]
        [HttpPatch("me/profile-photo")]
        public async Task<IActionResult> UpdateMyProfilePhoto([FromForm] IFormFile file)
        {
            var loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(loggedInUserId) || !Guid.TryParse(loggedInUserId, out var userId))
                return Unauthorized("User is not logged in.");

            if (file == null || file.Length == 0)
                return BadRequest("Profile photo file is required.");

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            var imagePath = await _fileService.UploadImageAsync(file);
            if (string.IsNullOrWhiteSpace(imagePath)) return BadRequest("Image upload failed.");

            user.UpdateProfilePhoto(imagePath);
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Profile photo updated successfully!", imagePath });
        }

        [Authorize]
        [HttpGet("me/role")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("User ID not found in token");

            var role = await _userService.GetUserRoleAsync(Guid.Parse(userId));
            if (role == null)
                return NotFound("User role not found");

            return Ok(role);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            return role switch
            {
                "Employer" => Ok(await BuildEmployerResponseAsync(user)),
                "Employee" => Ok(_mapper.Map<EmployeeDTO>(user)),
                "Admin" => Ok(new AdminDTO
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    ProfilePhoto = user.ProfilePhoto,
                    Role = "Admin"
                }),
                _ => BadRequest("Unknown role")
            };
        }

        private async Task<EmployerDTO> BuildEmployerResponseAsync(User user)
        {
            var employer = user as Employer ?? await _userService.GetUserByIdAsync(user.Id) as Employer;
            var response = _mapper.Map<EmployerDTO>(employer ?? user);
            response.ProfilePhoto = employer?.ProfilePhoto ?? user.ProfilePhoto ?? string.Empty;
            if (employer != null)
                response.Subscription = await _billingService.GetSubscriptionStatusAsync(employer.Id);

            return response;
        }

        [Authorize(Roles = "Employee")]
        [HttpPatch("me/profile")]
        public async Task<IActionResult> UpdateMyEmployeeProfile([FromBody] UpdateEmployeeProfileDTO request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var employeeId))
                return Unauthorized();

            var result = await _userService.UpdateEmployeeProfileAsync(
                employeeId,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.City);

            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(_mapper.Map<EmployeeDTO>(result.Value));
        }

        [Authorize(Roles = "Employer")]
        [HttpPatch("me/employer-profile")]
        public async Task<IActionResult> UpdateMyEmployerProfile([FromBody] UpdateEmployerProfileDTO request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var employerId))
                return Unauthorized();

            var result = await _userService.UpdateEmployerProfileAsync(
                employerId,
                request.Name,
                request.PhoneNumber,
                request.PIB,
                request.MB,
                request.StreetName,
                request.StreetNumber,
                request.PostalCode,
                request.CountryCode,
                request.RegionCode,
                request.CityCode);

            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(await BuildEmployerResponseAsync(result.Value));
        }

        [Authorize(Roles = "Employee")]
        [HttpPost("favourite/{employerId:guid}")]
        public async Task<IActionResult> ToggleFavourite(Guid employerId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var loggedInUserId))
                return Unauthorized("Invalid user claim.");

            var result = await _userService.ToggleFavouriteEmployerAsync(loggedInUserId, employerId);

            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok();
        }

        [HttpGet("employers/cities")]
        public async Task<IActionResult> GetEmployerCities()
        {
            var cities = await _userService.GetEmployerCitiesAsync();
            return Ok(cities);
        }

        [HttpGet("employers/directory")]
        public async Task<IActionResult> GetEmployerDirectory(
            [FromQuery] string? city = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 9)
        {
            Guid? employeeId = null;
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Employee"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out var employeeIdValue))
                    employeeId = employeeIdValue;
            }

            var result = await _employerProfileService.GetEmployerDirectoryPagedAsync(
                city,
                search,
                page,
                pageSize,
                employeeId);

            return Ok(result);
        }

        [Authorize(Roles = "Employee")]
        [HttpGet("employers/")]
        public async Task<IActionResult> GetEmployersWithFavouriteStatus([FromQuery] string? city)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid user claim.");

            var result = await _userService.GetAllEmployersWithFavouriteStatusAsync(userId, city);
            var response = result.Select(x => new EmployerDTO
            {
                Id = x.EmployerId,
                Name = x.Name,
                ProfilePhoto = x.ProfilePhoto ?? string.Empty,
                PublicSlug = x.PublicSlug,
                IsFavourite = x.IsFavourite
            });

            return Ok(response);
        }

        [Authorize]
        [HttpGet("me/locations")]
        public async Task<IActionResult> GetMyLocations()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var employerId))
                return Unauthorized("Invalid user claim.");

            var user = await _userService.GetUserByIdAsync(employerId);
            if (user is not Employer)
                return Forbid();

            var employer = (Employer)user;

            var locations = await _userService.GetEmployerLocationsAsync(employerId);
            var response = _mapper.Map<List<RestaurantLocationDTO>>(locations);

            foreach (var location in response)
            {
                if (string.IsNullOrWhiteSpace(location.PIB))
                    location.PIB = employer.PIB.Value;

                if (string.IsNullOrWhiteSpace(location.MB))
                    location.MB = employer.MB.Value;
            }

            return Ok(response);
        }

        [Authorize]
        [HttpPost("me/locations")]
        public async Task<IActionResult> CreateMyLocation([FromBody] CreateRestaurantLocationDTO request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var employerId))
                return Unauthorized("Invalid user claim.");

            var user = await _userService.GetUserByIdAsync(employerId);
            if (user is not Employer)
                return Forbid();

            var result = await _userService.CreateEmployerLocationAsync(
                employerId,
                request.Name,
                request.PhoneNumber,
                request.PIB,
                request.MB,
                request.StreetName,
                request.StreetNumber,
                request.PostalCode,
                request.CountryCode,
                request.RegionCode,
                request.CityCode);

            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(_mapper.Map<RestaurantLocationDTO>(result.Value));
        }

        [Authorize]
        [HttpPut("me/locations/{locationId:guid}")]
        public async Task<IActionResult> UpdateMyLocation(Guid locationId, [FromBody] CreateRestaurantLocationDTO request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var employerId))
                return Unauthorized("Invalid user claim.");

            var user = await _userService.GetUserByIdAsync(employerId);
            if (user is not Employer)
                return Forbid();

            var result = await _userService.UpdateEmployerLocationAsync(
                employerId,
                locationId,
                request.Name,
                request.PhoneNumber,
                request.PIB,
                request.MB,
                request.StreetName,
                request.StreetNumber,
                request.PostalCode,
                request.CountryCode,
                request.RegionCode,
                request.CityCode);

            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(_mapper.Map<RestaurantLocationDTO>(result.Value));
        }

        [Authorize]
        [HttpDelete("me/locations/{locationId:guid}")]
        public async Task<IActionResult> DeleteMyLocation(Guid locationId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var employerId))
                return Unauthorized("Invalid user claim.");

            var user = await _userService.GetUserByIdAsync(employerId);
            if (user is not Employer)
                return Forbid();

            var result = await _userService.DeleteEmployerLocationAsync(employerId, locationId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(new { message = "Location deleted successfully." });
        }
    }
}

using API.DTOs;
using AutoMapper;
using Core.Models.Entities;
using Core.Services;
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
        private readonly IMapper _mapper;
        private readonly IFileService _fileService;
        private readonly UserManager<User> _userManager;

        public UserController(IUserService userService, IMapper mapper, IFileService fileService, UserManager<User> userManager)
        {
            _userService = userService;
            _mapper = mapper;
            _fileService = fileService;
            _userManager = userManager;
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
        public async Task<IActionResult> GetUsersByRole(string roleName)
        {
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
                "Employer" => Ok(_mapper.Map<EmployerDTO>(user)),
                "Employee" => Ok(_mapper.Map<EmployeeDTO>(user)),
                //"Admin" => Ok(_mapper.Map<AdminDTO>(user)),
                _ => BadRequest("Unknown role")
            };
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

        [Authorize(Roles = "Employee")]
        [HttpGet("employers/")]
        public async Task<IActionResult> GetEmployersWithFavouriteStatus()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid user claim.");

            var result = await _userService.GetAllEmployersWithFavouriteStatusAsync(userId);
            var response = result.Select(x => new EmployerDTO
            {
                Name = x.Name,
                ProfilePhoto = x.ProfilePhoto ?? string.Empty,
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

            var locations = await _userService.GetEmployerLocationsAsync(employerId);
            var response = _mapper.Map<List<RestaurantLocationDTO>>(locations);
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
                request.City,
                request.PostalCode,
                request.Country,
                request.Region);

            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(_mapper.Map<RestaurantLocationDTO>(result.Value));
        }
    }
}

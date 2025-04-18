using API.DTOs;
using AutoMapper;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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

            return Ok("Employer registered successfully!");
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

        //[Authorize]
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


        //[HttpPost("login")]
        //public async Task<IActionResult> LoginUser([FromBody] LoginUserDTO loginDto)
        //{
        //    var user = _mapper.Map<User>(loginDto);

        //    var result = await _userService.LoginUserAsync(loginDto.Email, loginDto.Password);
        //    if (result.IsFailure) return BadRequest(result.Error);

        //    return Ok("User logged in successfully!");
        //}

        [Authorize]
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

        [Authorize]
        [HttpPost("upload-profile-photo")]
        public async Task<IActionResult> UploadProfilePhoto([FromForm] ProfilePhotoUploadDTO dto)
        {
            var loggedInUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(loggedInUserId))
                return Unauthorized("User is not logged in.");

            if (loggedInUserId != dto.UserId.ToString())
                return Forbid("You can only update your own profile photo.");

            var user = await _userService.GetUserByIdAsync(dto.UserId);
            if (user == null) return NotFound("User not found");

            var imagePath = await _fileService.UploadImageAsync(dto.File);
            if (string.IsNullOrWhiteSpace(imagePath)) return BadRequest("Image upload failed.");

            user.UpdateProfilePhoto(imagePath);
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Profile photo updated successfully!", imagePath });
        }
    }
}

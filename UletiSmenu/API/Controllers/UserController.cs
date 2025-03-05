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
        private readonly ICompanyService _companyService;
        private readonly IMapper _mapper;

        public UserController(IUserService userService, ICompanyService companyService, IMapper mapper)
        {
            _userService = userService;
            _mapper = mapper;
            _companyService = companyService;
        }

        [HttpPost("register/employer")]
        public async Task<IActionResult> RegisterEmployer([FromBody] RegisterEmployerDTO registerDto)
        {
            var companyId = Guid.NewGuid();

            var employer = _mapper.Map<Employer>(registerDto, opt => opt.Items["CompanyId"] = companyId);
            var company = _mapper.Map<Company>(registerDto, opt => opt.Items["CompanyId"] = companyId);

            var employerResult = await _userService.RegisterEmployerAsync(employer, registerDto.Password);
            if (employerResult.IsFailure) return BadRequest(employerResult.Error);

            var companyResult = await _companyService.CreateCompanyAsync(companyId, registerDto.Name, company.Address);
            if (companyResult.IsFailure)
            {
                return BadRequest($"Employer created but company creation failed: {companyResult.Error}");
            }

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
    }
}

using API.DTOs;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/v1/[controller]")]
    public class EmployeeProfileController : ControllerBase
    {
        private readonly IEmployeeProfileService _employeeProfileService;

        public EmployeeProfileController(IEmployeeProfileService employeeProfileService)
        {
            _employeeProfileService = employeeProfileService;
        }

        [Authorize(Roles = "Employee")]
        [HttpGet("me/work-experiences")]
        public async Task<IActionResult> GetMyWorkExperiences()
        {
            var employeeId = GetCurrentUserId();
            if (employeeId == null)
                return Unauthorized();

            var result = await _employeeProfileService.GetMyWorkExperiencesAsync(employeeId.Value);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [Authorize(Roles = "Employee")]
        [HttpPost("me/work-experiences")]
        public async Task<IActionResult> CreateWorkExperience([FromBody] UpsertWorkExperienceDTO request)
        {
            var employeeId = GetCurrentUserId();
            if (employeeId == null)
                return Unauthorized();

            var result = await _employeeProfileService.CreateWorkExperienceAsync(
                employeeId.Value,
                request.CompanyName,
                request.Position,
                request.StartDate,
                request.EndDate,
                request.Description);

            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [Authorize(Roles = "Employee")]
        [HttpPut("me/work-experiences/{experienceId:guid}")]
        public async Task<IActionResult> UpdateWorkExperience(Guid experienceId, [FromBody] UpsertWorkExperienceDTO request)
        {
            var employeeId = GetCurrentUserId();
            if (employeeId == null)
                return Unauthorized();

            var result = await _employeeProfileService.UpdateWorkExperienceAsync(
                employeeId.Value,
                experienceId,
                request.CompanyName,
                request.Position,
                request.StartDate,
                request.EndDate,
                request.Description);

            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [Authorize(Roles = "Employee")]
        [HttpDelete("me/work-experiences/{experienceId:guid}")]
        public async Task<IActionResult> DeleteWorkExperience(Guid experienceId)
        {
            var employeeId = GetCurrentUserId();
            if (employeeId == null)
                return Unauthorized();

            var result = await _employeeProfileService.DeleteWorkExperienceAsync(employeeId.Value, experienceId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return NoContent();
        }

        [Authorize(Roles = "Employee")]
        [HttpGet("me/platform-shifts")]
        public async Task<IActionResult> GetMyPlatformShifts()
        {
            var employeeId = GetCurrentUserId();
            if (employeeId == null)
                return Unauthorized();

            var result = await _employeeProfileService.GetMyPlatformShiftsAsync(employeeId.Value);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [Authorize(Roles = "Employer")]
        [HttpGet("{employeeId:guid}")]
        public async Task<IActionResult> GetEmployeeProfile(Guid employeeId)
        {
            var employerId = GetCurrentUserId();
            if (employerId == null)
                return Unauthorized();

            var result = await _employeeProfileService.GetEmployeeProfileForEmployerAsync(employerId.Value, employeeId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}

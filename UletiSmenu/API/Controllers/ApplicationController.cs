using API.DTOs;
using Core.Models.Enums;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ApplicationController : ControllerBase
    {
        private readonly IApplicationService _applicationService;

        public ApplicationController(IApplicationService applicationService)
        {
            _applicationService = applicationService;
        }

        [Authorize(Roles = "Employee")]
        [HttpPost("job-posts/{jobPostId:guid}")]
        public async Task<IActionResult> ApplyToJobPost(Guid jobPostId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var employeeId))
                return Unauthorized("Invalid user claim.");

            var result = await _applicationService.ApplyToJobPostAsync(employeeId, jobPostId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(new { message = "Application submitted successfully." });
        }

        [Authorize(Roles = "Employer")]
        [HttpGet("job-posts/{jobPostId:guid}/applicants")]
        public async Task<IActionResult> GetApplicantsForMyJobPost(Guid jobPostId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var employerId))
                return Unauthorized("Invalid user claim.");

            var includeContactInfo = User.IsInRole("Admin");
            var result = await _applicationService.GetApplicantsForEmployerJobPostAsync(
                employerId,
                jobPostId,
                includeContactInfo);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [Authorize(Roles = "Employee")]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyApplications()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var employeeId))
                return Unauthorized("Invalid user claim.");

            var result = await _applicationService.GetMyApplicationsAsync(employeeId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [Authorize(Roles = "Employer")]
        [HttpPatch("{applicationId:guid}/status")]
        public async Task<IActionResult> UpdateApplicationStatus(Guid applicationId, [FromBody] UpdateApplicationStatusDTO request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var employerId))
                return Unauthorized("Invalid user claim.");

            if (!Enum.TryParse<ApplicationStatusEnum>(request.Status, true, out var parsedStatus))
                return BadRequest("Invalid application status.");

            var result = await _applicationService.UpdateApplicationStatusByEmployerAsync(employerId, applicationId, parsedStatus);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(new { message = "Application status updated successfully." });
        }

        [Authorize(Roles = "Employee")]
        [HttpPatch("{applicationId:guid}/cancel")]
        public async Task<IActionResult> CancelMyApplication(Guid applicationId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var employeeId))
                return Unauthorized("Invalid user claim.");

            var result = await _applicationService.CancelMyApplicationAsync(employeeId, applicationId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(new { message = "Application cancelled successfully." });
        }
    }
}

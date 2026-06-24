using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/v1/[controller]")]
    public class EmployerProfileController : ControllerBase
    {
        private readonly IEmployerProfileService _employerProfileService;

        public EmployerProfileController(IEmployerProfileService employerProfileService)
        {
            _employerProfileService = employerProfileService;
        }

        [Authorize(Roles = "Employee")]
        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetEmployerProfileBySlug(string slug)
        {
            var employeeId = GetCurrentUserId();
            if (employeeId == null)
                return Unauthorized();

            var result = await _employerProfileService.GetEmployerPublicProfileBySlugAsync(slug, employeeId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [Authorize(Roles = "Employer")]
        [HttpGet("preview/slug/{slug}")]
        public async Task<IActionResult> GetEmployerDirectoryPreviewBySlug(string slug)
        {
            var viewerId = GetCurrentUserId();
            if (viewerId == null)
                return Unauthorized();

            var result = await _employerProfileService.GetEmployerDirectoryPreviewBySlugAsync(slug);
            if (result.IsFailure)
                return BadRequest(result.Error);

            if (result.Value.EmployerId == viewerId.Value)
                return BadRequest("Use your profile page to view your own restaurant details.");

            return Ok(result.Value);
        }

        [HttpGet("{employerId:guid}/slug")]
        public async Task<IActionResult> ResolveEmployerSlug(Guid employerId)
        {
            var result = await _employerProfileService.ResolveEmployerSlugAsync(employerId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(new { slug = result.Value });
        }

        [Authorize(Roles = "Employee")]
        [HttpGet("{employerId:guid}")]
        public async Task<IActionResult> GetEmployerProfile(Guid employerId)
        {
            var employeeId = GetCurrentUserId();
            if (employeeId == null)
                return Unauthorized();

            var result = await _employerProfileService.GetEmployerPublicProfileAsync(employerId, employeeId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [Authorize(Roles = "Employer")]
        [HttpGet("{employerId:guid}/preview")]
        public async Task<IActionResult> GetEmployerDirectoryPreview(Guid employerId)
        {
            var viewerId = GetCurrentUserId();
            if (viewerId == null)
                return Unauthorized();

            if (viewerId.Value == employerId)
                return BadRequest("Use your profile page to view your own restaurant details.");

            var result = await _employerProfileService.GetEmployerDirectoryPreviewAsync(employerId);
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

using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Authorize(Roles = "Employee")]
    [Route("api/v1/[controller]")]
    public class EmployerProfileController : ControllerBase
    {
        private readonly IEmployerProfileService _employerProfileService;

        public EmployerProfileController(IEmployerProfileService employerProfileService)
        {
            _employerProfileService = employerProfileService;
        }

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

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}

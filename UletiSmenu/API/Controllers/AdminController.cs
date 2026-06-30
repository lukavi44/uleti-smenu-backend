using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/v1/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(
            [FromQuery] DateTime? fromUtc = null,
            [FromQuery] DateTime? toUtc = null)
        {
            var dashboard = await _adminService.GetDashboardAsync(fromUtc, toUtc);
            return Ok(dashboard);
        }

        [HttpGet("employers")]
        public async Task<IActionResult> GetEmployers(
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] string? city = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetEmployersAsync(search, status, city, page, pageSize);
            return Ok(result);
        }

        [HttpGet("employers/{employerId:guid}")]
        public async Task<IActionResult> GetEmployerDetail(Guid employerId)
        {
            var result = await _adminService.GetEmployerDetailAsync(employerId);
            if (result.IsFailure)
                return NotFound(result.Error);

            return Ok(result.Value);
        }

        [HttpGet("candidates")]
        public async Task<IActionResult> GetCandidates(
            [FromQuery] string? search = null,
            [FromQuery] string? city = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetCandidatesAsync(search, city, page, pageSize);
            return Ok(result);
        }

        [HttpGet("restaurants")]
        public async Task<IActionResult> GetRestaurants(
            [FromQuery] string? search = null,
            [FromQuery] string? city = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetRestaurantsAsync(search, city, page, pageSize);
            return Ok(result);
        }

        [HttpGet("job-posts")]
        public async Task<IActionResult> GetJobPosts(
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetJobPostsAsync(search, status, page, pageSize);
            return Ok(result);
        }

        [HttpGet("applications")]
        public async Task<IActionResult> GetApplications(
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetApplicationsAsync(search, status, page, pageSize);
            return Ok(result);
        }

        [HttpGet("billing")]
        public async Task<IActionResult> GetBilling(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetBillingTransactionsAsync(search, page, pageSize);
            return Ok(result);
        }

        [HttpPut("employers/{employerId:guid}/verification")]
        public async Task<IActionResult> SetEmployerVerification(
            Guid employerId,
            [FromBody] SetEmployerVerificationRequest request)
        {
            var adminUserId = GetCurrentUserId();
            if (adminUserId == null)
                return Unauthorized();

            var result = await _adminService.SetEmployerVerificationAsync(
                employerId,
                request.IsVerified,
                adminUserId.Value);

            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        private Guid? GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out var userId) ? userId : null;
        }
    }

    public class SetEmployerVerificationRequest
    {
        public bool IsVerified { get; set; }
    }
}

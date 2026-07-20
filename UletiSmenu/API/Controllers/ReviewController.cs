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
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private readonly IUserService _userService;

        public ReviewController(
            IReviewService reviewService,
            IUserService userService)
        {
            _reviewService = reviewService;
            _userService = userService;
        }

        [HttpGet("me/pending")]
        public async Task<IActionResult> GetMyPendingReviews()
        {
            var (userId, role, errorResult) = await ResolveCurrentUserAsync();
            if (errorResult != null)
                return errorResult;

            var result = await _reviewService.GetMyPendingReviewsAsync(userId, role!);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewDTO request)
        {
            var (userId, _, errorResult) = await ResolveCurrentUserAsync();
            if (errorResult != null)
                return errorResult;

            var result = await _reviewService.SubmitReviewAsync(userId, request.ApplicationId, request.Rating, request.Comment);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [HttpGet("employees/{employeeId:guid}")]
        public async Task<IActionResult> GetEmployeeReviews(Guid employeeId)
        {
            var accessError = AuthorizeEmployeeReviewRead(employeeId);
            if (accessError != null)
                return accessError;

            var result = await _reviewService.GetEmployeeReviewsAsync(employeeId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [HttpGet("employees/{employeeId:guid}/summary")]
        public async Task<IActionResult> GetEmployeeReviewSummary(Guid employeeId)
        {
            var accessError = AuthorizeEmployeeReviewRead(employeeId);
            if (accessError != null)
                return accessError;

            var result = await _reviewService.GetEmployeeReviewSummaryAsync(employeeId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [HttpGet("employees/{employeeId:guid}/page")]
        public async Task<IActionResult> GetEmployeeReviewPage(Guid employeeId)
        {
            var accessError = AuthorizeEmployeeReviewRead(employeeId);
            if (accessError != null)
                return accessError;

            var result = await _reviewService.GetEmployeeReviewPageAsync(employeeId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [AllowAnonymous]
        [HttpGet("employers/{employerId:guid}/summary")]
        public async Task<IActionResult> GetEmployerReviewSummary(Guid employerId)
        {
            var result = await _reviewService.GetEmployerReviewSummaryAsync(employerId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [AllowAnonymous]
        [HttpGet("employers/{employerId:guid}/page")]
        public async Task<IActionResult> GetEmployerReviewPage(Guid employerId)
        {
            var result = await _reviewService.GetEmployerReviewPageAsync(employerId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        private IActionResult? AuthorizeEmployeeReviewRead(Guid employeeId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var currentUserId))
                return Unauthorized();

            if (currentUserId != employeeId && !User.IsInRole("Admin"))
                return Forbid();

            return null;
        }

        private async Task<(Guid UserId, string? Role, IActionResult? ErrorResult)> ResolveCurrentUserAsync()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                return (Guid.Empty, null, Unauthorized("Invalid user claim."));

            var role = await _userService.GetUserRoleAsync(userId);
            if (string.IsNullOrWhiteSpace(role))
                return (Guid.Empty, null, Forbid());

            return (userId, role, null);
        }
    }
}

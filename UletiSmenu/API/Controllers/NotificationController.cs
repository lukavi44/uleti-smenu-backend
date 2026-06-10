using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Roles = "Employee,Employer")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IReviewReminderService _reviewReminderService;
        private readonly IUserService _userService;

        public NotificationController(
            INotificationService notificationService,
            IReviewReminderService reviewReminderService,
            IUserService userService)
        {
            _notificationService = notificationService;
            _reviewReminderService = reviewReminderService;
            _userService = userService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyNotifications()
        {
            var (userId, role, errorResult) = await ResolveCurrentUserAsync();
            if (errorResult != null)
                return errorResult;

            await _reviewReminderService.SyncReviewRemindersAsync(userId, role!);

            var result = await _notificationService.GetMyNotificationsAsync(userId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [HttpGet("me/unread-count")]
        public async Task<IActionResult> GetMyUnreadCount()
        {
            var (userId, _, errorResult) = await ResolveCurrentUserAsync();
            if (errorResult != null)
                return errorResult;

            var result = await _notificationService.GetMyUnreadCountAsync(userId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(new { count = result.Value });
        }

        [HttpPatch("{notificationId:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid notificationId)
        {
            var (userId, _, errorResult) = await ResolveCurrentUserAsync();
            if (errorResult != null)
                return errorResult;

            var result = await _notificationService.MarkAsReadAsync(userId, notificationId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(new { message = "Notification marked as read." });
        }

        [HttpDelete("{notificationId:guid}")]
        public async Task<IActionResult> Delete(Guid notificationId)
        {
            var (userId, _, errorResult) = await ResolveCurrentUserAsync();
            if (errorResult != null)
                return errorResult;

            var result = await _notificationService.DeleteAsync(userId, notificationId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(new { message = "Notification deleted successfully." });
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

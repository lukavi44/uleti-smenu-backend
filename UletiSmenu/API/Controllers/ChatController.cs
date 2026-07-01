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
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IUserService _userService;

        public ChatController(IChatService chatService, IUserService userService)
        {
            _chatService = chatService;
            _userService = userService;
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetMyUnreadCount()
        {
            var (userId, role, errorResult) = await ResolveCurrentUserAsync();
            if (errorResult != null)
                return errorResult;

            var result = await _chatService.GetMyUnreadCountAsync(userId, role!);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(new { count = result.Value });
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetMyConversations([FromQuery] string status = "active")
        {
            var (userId, role, errorResult) = await ResolveCurrentUserAsync();
            if (errorResult != null)
                return errorResult;

            var result = await _chatService.GetMyConversationsAsync(userId, role!, status);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [HttpGet("applications/{applicationId:guid}")]
        public async Task<IActionResult> GetConversationByApplication(Guid applicationId)
        {
            var (userId, _, errorResult) = await ResolveCurrentUserAsync();
            if (errorResult != null)
                return errorResult;

            var result = await _chatService.GetConversationByApplicationAsync(userId, applicationId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [HttpGet("conversations/{conversationId:guid}/messages")]
        public async Task<IActionResult> GetMessages(Guid conversationId)
        {
            var (userId, _, errorResult) = await ResolveCurrentUserAsync();
            if (errorResult != null)
                return errorResult;

            var result = await _chatService.GetMessagesAsync(userId, conversationId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }

        [HttpPost("conversations/{conversationId:guid}/read")]
        public async Task<IActionResult> MarkConversationRead(Guid conversationId)
        {
            var (userId, _, errorResult) = await ResolveCurrentUserAsync();
            if (errorResult != null)
                return errorResult;

            var result = await _chatService.MarkConversationReadAsync(userId, conversationId);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return NoContent();
        }

        [HttpPost("conversations/{conversationId:guid}/messages")]
        public async Task<IActionResult> SendMessage(Guid conversationId, [FromBody] SendChatMessageDTO request)
        {
            var (userId, _, errorResult) = await ResolveCurrentUserAsync();
            if (errorResult != null)
                return errorResult;

            var result = await _chatService.SendMessageAsync(userId, conversationId, request.Content);
            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
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

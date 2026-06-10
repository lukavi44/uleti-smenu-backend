using Core.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace API.Hubs
{
    [Authorize]
    public class RealtimeHub : Hub
    {
        private readonly IChatRepository _chatRepository;

        public RealtimeHub(IChatRepository chatRepository)
        {
            _chatRepository = chatRepository;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId.Value));
            }

            await base.OnConnectedAsync();
        }

        public async Task JoinConversation(string conversationId)
        {
            if (!Guid.TryParse(conversationId, out var parsedConversationId))
                throw new HubException("Invalid conversation id.");

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                throw new HubException("Unauthorized.");

            if (!await _chatRepository.UserIsParticipantAsync(userId.Value, parsedConversationId))
                throw new HubException("You do not have access to this conversation.");

            await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(parsedConversationId));
        }

        public async Task LeaveConversation(string conversationId)
        {
            if (!Guid.TryParse(conversationId, out var parsedConversationId))
                return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversationGroup(parsedConversationId));
        }

        public static string UserGroup(Guid userId) => $"user:{userId}";

        public static string ConversationGroup(Guid conversationId) => $"conversation:{conversationId}";

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}

using API.Hubs;
using Core.DTOs;
using Core.Services;
using Microsoft.AspNetCore.SignalR;

namespace API.Services
{
    public class RealtimeNotifier : IRealtimeNotifier
    {
        private readonly IHubContext<RealtimeHub> _hubContext;

        public RealtimeNotifier(IHubContext<RealtimeHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task NotifyChatMessageAsync(Guid conversationId, Guid recipientUserId, ChatMessageDTO message)
        {
            return _hubContext.Clients
                .Group(RealtimeHub.ConversationGroup(conversationId))
                .SendAsync("ReceiveChatMessage", new
                {
                    conversationId,
                    message
                });
        }

        public Task NotifyChatUnreadCountAsync(Guid userId, int unreadCount)
        {
            return _hubContext.Clients
                .Group(RealtimeHub.UserGroup(userId))
                .SendAsync("ChatUnreadCountUpdated", new { count = unreadCount });
        }

        public Task NotifyNotificationAsync(Guid userId, UserNotificationDTO notification, int unreadCount)
        {
            return _hubContext.Clients
                .Group(RealtimeHub.UserGroup(userId))
                .SendAsync("NotificationReceived", new
                {
                    notification,
                    unreadCount
                });
        }
    }
}

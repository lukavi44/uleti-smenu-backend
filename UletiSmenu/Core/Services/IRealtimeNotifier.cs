using Core.DTOs;

namespace Core.Services
{
    public interface IRealtimeNotifier
    {
        Task NotifyChatMessageAsync(Guid conversationId, Guid recipientUserId, ChatMessageDTO message);
        Task NotifyChatUnreadCountAsync(Guid userId, int unreadCount);
        Task NotifyNotificationAsync(Guid userId, UserNotificationDTO notification, int unreadCount);
    }
}

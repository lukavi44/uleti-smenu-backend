using Core.DTOs;
using Core.Models.Entities;
using Core.Models.Enums;

namespace Core.Repositories
{
    public interface IChatRepository
    {
        Task<Conversation?> GetConversationByIdAsync(Guid conversationId);
        Task<Conversation?> GetConversationByApplicationIdAsync(Guid applicationId);
        Task<bool> UserIsParticipantAsync(Guid userId, Guid conversationId);
        Task<List<ChatConversationListItemDTO>> GetConversationsForEmployerAsync(
            Guid employerId,
            ConversationStatusEnum status);
        Task<List<ChatConversationListItemDTO>> GetConversationsForEmployeeAsync(
            Guid employeeId,
            ConversationStatusEnum status);
        Task<List<ChatMessageDTO>> GetMessagesAsync(Guid conversationId);
        Task<ChatMessagePageDTO> GetMessagesPageAsync(Guid conversationId, DateTime? beforeUtc, int pageSize);
        Task<DateTime?> GetLatestMessageSentAtAsync(Guid conversationId);
        Task AddConversationAsync(Conversation conversation);
        void UpdateConversation(Conversation conversation);
        Task AddMessageAsync(ChatMessage message);
        Task<int> GetTotalUnreadCountAsync(Guid userId);
        Task MarkConversationReadAsync(Guid userId, Guid conversationId, DateTime readAtUtc);
        Task ArchiveExpiredConversationsForUserAsync(Guid userId, DateTime utcNow);
        Task NormalizeEmptyConversationStatusesAsync();
    }
}

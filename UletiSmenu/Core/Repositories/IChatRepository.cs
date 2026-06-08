using Core.DTOs;
using Core.Models.Entities;

namespace Core.Repositories
{
    public interface IChatRepository
    {
        Task<Conversation?> GetConversationByIdAsync(Guid conversationId);
        Task<Conversation?> GetConversationByApplicationIdAsync(Guid applicationId);
        Task<bool> UserIsParticipantAsync(Guid userId, Guid conversationId);
        Task<List<ChatConversationListItemDTO>> GetConversationsForEmployerAsync(Guid employerId);
        Task<List<ChatConversationListItemDTO>> GetConversationsForEmployeeAsync(Guid employeeId);
        Task<List<ChatMessageDTO>> GetMessagesAsync(Guid conversationId);
        Task AddConversationAsync(Conversation conversation);
        Task AddMessageAsync(ChatMessage message);
        Task<int> GetTotalUnreadCountAsync(Guid userId);
        Task MarkConversationReadAsync(Guid userId, Guid conversationId, DateTime readAtUtc);
    }
}

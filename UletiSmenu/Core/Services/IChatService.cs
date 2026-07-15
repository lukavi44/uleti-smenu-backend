using Core.DTOs;
using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IChatService
    {
        Task<Result<List<ChatConversationListItemDTO>>> GetMyConversationsAsync(
            Guid userId,
            string role,
            string status = "active");
        Task<Result<ChatConversationListItemDTO>> GetConversationByApplicationAsync(Guid userId, Guid applicationId);
        Task<Result<ChatMessagePageDTO>> GetMessagesAsync(
            Guid userId,
            Guid conversationId,
            DateTime? beforeUtc = null,
            int pageSize = 30);
        Task<Result<ChatMessageDTO>> SendMessageAsync(Guid userId, Guid conversationId, string content);
        Task<Result<int>> GetMyUnreadCountAsync(Guid userId, string role);
        Task<Result> MarkConversationReadAsync(Guid userId, Guid conversationId);
    }
}

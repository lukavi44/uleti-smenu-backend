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
        Task<Result<List<ChatMessageDTO>>> GetMessagesAsync(Guid userId, Guid conversationId);
        Task<Result<ChatMessageDTO>> SendMessageAsync(Guid userId, Guid conversationId, string content);
        Task<Result<int>> GetMyUnreadCountAsync(Guid userId, string role);
        Task<Result> MarkConversationReadAsync(Guid userId, Guid conversationId);
    }
}

using Core.DTOs;
using Core.Models.Entities;
using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IChatAccessService
    {
        Task<Result<ChatAccessEvaluationDTO>> EvaluateApplicationAccessAsync(Guid userId, Guid applicationId);
        Task<Result<ChatAccessEvaluationDTO>> EvaluateConversationAccessAsync(
            Guid userId,
            Guid conversationId,
            bool requireSend);
        Task ArchiveConversationIfNeededAsync(Conversation conversation, DateTime shiftStartUtc, DateTime utcNow);
    }
}

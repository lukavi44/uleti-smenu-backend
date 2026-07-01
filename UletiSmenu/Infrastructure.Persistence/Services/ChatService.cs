using Core.DTOs;
using Core.Models.Enums;
using Core.Repositories;
using Core.Services;
using CSharpFunctionalExtensions;

namespace Infrastructure.Persistence.Services
{
    public class ChatService : IChatService
    {
        private readonly IChatRepository _chatRepository;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IJobPostRepository _jobPostRepository;
        private readonly IChatAccessService _chatAccessService;
        private readonly IApplicationUnitOfWork _applicationUnitOfWork;
        private readonly IRealtimeNotifier _realtimeNotifier;

        public ChatService(
            IChatRepository chatRepository,
            IApplicationRepository applicationRepository,
            IJobPostRepository jobPostRepository,
            IChatAccessService chatAccessService,
            IApplicationUnitOfWork applicationUnitOfWork,
            IRealtimeNotifier realtimeNotifier)
        {
            _chatRepository = chatRepository;
            _applicationRepository = applicationRepository;
            _jobPostRepository = jobPostRepository;
            _chatAccessService = chatAccessService;
            _applicationUnitOfWork = applicationUnitOfWork;
            _realtimeNotifier = realtimeNotifier;
        }

        public async Task<Result<List<ChatConversationListItemDTO>>> GetMyConversationsAsync(
            Guid userId,
            string role,
            string status = "active")
        {
            if (role != UserRolesEnum.Employer.ToString() && role != UserRolesEnum.Employee.ToString())
                return Result.Failure<List<ChatConversationListItemDTO>>("Only employers and employees can access chat.");

            var conversationStatus = ParseConversationStatus(status);
            if (conversationStatus == null)
                return Result.Failure<List<ChatConversationListItemDTO>>("Invalid conversation status filter.");

            var utcNow = DateTime.UtcNow;
            await _chatRepository.NormalizeEmptyConversationStatusesAsync();
            await _chatRepository.ArchiveExpiredConversationsForUserAsync(userId, utcNow);
            await _applicationUnitOfWork.SaveChangesAsync();

            if (role == UserRolesEnum.Employer.ToString())
            {
                return Result.Success(
                    await _chatRepository.GetConversationsForEmployerAsync(userId, conversationStatus.Value));
            }

            return Result.Success(
                await _chatRepository.GetConversationsForEmployeeAsync(userId, conversationStatus.Value));
        }

        public async Task<Result<ChatConversationListItemDTO>> GetConversationByApplicationAsync(
            Guid userId,
            Guid applicationId)
        {
            var accessResult = await _chatAccessService.EvaluateApplicationAccessAsync(userId, applicationId);
            if (accessResult.IsFailure)
                return Result.Failure<ChatConversationListItemDTO>(accessResult.Error);

            var evaluation = accessResult.Value;
            var conversation = evaluation.Conversation;
            if (conversation == null)
            {
                var jobPost = await _jobPostRepository.GetJobPostByIdAsync(evaluation.Application.JobPostId);
                if (jobPost == null)
                    return Result.Failure<ChatConversationListItemDTO>("Job post not found.");

                conversation = Core.Models.Entities.Conversation.Create(
                    evaluation.Application.Id,
                    jobPost.EmployerId,
                    evaluation.Application.UserId,
                    jobPost.Id,
                    DateTime.UtcNow);

                await _chatRepository.AddConversationAsync(conversation);
                await _applicationUnitOfWork.SaveChangesAsync();

                if (evaluation.IsArchived)
                {
                    await _chatAccessService.ArchiveConversationIfNeededAsync(
                        conversation,
                        evaluation.ShiftStartUtc,
                        DateTime.UtcNow);
                }
            }

            var role = conversation.EmployerId == userId
                ? UserRolesEnum.Employer.ToString()
                : UserRolesEnum.Employee.ToString();

            var statusFilter = evaluation.IsArchived ? "archived" : "active";
            var conversationsResult = await GetMyConversationsAsync(userId, role, statusFilter);
            if (conversationsResult.IsFailure)
                return Result.Failure<ChatConversationListItemDTO>(conversationsResult.Error);

            var conversationItem = conversationsResult.Value
                .FirstOrDefault(item => item.ConversationId == conversation.Id);

            if (conversationItem == null)
            {
                conversationItem = new ChatConversationListItemDTO
                {
                    ConversationId = conversation.Id,
                    ApplicationId = conversation.ApplicationId,
                    JobPostId = conversation.JobPostId,
                    Status = evaluation.Status.ToString(),
                    IsReadOnly = evaluation.IsArchived,
                    CanSendMessages = evaluation.CanSend
                };
            }

            return Result.Success(conversationItem);
        }

        public async Task<Result<List<ChatMessageDTO>>> GetMessagesAsync(Guid userId, Guid conversationId)
        {
            var accessResult = await _chatAccessService.EvaluateConversationAccessAsync(
                userId,
                conversationId,
                requireSend: false);

            if (accessResult.IsFailure)
                return Result.Failure<List<ChatMessageDTO>>(accessResult.Error);

            var messages = await _chatRepository.GetMessagesAsync(conversationId);
            return Result.Success(messages);
        }

        public async Task<Result<ChatMessageDTO>> SendMessageAsync(Guid userId, Guid conversationId, string content)
        {
            var accessResult = await _chatAccessService.EvaluateConversationAccessAsync(
                userId,
                conversationId,
                requireSend: true);

            if (accessResult.IsFailure)
                return Result.Failure<ChatMessageDTO>(accessResult.Error);

            var messageResult = Core.Models.Entities.ChatMessage.Create(
                conversationId,
                userId,
                content,
                DateTime.UtcNow);

            if (messageResult.IsFailure)
                return Result.Failure<ChatMessageDTO>(messageResult.Error);

            var conversation = accessResult.Value.Conversation!;
            conversation.TouchLastMessageAt(messageResult.Value.SentAtUtc);
            _chatRepository.UpdateConversation(conversation);

            await _chatRepository.AddMessageAsync(messageResult.Value);
            await _applicationUnitOfWork.SaveChangesAsync();

            var message = messageResult.Value;
            var messageDto = new ChatMessageDTO
            {
                Id = message.Id,
                SenderId = message.SenderId,
                Content = message.Content,
                SentAtUtc = message.SentAtUtc
            };

            var recipientUserId = conversation.EmployerId == userId
                ? conversation.EmployeeId
                : conversation.EmployerId;

            await _realtimeNotifier.NotifyChatMessageAsync(conversationId, recipientUserId, messageDto);

            var unreadCount = await _chatRepository.GetTotalUnreadCountAsync(recipientUserId);
            await _realtimeNotifier.NotifyChatUnreadCountAsync(recipientUserId, unreadCount);

            return Result.Success(messageDto);
        }

        public async Task<Result<int>> GetMyUnreadCountAsync(Guid userId, string role)
        {
            if (role != UserRolesEnum.Employer.ToString() && role != UserRolesEnum.Employee.ToString())
                return Result.Failure<int>("Only employers and employees can access chat.");

            await _chatRepository.NormalizeEmptyConversationStatusesAsync();
            await _chatRepository.ArchiveExpiredConversationsForUserAsync(userId, DateTime.UtcNow);
            await _applicationUnitOfWork.SaveChangesAsync();

            var count = await _chatRepository.GetTotalUnreadCountAsync(userId);
            return Result.Success(count);
        }

        public async Task<Result> MarkConversationReadAsync(Guid userId, Guid conversationId)
        {
            var accessResult = await _chatAccessService.EvaluateConversationAccessAsync(
                userId,
                conversationId,
                requireSend: false);

            if (accessResult.IsFailure)
                return Result.Failure(accessResult.Error);

            var messages = await _chatRepository.GetMessagesAsync(conversationId);
            var readAtUtc = messages.Count > 0
                ? messages.Max(message => message.SentAtUtc)
                : DateTime.UtcNow;

            await _chatRepository.MarkConversationReadAsync(userId, conversationId, readAtUtc);
            await _applicationUnitOfWork.SaveChangesAsync();

            var unreadCount = await _chatRepository.GetTotalUnreadCountAsync(userId);
            await _realtimeNotifier.NotifyChatUnreadCountAsync(userId, unreadCount);

            return Result.Success();
        }

        private static ConversationStatusEnum? ParseConversationStatus(string status)
        {
            return status.Trim().ToLowerInvariant() switch
            {
                "active" => ConversationStatusEnum.Active,
                "archived" => ConversationStatusEnum.Archived,
                _ => null
            };
        }
    }
}

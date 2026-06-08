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
        private readonly IApplicationUnitOfWork _applicationUnitOfWork;

        public ChatService(
            IChatRepository chatRepository,
            IApplicationRepository applicationRepository,
            IJobPostRepository jobPostRepository,
            IApplicationUnitOfWork applicationUnitOfWork)
        {
            _chatRepository = chatRepository;
            _applicationRepository = applicationRepository;
            _jobPostRepository = jobPostRepository;
            _applicationUnitOfWork = applicationUnitOfWork;
        }

        public async Task<Result<List<ChatConversationListItemDTO>>> GetMyConversationsAsync(Guid userId, string role)
        {
            if (role == UserRolesEnum.Employer.ToString())
            {
                return Result.Success(await _chatRepository.GetConversationsForEmployerAsync(userId));
            }

            if (role == UserRolesEnum.Employee.ToString())
            {
                return Result.Success(await _chatRepository.GetConversationsForEmployeeAsync(userId));
            }

            return Result.Failure<List<ChatConversationListItemDTO>>("Only employers and employees can access chat.");
        }

        public async Task<Result<ChatConversationListItemDTO>> GetConversationByApplicationAsync(Guid userId, Guid applicationId)
        {
            var application = await _applicationRepository.GetByIdAsync(applicationId);
            if (application == null)
                return Result.Failure<ChatConversationListItemDTO>("Application not found.");

            if (application.Status != ApplicationStatusEnum.Accepted)
                return Result.Failure<ChatConversationListItemDTO>("Chat is available only for accepted applications.");

            var conversation = await _chatRepository.GetConversationByApplicationIdAsync(applicationId);
            if (conversation == null)
            {
                var jobPost = await _jobPostRepository.GetJobPostByIdAsync(application.JobPostId);
                if (jobPost == null)
                    return Result.Failure<ChatConversationListItemDTO>("Job post not found.");

                conversation = Core.Models.Entities.Conversation.Create(
                    application.Id,
                    jobPost.EmployerId,
                    application.UserId,
                    jobPost.Id,
                    DateTime.UtcNow);

                await _chatRepository.AddConversationAsync(conversation);
                await _applicationUnitOfWork.SaveChangesAsync();
            }

            if (conversation.EmployerId != userId && conversation.EmployeeId != userId)
                return Result.Failure<ChatConversationListItemDTO>("You do not have access to this conversation.");

            var conversationsResult = await GetMyConversationsAsync(
                userId,
                conversation.EmployerId == userId
                    ? UserRolesEnum.Employer.ToString()
                    : UserRolesEnum.Employee.ToString());

            if (conversationsResult.IsFailure)
                return Result.Failure<ChatConversationListItemDTO>(conversationsResult.Error);

            var conversationItem = conversationsResult.Value
                .FirstOrDefault(item => item.ConversationId == conversation.Id);

            if (conversationItem == null)
                return Result.Failure<ChatConversationListItemDTO>("Conversation not found.");

            return Result.Success(conversationItem);
        }

        public async Task<Result<List<ChatMessageDTO>>> GetMessagesAsync(Guid userId, Guid conversationId)
        {
            if (!await _chatRepository.UserIsParticipantAsync(userId, conversationId))
                return Result.Failure<List<ChatMessageDTO>>("You do not have access to this conversation.");

            var messages = await _chatRepository.GetMessagesAsync(conversationId);
            return Result.Success(messages);
        }

        public async Task<Result<ChatMessageDTO>> SendMessageAsync(Guid userId, Guid conversationId, string content)
        {
            if (!await _chatRepository.UserIsParticipantAsync(userId, conversationId))
                return Result.Failure<ChatMessageDTO>("You do not have access to this conversation.");

            var messageResult = Core.Models.Entities.ChatMessage.Create(
                conversationId,
                userId,
                content,
                DateTime.UtcNow);

            if (messageResult.IsFailure)
                return Result.Failure<ChatMessageDTO>(messageResult.Error);

            await _chatRepository.AddMessageAsync(messageResult.Value);
            await _applicationUnitOfWork.SaveChangesAsync();

            var message = messageResult.Value;
            return Result.Success(new ChatMessageDTO
            {
                Id = message.Id,
                SenderId = message.SenderId,
                Content = message.Content,
                SentAtUtc = message.SentAtUtc
            });
        }

        public async Task<Result<int>> GetMyUnreadCountAsync(Guid userId, string role)
        {
            if (role != UserRolesEnum.Employer.ToString() && role != UserRolesEnum.Employee.ToString())
                return Result.Failure<int>("Only employers and employees can access chat.");

            var count = await _chatRepository.GetTotalUnreadCountAsync(userId);
            return Result.Success(count);
        }

        public async Task<Result> MarkConversationReadAsync(Guid userId, Guid conversationId)
        {
            if (!await _chatRepository.UserIsParticipantAsync(userId, conversationId))
                return Result.Failure("You do not have access to this conversation.");

            var messages = await _chatRepository.GetMessagesAsync(conversationId);
            var readAtUtc = messages.Count > 0
                ? messages.Max(message => message.SentAtUtc)
                : DateTime.UtcNow;

            await _chatRepository.MarkConversationReadAsync(userId, conversationId, readAtUtc);
            await _applicationUnitOfWork.SaveChangesAsync();

            return Result.Success();
        }
    }
}

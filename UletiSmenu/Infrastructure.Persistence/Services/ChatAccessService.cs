using Core.DTOs;
using Core.Helpers;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Core.Services;
using CSharpFunctionalExtensions;

namespace Infrastructure.Persistence.Services
{
    public class ChatAccessService : IChatAccessService
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IJobPostRepository _jobPostRepository;
        private readonly IChatRepository _chatRepository;
        private readonly IApplicationUnitOfWork _applicationUnitOfWork;

        public ChatAccessService(
            IApplicationRepository applicationRepository,
            IJobPostRepository jobPostRepository,
            IChatRepository chatRepository,
            IApplicationUnitOfWork applicationUnitOfWork)
        {
            _applicationRepository = applicationRepository;
            _jobPostRepository = jobPostRepository;
            _chatRepository = chatRepository;
            _applicationUnitOfWork = applicationUnitOfWork;
        }

        public async Task<Result<ChatAccessEvaluationDTO>> EvaluateApplicationAccessAsync(
            Guid userId,
            Guid applicationId)
        {
            var application = await _applicationRepository.GetByIdAsync(applicationId);
            if (application == null)
                return Result.Failure<ChatAccessEvaluationDTO>("Application not found.");

            if (application.Status != ApplicationStatusEnum.Accepted)
                return Result.Failure<ChatAccessEvaluationDTO>("Chat is available only for accepted applications.");

            var jobPost = await _jobPostRepository.GetJobPostByIdAsync(application.JobPostId);
            if (jobPost == null)
                return Result.Failure<ChatAccessEvaluationDTO>("Job post not found.");

            if (!IsParticipant(userId, application, jobPost.EmployerId))
                return Result.Failure<ChatAccessEvaluationDTO>("You do not have access to this conversation.");

            var conversation = await _chatRepository.GetConversationByApplicationIdAsync(applicationId);
            if (conversation != null)
                await ArchiveConversationIfNeededAsync(conversation, jobPost.StartingDate, DateTime.UtcNow);

            return Result.Success(BuildEvaluation(application, conversation, jobPost.StartingDate));
        }

        public async Task<Result<ChatAccessEvaluationDTO>> EvaluateConversationAccessAsync(
            Guid userId,
            Guid conversationId,
            bool requireSend)
        {
            var conversation = await _chatRepository.GetConversationByIdAsync(conversationId);
            if (conversation == null)
                return Result.Failure<ChatAccessEvaluationDTO>("Conversation not found.");

            if (!await _chatRepository.UserIsParticipantAsync(userId, conversationId))
                return Result.Failure<ChatAccessEvaluationDTO>("You do not have access to this conversation.");

            var application = await _applicationRepository.GetByIdAsync(conversation.ApplicationId);
            if (application == null)
                return Result.Failure<ChatAccessEvaluationDTO>("Application not found.");

            if (application.Status != ApplicationStatusEnum.Accepted)
                return Result.Failure<ChatAccessEvaluationDTO>("Chat is available only for accepted applications.");

            var jobPost = await _jobPostRepository.GetJobPostByIdAsync(conversation.JobPostId);
            if (jobPost == null)
                return Result.Failure<ChatAccessEvaluationDTO>("Job post not found.");

            if (!IsParticipant(userId, application, jobPost.EmployerId))
                return Result.Failure<ChatAccessEvaluationDTO>("You do not have access to this conversation.");

            await ArchiveConversationIfNeededAsync(conversation, jobPost.StartingDate, DateTime.UtcNow);

            var evaluation = BuildEvaluation(application, conversation, jobPost.StartingDate);
            if (requireSend && !evaluation.CanSend)
                return Result.Failure<ChatAccessEvaluationDTO>(
                    "The shift has started or ended. Chat is available for viewing only.");

            return Result.Success(evaluation);
        }

        public async Task ArchiveConversationIfNeededAsync(
            Conversation conversation,
            DateTime shiftStartUtc,
            DateTime utcNow)
        {
            if (conversation.Status == ConversationStatusEnum.Archived)
                return;

            if (!ChatRules.ShouldArchiveChat(shiftStartUtc, utcNow))
                return;

            conversation.Archive(utcNow);
            _chatRepository.UpdateConversation(conversation);
            await _applicationUnitOfWork.SaveChangesAsync();
        }

        private static bool IsParticipant(Guid userId, Application application, Guid employerId)
        {
            return application.UserId == userId || employerId == userId;
        }

        private static ChatAccessEvaluationDTO BuildEvaluation(
            Application application,
            Conversation? conversation,
            DateTime shiftStartUtc)
        {
            var utcNow = DateTime.UtcNow;
            var shouldArchive = ChatRules.ShouldArchiveChat(shiftStartUtc, utcNow);
            var status = conversation?.Status
                ?? (shouldArchive ? ConversationStatusEnum.Archived : ConversationStatusEnum.Active);

            if (conversation == null && shouldArchive)
                status = ConversationStatusEnum.Archived;

            var isArchived = status == ConversationStatusEnum.Archived || shouldArchive;

            return new ChatAccessEvaluationDTO
            {
                Application = application,
                Conversation = conversation,
                ShiftStartUtc = shiftStartUtc,
                Status = isArchived ? ConversationStatusEnum.Archived : ConversationStatusEnum.Active,
                CanRead = true,
                CanSend = !isArchived
            };
        }
    }
}

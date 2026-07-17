using Core.DTOs;
using Core.Helpers;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Core.Services;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Services
{
    public class ApplicationService : IApplicationService
    {
        private const string ApplicationAcceptedNotificationType = "ApplicationAccepted";
        private const string ApplicationDeclinedNotificationType = "ApplicationDeclined";
        private const string ApplicationReceivedNotificationType = "ApplicationReceived";
        private readonly IApplicationRepository _applicationRepository;
        private readonly IJobPostRepository _jobPostRepository;
        private readonly IChatRepository _chatRepository;
        private readonly IUserRepository _userRepository;
        private readonly IApplicationUnitOfWork _applicationUnitOfWork;
        private readonly IRealtimeNotifier _realtimeNotifier;

        public ApplicationService(
            IApplicationRepository applicationRepository,
            IJobPostRepository jobPostRepository,
            IChatRepository chatRepository,
            IUserRepository userRepository,
            IApplicationUnitOfWork applicationUnitOfWork,
            IRealtimeNotifier realtimeNotifier)
        {
            _applicationRepository = applicationRepository;
            _jobPostRepository = jobPostRepository;
            _chatRepository = chatRepository;
            _userRepository = userRepository;
            _applicationUnitOfWork = applicationUnitOfWork;
            _realtimeNotifier = realtimeNotifier;
        }

        public async Task<Result> ApplyToJobPostAsync(Guid employeeId, Guid jobPostId)
        {
            var employee = await _userRepository.GetByIdAsync<Employee>(employeeId);
            if (employee == null)
                return Result.Failure("Employee not found.");

            var jobPost = await _jobPostRepository.GetJobPostByIdAsync(jobPostId);
            if (jobPost == null)
                return Result.Failure("Job post not found.");

            var canApplyResult = jobPost.ValidateCanApply(DateTime.UtcNow);
            if (canApplyResult.IsFailure)
                return Result.Failure(canApplyResult.Error);

            var alreadyApplied = await _applicationRepository.HasEmployeeAppliedAsync(employeeId, jobPostId);
            if (alreadyApplied)
                return Result.Success();

            var applicationResult = Application.Create(
                Guid.NewGuid(),
                employeeId,
                jobPostId,
                ApplicationStatusEnum.Applied,
                DateTime.UtcNow);

            if (applicationResult.IsFailure)
                return Result.Failure(applicationResult.Error);

            await _applicationUnitOfWork.BeginTransactionAsync();
            try
            {
                var application = applicationResult.Value;
                await _applicationRepository.AddAsync(application);

                var notification = CreateApplicationReceivedNotification(application, employee, jobPost);
                await _applicationUnitOfWork.Notifications.AddAsync(notification);

                await _applicationUnitOfWork.SaveChangesAsync();
                await _applicationUnitOfWork.CommitTransactionAsync();

                await NotifyApplicationReceivedAsync(notification);

                return Result.Success();
            }
            catch (DbUpdateException)
            {
                await _applicationUnitOfWork.RollbackTransactionAsync();

                if (await _applicationRepository.HasEmployeeAppliedAsync(employeeId, jobPostId))
                    return Result.Success();

                return Result.Failure("Failed to apply for job post.");
            }
            catch (Exception)
            {
                await _applicationUnitOfWork.RollbackTransactionAsync();
                return Result.Failure("Failed to apply for job post.");
            }
        }

        public async Task<Result<List<ApplicationApplicantDTO>>> GetApplicantsForEmployerJobPostAsync(
            Guid employerId,
            Guid jobPostId,
            bool includeContactInfo = false)
        {
            var jobPost = await _jobPostRepository.GetJobPostByIdAsync(jobPostId);
            if (jobPost == null)
                return Result.Failure<List<ApplicationApplicantDTO>>("Job post not found.");

            if (jobPost.EmployerId != employerId)
                return Result.Failure<List<ApplicationApplicantDTO>>("You can view applicants only for your own job posts.");

            await ExpirePendingApplicationsForJobPostIfNeededAsync(jobPost);

            var applicants = await _applicationRepository.GetApplicantsForJobPostAsync(jobPostId);

            if (!includeContactInfo)
            {
                foreach (var applicant in applicants)
                    CandidateContactPrivacy.RedactApplicantContactInfo(applicant);
            }

            return Result.Success(applicants);
        }

        public async Task<Result<List<EmployeeApplicationDTO>>> GetMyApplicationsAsync(Guid employeeId)
        {
            var employee = await _userRepository.GetByIdAsync<Employee>(employeeId);
            if (employee == null)
                return Result.Failure<List<EmployeeApplicationDTO>>("Employee not found.");

            await ExpirePendingApplicationsForEmployeeIfNeededAsync(employeeId);

            var applications = await _applicationRepository.GetEmployeeApplicationsAsync(employeeId);
            return Result.Success(applications);
        }

        public async Task<Result> UpdateApplicationStatusByEmployerAsync(Guid employerId, Guid applicationId, ApplicationStatusEnum newStatus)
        {
            var application = await _applicationRepository.GetByIdAsync(applicationId);
            if (application == null)
                return Result.Failure("Application not found.");

            var jobPost = await _jobPostRepository.GetJobPostByIdAsync(application.JobPostId);
            if (jobPost == null)
                return Result.Failure("Job post not found.");

            if (jobPost.EmployerId != employerId)
                return Result.Failure("You can update applications only for your own job posts.");

            await ExpirePendingApplicationsForJobPostIfNeededAsync(jobPost);

            application = await _applicationRepository.GetByIdAsync(applicationId);
            if (application == null)
                return Result.Failure("Application not found.");

            if (!jobPost.AcceptsEmployerApplicationDecisions(DateTime.UtcNow))
                return Result.Failure("Applications for this job post can no longer be accepted or rejected.");

            var statusResult = application.SetEmployerDecision(newStatus);
            if (statusResult.IsFailure)
                return Result.Failure(statusResult.Error);

            await CreateDecisionNotificationIfNeededAsync(application, jobPost, newStatus);
            await CreateConversationIfAcceptedAsync(application, jobPost, newStatus);
            await _applicationUnitOfWork.SaveChangesAsync();
            return Result.Success();
        }

        private async Task CreateConversationIfAcceptedAsync(
            Application application,
            JobPost jobPost,
            ApplicationStatusEnum newStatus)
        {
            if (newStatus != ApplicationStatusEnum.Accepted)
                return;

            var existingConversation = await _chatRepository.GetConversationByApplicationIdAsync(application.Id);
            if (existingConversation != null)
                return;

            var conversation = Conversation.Create(
                application.Id,
                jobPost.EmployerId,
                application.UserId,
                jobPost.Id,
                DateTime.UtcNow);

            await _chatRepository.AddConversationAsync(conversation);
        }

        public async Task<Result> CancelMyApplicationAsync(Guid employeeId, Guid applicationId)
        {
            var application = await _applicationRepository.GetByIdAsync(applicationId);
            if (application == null)
                return Result.Failure("Application not found.");

            if (application.UserId != employeeId)
                return Result.Failure("You can cancel only your own application.");

            var cancelResult = application.CancelByEmployee();
            if (cancelResult.IsFailure)
                return Result.Failure(cancelResult.Error);

            await _applicationUnitOfWork.SaveChangesAsync();
            return Result.Success();
        }

        private static Notification CreateApplicationReceivedNotification(
            Application application,
            Employee employee,
            JobPost jobPost)
        {
            var applicantName = $"{employee.FirstName} {employee.LastName}".Trim();
            var notificationType = $"{ApplicationReceivedNotificationType}:{application.Id}";
            var message = $"New application from {applicantName} for {jobPost.Title}.";

            return Notification.Create(
                jobPost.EmployerId,
                jobPost.EmployerId,
                jobPost.Id,
                notificationType,
                message);
        }

        private async Task NotifyApplicationReceivedAsync(Notification notification)
        {
            var unreadCount = await _applicationUnitOfWork.Notifications
                .GetUnreadCountByUserIdAsync(notification.UserId);

            var notificationDto = new UserNotificationDTO
            {
                Id = notification.Id,
                EmployerId = notification.EmployerId,
                JobPostId = notification.JobPostId,
                Type = notification.Type,
                Message = notification.Message,
                IsRead = notification.IsRead,
                CreatedAtUtc = notification.CreatedAtUtc
            };

            await _realtimeNotifier.NotifyNotificationAsync(notification.UserId, notificationDto, unreadCount);
        }

        private async Task CreateDecisionNotificationIfNeededAsync(
            Application application,
            JobPost jobPost,
            ApplicationStatusEnum newStatus)
        {
            if (newStatus != ApplicationStatusEnum.Accepted && newStatus != ApplicationStatusEnum.Denied)
                return;

            var notificationType = newStatus == ApplicationStatusEnum.Accepted
                ? ApplicationAcceptedNotificationType
                : ApplicationDeclinedNotificationType;

            var existingRecipientIds = await _applicationUnitOfWork.Notifications
                .GetRecipientIdsForJobPostAsync(jobPost.Id, notificationType);

            if (existingRecipientIds.Contains(application.UserId))
                return;

            var message = newStatus == ApplicationStatusEnum.Accepted
                ? $"Your application for {jobPost.Title} has been accepted."
                : $"Your application for {jobPost.Title} has been declined.";

            var notification = Notification.Create(
                application.UserId,
                jobPost.EmployerId,
                jobPost.Id,
                notificationType,
                message);

            await _applicationUnitOfWork.Notifications.AddRangeAsync(new[] { notification });
        }

        private async Task ExpirePendingApplicationsForJobPostIfNeededAsync(JobPost jobPost)
        {
            if (jobPost.AcceptsEmployerApplicationDecisions(DateTime.UtcNow))
                return;

            var pendingApplications = await _applicationRepository.GetPendingApplicationsByJobPostIdAsync(jobPost.Id);
            if (pendingApplications.Count == 0)
                return;

            var hasChanges = false;
            foreach (var pendingApplication in pendingApplications)
            {
                if (pendingApplication.ExpireDueToInactiveJobPost().IsSuccess)
                    hasChanges = true;
            }

            if (hasChanges)
                await _applicationUnitOfWork.SaveChangesAsync();
        }

        private async Task ExpirePendingApplicationsForEmployeeIfNeededAsync(Guid employeeId)
        {
            var pendingApplications = await _applicationRepository.GetPendingApplicationsForEmployeeAsync(employeeId);
            if (pendingApplications.Count == 0)
                return;

            var hasChanges = false;
            foreach (var pendingApplication in pendingApplications)
            {
                var jobPost = await _jobPostRepository.GetJobPostByIdAsync(pendingApplication.JobPostId);
                if (jobPost == null || jobPost.AcceptsEmployerApplicationDecisions(DateTime.UtcNow))
                    continue;

                if (pendingApplication.ExpireDueToInactiveJobPost().IsSuccess)
                    hasChanges = true;
            }

            if (hasChanges)
                await _applicationUnitOfWork.SaveChangesAsync();
        }
    }
}

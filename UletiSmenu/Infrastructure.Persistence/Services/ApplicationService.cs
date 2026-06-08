using Core.DTOs;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Core.Services;
using CSharpFunctionalExtensions;

namespace Infrastructure.Persistence.Services
{
    public class ApplicationService : IApplicationService
    {
        private const string ApplicationAcceptedNotificationType = "ApplicationAccepted";
        private const string ApplicationDeclinedNotificationType = "ApplicationDeclined";
        private readonly IApplicationRepository _applicationRepository;
        private readonly IJobPostRepository _jobPostRepository;
        private readonly IUserRepository _userRepository;
        private readonly IApplicationUnitOfWork _applicationUnitOfWork;

        public ApplicationService(
            IApplicationRepository applicationRepository,
            IJobPostRepository jobPostRepository,
            IUserRepository userRepository,
            IApplicationUnitOfWork applicationUnitOfWork)
        {
            _applicationRepository = applicationRepository;
            _jobPostRepository = jobPostRepository;
            _userRepository = userRepository;
            _applicationUnitOfWork = applicationUnitOfWork;
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
                return Result.Failure("You have already applied to this job post.");

            var applicantCount = await _applicationRepository.GetApplicantCountByJobPostAsync(jobPostId);
            var applicationResult = Application.Create(
                Guid.NewGuid(),
                employeeId,
                jobPostId,
                ApplicationStatusEnum.Applied,
                applicantCount + 1,
                DateTime.UtcNow);

            if (applicationResult.IsFailure)
                return Result.Failure(applicationResult.Error);

            await _applicationUnitOfWork.BeginTransactionAsync();
            try
            {
                await _applicationRepository.AddAsync(applicationResult.Value);
                await _applicationUnitOfWork.SaveChangesAsync();
                await _applicationUnitOfWork.CommitTransactionAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                await _applicationUnitOfWork.RollbackTransactionAsync();
                return Result.Failure($"Failed to apply for job post: {ex.Message}");
            }
        }

        public async Task<Result<List<ApplicationApplicantDTO>>> GetApplicantsForEmployerJobPostAsync(Guid employerId, Guid jobPostId)
        {
            var jobPost = await _jobPostRepository.GetJobPostByIdAsync(jobPostId);
            if (jobPost == null)
                return Result.Failure<List<ApplicationApplicantDTO>>("Job post not found.");

            if (jobPost.EmployerId != employerId)
                return Result.Failure<List<ApplicationApplicantDTO>>("You can view applicants only for your own job posts.");

            var applicants = await _applicationRepository.GetApplicantsForJobPostAsync(jobPostId);
            return Result.Success(applicants);
        }

        public async Task<Result<List<EmployeeApplicationDTO>>> GetMyApplicationsAsync(Guid employeeId)
        {
            var employee = await _userRepository.GetByIdAsync<Employee>(employeeId);
            if (employee == null)
                return Result.Failure<List<EmployeeApplicationDTO>>("Employee not found.");

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

            var statusResult = application.SetEmployerDecision(newStatus);
            if (statusResult.IsFailure)
                return Result.Failure(statusResult.Error);

            await CreateDecisionNotificationIfNeededAsync(application, jobPost, newStatus);
            await _applicationUnitOfWork.SaveChangesAsync();
            return Result.Success();
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
    }
}

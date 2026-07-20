using Core.DTOs;
using Core.Interfaces;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Core.Services;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Services
{
    public class JobPostService : IJobPostService
    {
        private const string NewFavouriteRestaurantJobPostType = "NewFavouriteRestaurantJobPost";
        private const string CompleteEmployerProfileMessage = "Da biste objavili oglas, prvo popunite profil poslodavca.";
        private readonly IJobPostRepository _jobPostRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRestaurantLocationRepository _restaurantLocationRepository;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IApplicationUnitOfWork _applicationUnitOfWork;
        private readonly IBillingService _billingService;
        private readonly IEmailService _emailService;
        private readonly ILogger<JobPostService> _logger;

        public JobPostService(
            IJobPostRepository jobPostRepository,
            IUserRepository userRepository,
            IRestaurantLocationRepository restaurantLocationRepository,
            IApplicationRepository applicationRepository,
            IApplicationUnitOfWork applicationUnitOfWork,
            IBillingService billingService,
            IEmailService emailService,
            ILogger<JobPostService> logger)
        {
            _jobPostRepository = jobPostRepository;
            _userRepository = userRepository;
            _restaurantLocationRepository = restaurantLocationRepository;
            _applicationRepository = applicationRepository;
            _applicationUnitOfWork = applicationUnitOfWork;
            _billingService = billingService;
            _emailService = emailService;
            _logger = logger;
        }
        public async Task<Result> CreateJobPostAsync(JobPost jobPost)
        {
            var isActivePost = jobPost.Status == JobStatusEnum.Active;

            if (isActivePost)
            {
                var profileValidation = await ValidateEmployerProfileCompleteAsync(jobPost.EmployerId);
                if (profileValidation.IsFailure)
                    return profileValidation;

                var billingValidation = await _billingService.ValidateEmployerCanCreatePostAsync(jobPost.EmployerId);
                if (billingValidation.IsFailure)
                    return Result.Failure(billingValidation.Error);
            }

            if (!jobPost.RestaurantLocationId.HasValue)
                return Result.Failure("Restaurant location must be selected.");

            var location = await _restaurantLocationRepository.GetByIdAsync(jobPost.RestaurantLocationId.Value);
            if (location == null)
                return Result.Failure("Selected restaurant location was not found.");

            if (location.EmployerId != jobPost.EmployerId)
                return Result.Failure("Selected location does not belong to this brand account.");

            await _applicationUnitOfWork.BeginTransactionAsync();

            try
            {
                await _jobPostRepository.AddAsync(jobPost);

                if (isActivePost)
                {
                    var creditResult = await _billingService.OnJobPostCreatedAsync(jobPost.EmployerId, jobPost.Id);
                    if (creditResult.IsFailure)
                    {
                        await _applicationUnitOfWork.RollbackTransactionAsync();
                        return Result.Failure(creditResult.Error);
                    }

                    await CreateInAppNotificationsAsync(jobPost);
                }

                await _applicationUnitOfWork.SaveChangesAsync();
                await _applicationUnitOfWork.CommitTransactionAsync();

                if (isActivePost)
                    await NotifyFollowersAsync(jobPost);

                return Result.Success("Job post created successfully.");

            }
            catch (Exception ex)
            {
                await _applicationUnitOfWork.RollbackTransactionAsync();
                _logger.LogError(
                    ex,
                    "Job post creation failed. JobPostId: {JobPostId}, EmployerId: {EmployerId}",
                    jobPost.Id,
                    jobPost.EmployerId);
                return Result.Failure("Job post creation failed.");
            }
        }

        public async Task<IEnumerable<JobPost>> GetVisibleJobPostsAsync(string? sortBy = null, string? sortDirection = null)
        {
            return await _jobPostRepository.GetVisibleJobPostsAsync(DateTime.UtcNow, sortBy, sortDirection);
        }

        public async Task<PagedResultDTO<JobPost>> GetVisibleJobPostsPagedAsync(
            int page,
            int pageSize,
            string? sortBy = null,
            string? sortDirection = null,
            string? city = null,
            Guid? restaurantLocationId = null,
            string? position = null,
            IReadOnlyList<string>? positions = null,
            int? minSalary = null,
            int? maxSalary = null,
            DateTime? shiftDateFrom = null,
            DateTime? shiftDateTo = null,
            Guid? employeeId = null,
            string? applicationFilter = null,
            bool? favouritesOnly = null)
        {
            var safePage = page < 1 ? 1 : page;
            var safePageSize = pageSize < 1 ? 6 : Math.Min(pageSize, 50);

            var (items, totalCount) = await _jobPostRepository.GetVisibleJobPostsPagedAsync(
                DateTime.UtcNow,
                safePage,
                safePageSize,
                sortBy,
                sortDirection,
                city,
                restaurantLocationId,
                position,
                positions,
                minSalary,
                maxSalary,
                shiftDateFrom,
                shiftDateTo,
                employeeId,
                applicationFilter,
                favouritesOnly);

            return new PagedResultDTO<JobPost>
            {
                Items = items,
                TotalCount = totalCount,
                Page = safePage,
                PageSize = safePageSize
            };
        }

        public async Task<VisibleJobPostFilterOptionsDTO> GetVisibleJobPostFilterOptionsAsync(string? city = null)
        {
            return await _jobPostRepository.GetVisibleJobPostFilterOptionsAsync(DateTime.UtcNow, city);
        }

        public async Task<List<JobPost>> GetCandidateRecommendedJobPostsAsync(Guid employeeId, string? city, int pageSize = 3)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return new List<JobPost>();
            }

            var safePageSize = pageSize < 1 ? 3 : Math.Min(pageSize, 12);

            return await _jobPostRepository.GetCandidateRecommendedJobPostsAsync(
                employeeId,
                city,
                DateTime.UtcNow,
                safePageSize);
        }

        public async Task<JobPost?> GetVisibleJobPostByIdAsync(Guid jobPostId)
        {
            return await _jobPostRepository.GetVisibleJobPostByIdAsync(jobPostId, DateTime.UtcNow);
        }

        public async Task<IEnumerable<JobPost>> GetMyJobPostsAsync(Guid employerId)
        {
            return await _jobPostRepository.GetAllByEmployerIdAsync(employerId);
        }

        public async Task<List<string>> GetMyJobPostPositionsAsync(Guid employerId)
        {
            return await _jobPostRepository.GetDistinctPositionsByEmployerIdAsync(employerId);
        }

        public async Task<PagedResultDTO<JobPost>> GetMyJobPostsPagedAsync(
            Guid employerId,
            int page,
            int pageSize,
            string? position = null,
            string? status = null,
            string? lifecycle = null,
            string? sortBy = null,
            string? sortDirection = null,
            bool? hasApplicants = null,
            string? city = null,
            Guid? restaurantLocationId = null,
            int? minSalary = null,
            int? maxSalary = null)
        {
            var safePage = page < 1 ? 1 : page;
            var safePageSize = pageSize < 1 ? 6 : Math.Min(pageSize, 50);

            var (items, totalCount) = await _jobPostRepository.GetByEmployerIdPagedAsync(
                employerId,
                DateTime.UtcNow,
                safePage,
                safePageSize,
                position,
                status,
                lifecycle,
                sortBy,
                sortDirection,
                hasApplicants,
                city,
                restaurantLocationId,
                minSalary,
                maxSalary);

            return new PagedResultDTO<JobPost>
            {
                Items = items,
                TotalCount = totalCount,
                Page = safePage,
                PageSize = safePageSize
            };
        }

        public async Task<EmployerDashboardSummaryDTO> GetEmployerDashboardSummaryAsync(Guid employerId)
        {
            return await _jobPostRepository.GetEmployerDashboardSummaryAsync(employerId);
        }

        public async Task<Result> UpdateJobPostAsync(
            Guid employerId,
            Guid jobPostId,
            string title,
            string description,
            string position,
            string status,
            int salary,
            DateTime startingDate,
            DateTime? visibleUntil,
            Guid restaurantLocationId)
        {
            var jobPost = await _jobPostRepository.GetJobPostByIdAsync(jobPostId);
            if (jobPost == null)
                return Result.Failure("Job post not found.");

            if (jobPost.EmployerId != employerId)
                return Result.Failure("You can edit only your own job posts.");

            var location = await _restaurantLocationRepository.GetByIdAsync(restaurantLocationId);
            if (location == null)
                return Result.Failure("Selected restaurant location was not found.");

            if (location.EmployerId != employerId)
                return Result.Failure("Selected location does not belong to this brand account.");

            var parseStatusOk = Enum.TryParse<JobStatusEnum>(status, out var parsedStatus);
            if (!parseStatusOk)
                return Result.Failure("Invalid job post status.");

            var wasActive = jobPost.Status == JobStatusEnum.Active;
            var willBeActive = parsedStatus == JobStatusEnum.Active;

            if (willBeActive)
            {
                var profileValidation = await ValidateEmployerProfileCompleteAsync(employerId);
                if (profileValidation.IsFailure)
                    return profileValidation;

                if (!wasActive)
                {
                    var billingValidation = await _billingService.ValidateEmployerCanCreatePostAsync(employerId);
                    if (billingValidation.IsFailure)
                        return Result.Failure(billingValidation.Error);
                }
            }

            var updateResult = jobPost.Update(
                title,
                description,
                parsedStatus,
                startingDate,
                visibleUntil,
                restaurantLocationId,
                salary,
                position);

            if (updateResult.IsFailure)
                return Result.Failure(updateResult.Error);

            if (willBeActive && !wasActive)
            {
                var creditResult = await _billingService.OnJobPostCreatedAsync(employerId, jobPostId);
                if (creditResult.IsFailure)
                    return Result.Failure(creditResult.Error);
            }

            await _applicationUnitOfWork.SaveChangesAsync();
            return Result.Success("Job post updated successfully.");
        }

        public async Task<Result<JobPostApplicationStatsDTO>> GetMyJobPostApplicationStatsAsync(
            Guid employerId,
            Guid jobPostId)
        {
            var jobPost = await _jobPostRepository.GetJobPostByIdAsync(jobPostId);
            if (jobPost == null)
                return Result.Failure<JobPostApplicationStatsDTO>("Job post not found.");

            if (jobPost.EmployerId != employerId)
                return Result.Failure<JobPostApplicationStatsDTO>("You can view only your own job posts.");

            return Result.Success(await _applicationRepository.GetApplicationStatsByJobPostIdAsync(jobPostId));
        }

        public async Task<Result<JobPost>> DuplicateMyJobPostAsync(Guid employerId, Guid jobPostId)
        {
            var source = await _jobPostRepository.GetJobPostByIdAsync(jobPostId);
            if (source == null)
                return Result.Failure<JobPost>("Job post not found.");

            if (source.EmployerId != employerId)
                return Result.Failure<JobPost>("You can duplicate only your own job posts.");

            if (!source.RestaurantLocationId.HasValue)
                return Result.Failure<JobPost>("Restaurant location must be selected.");

            var utcNow = DateTime.UtcNow;
            var startingDate = ResolveDuplicateStartingDate(source.StartingDate, utcNow);
            var visibleUntil = source.VisibleUntil < startingDate
                ? startingDate
                : source.VisibleUntil > startingDate.AddHours(1)
                    ? startingDate.AddHours(1)
                    : source.VisibleUntil;

            var duplicateResult = JobPost.Create(
                Guid.NewGuid(),
                source.Title,
                source.Description,
                JobStatusEnum.Draft,
                startingDate,
                visibleUntil,
                employerId,
                source.RestaurantLocationId.Value,
                source.Salary,
                source.Position);

            if (duplicateResult.IsFailure)
                return Result.Failure<JobPost>(duplicateResult.Error);

            var createResult = await CreateJobPostAsync(duplicateResult.Value);
            if (createResult.IsFailure)
                return Result.Failure<JobPost>(createResult.Error);

            return Result.Success(duplicateResult.Value);
        }

        public async Task<Result> ArchiveMyJobPostAsync(Guid employerId, Guid jobPostId)
        {
            var jobPost = await _jobPostRepository.GetJobPostByIdAsync(jobPostId);
            if (jobPost == null)
                return Result.Failure("Job post not found.");

            if (jobPost.EmployerId != employerId)
                return Result.Failure("You can archive only your own job posts.");

            if (jobPost.Status == JobStatusEnum.Cancelled)
                return Result.Success("Job post is already archived.");

            var archiveResult = jobPost.Archive();
            if (archiveResult.IsFailure)
                return Result.Failure(archiveResult.Error);

            await _applicationUnitOfWork.SaveChangesAsync();
            return Result.Success("Job post archived successfully.");
        }

        public async Task<Result> DeleteMyJobPostAsync(Guid employerId, Guid jobPostId)
        {
            var jobPost = await _jobPostRepository.GetJobPostByIdAsync(jobPostId);
            if (jobPost == null)
                return Result.Failure("Job post not found.");

            if (jobPost.EmployerId != employerId)
                return Result.Failure("You can delete only your own job posts.");

            var applicantCount = await _applicationRepository.GetApplicantCountByJobPostAsync(jobPostId);
            if (applicantCount > 0)
                return Result.Failure("Cannot delete a job post that has applications. Archive it instead.");

            await _jobPostRepository.DeleteJobPostAsync(jobPost);
            await _applicationUnitOfWork.SaveChangesAsync();
            return Result.Success("Job post deleted successfully.");
        }

        private async Task<Result> ValidateEmployerProfileCompleteAsync(Guid employerId)
        {
            var locations = await _restaurantLocationRepository.GetByEmployerIdAsync(employerId);
            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);

            if (employer == null || !employer.HasCompletedRequiredProfile() || locations.Count == 0)
                return Result.Failure(CompleteEmployerProfileMessage);

            return Result.Success();
        }

        private static DateTime ResolveDuplicateStartingDate(DateTime sourceStartingDate, DateTime utcNow)
        {
            if (sourceStartingDate > utcNow.AddHours(1))
                return sourceStartingDate;

            var nextWeek = utcNow.Date.AddDays(7).Add(sourceStartingDate.TimeOfDay);
            if (nextWeek > utcNow.AddHours(1))
                return nextWeek;

            return utcNow.AddDays(1);
        }

        private async Task CreateInAppNotificationsAsync(JobPost jobPost)
        {
            var followerIds = await _applicationUnitOfWork.Favourites.GetEmployeeIdsByEmployerIdAsync(jobPost.EmployerId);
            if (followerIds.Count == 0)
                return;

            var existingRecipientIds = await _applicationUnitOfWork.Notifications
                .GetRecipientIdsForJobPostAsync(jobPost.Id, NewFavouriteRestaurantJobPostType);

            var recipients = followerIds
                .Distinct()
                .Where(employeeId => !existingRecipientIds.Contains(employeeId))
                .ToList();

            if (recipients.Count == 0)
                return;

            var notifications = recipients.Select(employeeId =>
                Notification.Create(
                    employeeId,
                    jobPost.EmployerId,
                    jobPost.Id,
                    NewFavouriteRestaurantJobPostType,
                    $"New job post from a favourite restaurant: {jobPost.Title}"))
                .ToList();

            await _applicationUnitOfWork.Notifications.AddRangeAsync(notifications);
        }

        private async Task NotifyFollowersAsync(JobPost jobPost)
        {
            try
            {
                var followerEmails = await _applicationUnitOfWork.Favourites.GetFollowerEmailsByEmployerIdAsync(jobPost.EmployerId);

                foreach (var email in followerEmails.Distinct())
                {
                    await _emailService.SendEmailAsync(
                        email,
                        "New restaurant shift available",
                        $"A restaurant you follow just posted a new shift: <b>{jobPost.Title}</b>.");
                }
            }
            catch (Exception ex)
            {
                // Notification failure should not roll back job post creation.
                _logger.LogWarning(ex, "Job post created but follower notification failed for employer {EmployerId}", jobPost.EmployerId);
            }
        }
    }
}

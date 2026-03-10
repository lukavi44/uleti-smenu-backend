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
        private readonly IJobPostRepository _jobPostRepository;
        private readonly IRestaurantLocationRepository _restaurantLocationRepository;
        private readonly IApplicationUnitOfWork _applicationUnitOfWork;
        private readonly IBillingService _billingService;
        private readonly IEmailService _emailService;
        private readonly ILogger<JobPostService> _logger;

        public JobPostService(
            IJobPostRepository jobPostRepository,
            IRestaurantLocationRepository restaurantLocationRepository,
            IApplicationUnitOfWork applicationUnitOfWork,
            IBillingService billingService,
            IEmailService emailService,
            ILogger<JobPostService> logger)
        {
            _jobPostRepository = jobPostRepository;
            _restaurantLocationRepository = restaurantLocationRepository;
            _applicationUnitOfWork = applicationUnitOfWork;
            _billingService = billingService;
            _emailService = emailService;
            _logger = logger;
        }
        public async Task<Result> CreateJobPostAsync(JobPost jobPost)
        {
            var billingValidation = await _billingService.ValidateEmployerCanCreatePostAsync(jobPost.EmployerId);
            if (billingValidation.IsFailure)
                return Result.Failure(billingValidation.Error);

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
                await _applicationUnitOfWork.SaveChangesAsync();
                await _applicationUnitOfWork.CommitTransactionAsync();
                await NotifyFollowersAsync(jobPost);

                return Result.Success("Job post created successfully.");

            }
            catch (Exception ex)
            {
                await _applicationUnitOfWork.RollbackTransactionAsync();
                return Result.Failure($"Job post creation failed: {ex.Message}");
            }
        }

        public async Task<IEnumerable<JobPost>> GetVisibleJobPostsAsync()
        {
            return await _jobPostRepository.GetVisibleJobPostsAsync(DateTime.UtcNow);
        }

        public async Task<IEnumerable<JobPost>> GetMyJobPostsAsync(Guid employerId)
        {
            return await _jobPostRepository.GetAllByEmployerIdAsync(employerId);
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

            await _applicationUnitOfWork.SaveChangesAsync();
            return Result.Success("Job post updated successfully.");
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

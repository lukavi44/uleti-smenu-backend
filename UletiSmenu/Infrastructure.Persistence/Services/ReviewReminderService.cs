using System.Collections.Concurrent;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Core.Services;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Services
{
    public class ReviewReminderService : IReviewReminderService
    {
        public const string ReviewReminderType = "ReviewReminder";

        private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> UserSyncLocks = new();

        private readonly IReviewService _reviewService;
        private readonly INotificationRepository _notificationRepository;
        private readonly IApplicationUnitOfWork _applicationUnitOfWork;

        public ReviewReminderService(
            IReviewService reviewService,
            INotificationRepository notificationRepository,
            IApplicationUnitOfWork applicationUnitOfWork)
        {
            _reviewService = reviewService;
            _notificationRepository = notificationRepository;
            _applicationUnitOfWork = applicationUnitOfWork;
        }

        public async Task<Result> SyncReviewRemindersAsync(Guid userId, string role)
        {
            if (role != UserRolesEnum.Employee.ToString() && role != UserRolesEnum.Employer.ToString())
                return Result.Success();

            var userLock = UserSyncLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
            await userLock.WaitAsync();

            try
            {
                var pendingResult = await _reviewService.GetMyPendingReviewsAsync(userId, role);
                if (pendingResult.IsFailure)
                    return Result.Failure(pendingResult.Error);

                var existingJobPostIds = await _notificationRepository.GetJobPostIdsByTypeAsync(userId, ReviewReminderType);

                foreach (var pendingReview in pendingResult.Value)
                {
                    if (existingJobPostIds.Contains(pendingReview.JobPostId))
                        continue;

                    var employerId = role == UserRolesEnum.Employee.ToString()
                        ? pendingReview.RevieweeId
                        : userId;

                    var notification = Notification.Create(
                        userId,
                        employerId,
                        pendingReview.JobPostId,
                        ReviewReminderType,
                        $"Leave a review for {pendingReview.RevieweeName}: {pendingReview.JobPostTitle}");

                    await _notificationRepository.AddAsync(notification);
                    existingJobPostIds.Add(pendingReview.JobPostId);
                }

                try
                {
                    await _applicationUnitOfWork.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // Another concurrent request may have inserted the same reminder first.
                }

                return Result.Success();
            }
            finally
            {
                userLock.Release();
            }
        }
    }
}

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
        private readonly IRealtimeNotifier _realtimeNotifier;
        private readonly IApplicationUnitOfWork _applicationUnitOfWork;

        public ReviewReminderService(
            IReviewService reviewService,
            INotificationRepository notificationRepository,
            IRealtimeNotifier realtimeNotifier,
            IApplicationUnitOfWork applicationUnitOfWork)
        {
            _reviewService = reviewService;
            _notificationRepository = notificationRepository;
            _realtimeNotifier = realtimeNotifier;
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
                var createdNotifications = new List<Notification>();

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
                    createdNotifications.Add(notification);
                    existingJobPostIds.Add(pendingReview.JobPostId);
                }

                if (createdNotifications.Count == 0)
                    return Result.Success();

                try
                {
                    await _applicationUnitOfWork.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // Another concurrent request may have inserted the same reminder first.
                    return Result.Success();
                }

                var unreadCount = await _notificationRepository.GetUnreadCountByUserIdAsync(userId);
                foreach (var notification in createdNotifications)
                {
                    await _realtimeNotifier.NotifyNotificationAsync(
                        userId,
                        MapNotification(notification),
                        unreadCount);
                }

                return Result.Success();
            }
            finally
            {
                userLock.Release();
            }
        }

        private static Core.DTOs.UserNotificationDTO MapNotification(Notification notification)
        {
            return new Core.DTOs.UserNotificationDTO
            {
                Id = notification.Id,
                EmployerId = notification.EmployerId,
                JobPostId = notification.JobPostId,
                Type = notification.Type,
                Message = notification.Message,
                IsRead = notification.IsRead,
                CreatedAtUtc = notification.CreatedAtUtc
            };
        }
    }
}

using Core.DTOs;
using Core.Models.Entities;
using Core.Repositories;
using Core.Services;
using CSharpFunctionalExtensions;

namespace Infrastructure.Persistence.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IApplicationUnitOfWork _applicationUnitOfWork;

        public NotificationService(
            INotificationRepository notificationRepository,
            IUserRepository userRepository,
            IApplicationUnitOfWork applicationUnitOfWork)
        {
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
            _applicationUnitOfWork = applicationUnitOfWork;
        }

        public async Task<Result<List<UserNotificationDTO>>> GetMyNotificationsAsync(Guid userId)
        {
            var employee = await _userRepository.GetByIdAsync<Employee>(userId);
            if (employee == null)
                return Result.Failure<List<UserNotificationDTO>>("Employee not found.");

            var notifications = await _notificationRepository.GetByUserIdAsync(userId);

            var response = notifications.Select(n => new UserNotificationDTO
            {
                Id = n.Id,
                EmployerId = n.EmployerId,
                JobPostId = n.JobPostId,
                Type = n.Type,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAtUtc = n.CreatedAtUtc
            }).ToList();

            return Result.Success(response);
        }

        public async Task<Result<int>> GetMyUnreadCountAsync(Guid userId)
        {
            var employee = await _userRepository.GetByIdAsync<Employee>(userId);
            if (employee == null)
                return Result.Failure<int>("Employee not found.");

            var count = await _notificationRepository.GetUnreadCountByUserIdAsync(userId);
            return Result.Success(count);
        }

        public async Task<Result> MarkAsReadAsync(Guid userId, Guid notificationId)
        {
            var employee = await _userRepository.GetByIdAsync<Employee>(userId);
            if (employee == null)
                return Result.Failure("Employee not found.");

            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification == null)
                return Result.Failure("Notification not found.");

            if (notification.UserId != userId)
                return Result.Failure("You can update only your own notifications.");

            notification.MarkAsRead();
            await _applicationUnitOfWork.SaveChangesAsync();

            return Result.Success();
        }
    }
}

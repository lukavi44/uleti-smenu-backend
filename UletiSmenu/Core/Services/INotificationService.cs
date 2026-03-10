using Core.DTOs;
using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface INotificationService
    {
        Task<Result<List<UserNotificationDTO>>> GetMyNotificationsAsync(Guid userId);
        Task<Result<int>> GetMyUnreadCountAsync(Guid userId);
        Task<Result> MarkAsReadAsync(Guid userId, Guid notificationId);
    }
}

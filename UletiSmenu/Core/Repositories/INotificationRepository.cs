using Core.Models.Entities;

namespace Core.Repositories
{
    public interface INotificationRepository
    {
        Task AddRangeAsync(IEnumerable<Notification> notifications);
        Task AddAsync(Notification notification);
        Task<List<Notification>> GetByUserIdAsync(Guid userId);
        Task<int> GetUnreadCountByUserIdAsync(Guid userId);
        Task<Notification?> GetByIdAsync(Guid notificationId);
        Task<HashSet<Guid>> GetRecipientIdsForJobPostAsync(Guid jobPostId, string type);
        Task<bool> ExistsAsync(Guid userId, Guid jobPostId, string type);
        Task<HashSet<Guid>> GetJobPostIdsByTypeAsync(Guid userId, string type);
        void Delete(Notification notification);
    }
}

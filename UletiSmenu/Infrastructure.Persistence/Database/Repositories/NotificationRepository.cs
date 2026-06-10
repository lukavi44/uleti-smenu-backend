using Core.Models.Entities;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Database.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _context;

        public NotificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<Notification> notifications)
        {
            await _context.Set<Notification>().AddRangeAsync(notifications);
        }

        public async Task AddAsync(Notification notification)
        {
            await _context.Set<Notification>().AddAsync(notification);
        }

        public async Task<List<Notification>> GetByUserIdAsync(Guid userId)
        {
            var notifications = await _context.Set<Notification>()
                .Where(n => n.UserId == userId && !n.IsDismissed)
                .OrderByDescending(n => n.CreatedAtUtc)
                .ToListAsync();

            return notifications
                .GroupBy(notification => new { notification.JobPostId, notification.Type })
                .Select(group => group.First())
                .OrderByDescending(notification => notification.CreatedAtUtc)
                .ToList();
        }

        public async Task<int> GetUnreadCountByUserIdAsync(Guid userId)
        {
            return await _context.Set<Notification>()
                .CountAsync(n => n.UserId == userId && !n.IsDismissed && !n.IsRead);
        }

        public async Task<Notification?> GetByIdAsync(Guid notificationId)
        {
            return await _context.Set<Notification>()
                .FirstOrDefaultAsync(n => n.Id == notificationId);
        }

        public async Task<HashSet<Guid>> GetRecipientIdsForJobPostAsync(Guid jobPostId, string type)
        {
            var userIds = await _context.Set<Notification>()
                .Where(n => n.JobPostId == jobPostId && n.Type == type)
                .Select(n => n.UserId)
                .ToListAsync();

            return userIds.ToHashSet();
        }

        public async Task<bool> ExistsAsync(Guid userId, Guid jobPostId, string type)
        {
            return await _context.Set<Notification>()
                .AnyAsync(n => n.UserId == userId && n.JobPostId == jobPostId && n.Type == type);
        }

        public async Task<HashSet<Guid>> GetJobPostIdsByTypeAsync(Guid userId, string type)
        {
            var jobPostIds = await _context.Set<Notification>()
                .Where(n => n.UserId == userId && n.Type == type)
                .Select(n => n.JobPostId)
                .ToListAsync();

            return jobPostIds.ToHashSet();
        }

        public void Delete(Notification notification)
        {
            _context.Set<Notification>().Remove(notification);
        }
    }
}

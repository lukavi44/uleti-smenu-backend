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

        public async Task<List<Notification>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Set<Notification>()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountByUserIdAsync(Guid userId)
        {
            return await _context.Set<Notification>()
                .CountAsync(n => n.UserId == userId && !n.IsRead);
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

        public void Delete(Notification notification)
        {
            _context.Set<Notification>().Remove(notification);
        }
    }
}

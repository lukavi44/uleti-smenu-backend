using Core.JobPosts;
using Core.Models.Enums;
using Core.Services;
using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Persistence.Services
{
    public class JobPostLifecycleService : IJobPostLifecycleService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOptions<JobPostLifecycleSettings> _settings;
        private readonly ILogger<JobPostLifecycleService> _logger;

        public JobPostLifecycleService(
            ApplicationDbContext context,
            IOptions<JobPostLifecycleSettings> settings,
            ILogger<JobPostLifecycleService> logger)
        {
            _context = context;
            _settings = settings;
            _logger = logger;
        }

        public async Task<int> ExpireStaleActivePostsAsync(CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;
            var batchSize = Math.Clamp(_settings.Value.BatchSize, 1, 2000);

            var stalePosts = await _context.JobPosts
                .Where(post =>
                    post.Status == JobStatusEnum.Active
                    && post.StartingDate.AddHours(1) < utcNow)
                .OrderBy(post => post.StartingDate)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (stalePosts.Count == 0)
                return 0;

            var expiredPostIds = new List<Guid>(stalePosts.Count);
            foreach (var post in stalePosts)
            {
                var result = post.ExpireDueToElapsedWindow(utcNow);
                if (result.IsSuccess && post.Status == JobStatusEnum.Expired)
                    expiredPostIds.Add(post.Id);
            }

            if (expiredPostIds.Count == 0)
                return 0;

            var pendingApplications = await _context.Applications
                .Where(application =>
                    expiredPostIds.Contains(application.JobPostId)
                    && application.Status == ApplicationStatusEnum.Applied)
                .ToListAsync(cancellationToken);

            var expiredApplications = 0;
            foreach (var application in pendingApplications)
            {
                if (application.ExpireDueToInactiveJobPost().IsSuccess)
                    expiredApplications++;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Expired {PostCount} stale Active job post(s) and {ApplicationCount} pending application(s).",
                expiredPostIds.Count,
                expiredApplications);

            return expiredPostIds.Count;
        }
    }
}

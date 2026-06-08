using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Database.Repositories
{
    public class JobPostRepository : Repository<JobPost>, IJobPostRepository
    {
        public JobPostRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task AddJobPostAsync(JobPost jobPost)
        {
            await _context.JobPosts.AddAsync(jobPost);
        }

        public async Task DeleteJobPostAsync(JobPost jobPost)
        {
            _context.JobPosts.Remove(jobPost);
        }

        public async Task<IEnumerable<JobPost>> GetAllByEmployerIdAsync(Guid employerId)
        {
            return await _context.JobPosts
              .Include(jp => jp.Employer)
              .Include(jp => jp.RestaurantLocation)
              .Where(jp => jp.EmployerId == employerId)
              .ToListAsync();
        }

        public async Task<List<string>> GetDistinctPositionsByEmployerIdAsync(Guid employerId)
        {
            return await _context.JobPosts
                .Where(jp => jp.EmployerId == employerId)
                .Select(jp => jp.Position)
                .Distinct()
                .OrderBy(position => position)
                .ToListAsync();
        }

        public async Task<(List<JobPost> Items, int TotalCount)> GetByEmployerIdPagedAsync(
            Guid employerId,
            DateTime utcNow,
            int page,
            int pageSize,
            string? position = null,
            string? status = null,
            string? lifecycle = null,
            string? sortBy = null,
            string? sortDirection = null)
        {
            var query = _context.JobPosts
                .Include(jp => jp.Employer)
                .Include(jp => jp.RestaurantLocation)
                .Where(jp => jp.EmployerId == employerId);

            if (!string.IsNullOrWhiteSpace(position))
            {
                var normalizedPosition = position.Trim();
                query = query.Where(jp => jp.Position == normalizedPosition);
            }

            if (!string.IsNullOrWhiteSpace(status)
                && Enum.TryParse<JobStatusEnum>(status, true, out var parsedStatus))
            {
                query = query.Where(jp => jp.Status == parsedStatus);
            }

            var normalizedLifecycle = lifecycle?.Trim().ToLowerInvariant();
            if (normalizedLifecycle == "active")
            {
                query = query.Where(jp =>
                    jp.Status != JobStatusEnum.Cancelled
                    && jp.Status != JobStatusEnum.Completed
                    && jp.Status != JobStatusEnum.Expired
                    && jp.StartingDate.AddHours(1) >= utcNow);
            }
            else if (normalizedLifecycle == "archived")
            {
                query = query.Where(jp =>
                    jp.Status == JobStatusEnum.Cancelled
                    || jp.Status == JobStatusEnum.Completed
                    || jp.Status == JobStatusEnum.Expired
                    || jp.StartingDate.AddHours(1) < utcNow);
            }

            var isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

            query = normalizedSortBy switch
            {
                "position" => isAscending
                    ? query.OrderBy(jp => jp.Position).ThenByDescending(jp => jp.CreatedAtUtc)
                    : query.OrderByDescending(jp => jp.Position).ThenByDescending(jp => jp.CreatedAtUtc),
                "startingdate" => isAscending
                    ? query.OrderBy(jp => jp.StartingDate).ThenByDescending(jp => jp.CreatedAtUtc)
                    : query.OrderByDescending(jp => jp.StartingDate).ThenByDescending(jp => jp.CreatedAtUtc),
                _ => isAscending
                    ? query.OrderBy(jp => jp.CreatedAtUtc).ThenBy(jp => jp.StartingDate)
                    : query.OrderByDescending(jp => jp.CreatedAtUtc).ThenByDescending(jp => jp.StartingDate)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<JobPost>> GetAllJobPostsAsync()
        {
            var response = await _context.JobPosts
            .Include(jp => jp.Employer)
            .Include(jp => jp.RestaurantLocation)
            .ToListAsync();

            return response;
        }

        public async Task<IEnumerable<JobPost>> GetVisibleJobPostsAsync(DateTime utcNow, string? sortBy = null, string? sortDirection = null)
        {
            var query = _context.JobPosts
                .Include(jp => jp.Employer)
                .Include(jp => jp.RestaurantLocation)
                .Where(jp =>
                    jp.Status == Core.Models.Enums.JobStatusEnum.Active
                    && (jp.VisibleUntil >= utcNow || jp.StartingDate.AddHours(1) >= utcNow));

            var isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

            query = normalizedSortBy switch
            {
                "salary" => isAscending
                    ? query.OrderBy(jp => jp.Salary).ThenBy(jp => jp.CreatedAtUtc)
                    : query.OrderByDescending(jp => jp.Salary).ThenByDescending(jp => jp.CreatedAtUtc),
                _ => isAscending
                    ? query.OrderBy(jp => jp.CreatedAtUtc).ThenBy(jp => jp.StartingDate)
                    : query.OrderByDescending(jp => jp.CreatedAtUtc).ThenByDescending(jp => jp.StartingDate)
            };

            return await query.ToListAsync();
        }

        public async Task<JobPost?> GetJobPostByIdAsync(Guid id)
        {
            return await _context.JobPosts
              .Include(jp => jp.Employer)
              .Include(jp => jp.RestaurantLocation)
              .FirstOrDefaultAsync(jp => jp.Id == id);
        }
    }
}

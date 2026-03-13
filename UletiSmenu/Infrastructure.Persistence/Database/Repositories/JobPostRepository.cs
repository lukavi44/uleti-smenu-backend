using Core.Models.Entities;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

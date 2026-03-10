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

        public async Task<IEnumerable<JobPost>> GetVisibleJobPostsAsync(DateTime utcNow)
        {
            return await _context.JobPosts
                .Include(jp => jp.Employer)
                .Include(jp => jp.RestaurantLocation)
                .Where(jp =>
                    jp.Status == Core.Models.Enums.JobStatusEnum.Active
                    && (jp.VisibleUntil >= utcNow || jp.StartingDate.AddHours(1) >= utcNow))
                .ToListAsync();
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

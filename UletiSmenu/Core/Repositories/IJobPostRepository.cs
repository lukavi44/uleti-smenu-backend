using Core.Models.Entities;

namespace Core.Repositories
{
    public interface IJobPostRepository : IRepository<JobPost>
    {
        Task AddJobPostAsync(JobPost jobPost);
        Task<JobPost?> GetJobPostByIdAsync(Guid id);
        Task<IEnumerable<JobPost>> GetAllByEmployerIdAsync(Guid employerId);
        Task DeleteJobPostAsync(JobPost jobPost);
        Task<IEnumerable<JobPost>> GetAllJobPostsAsync();
        Task<IEnumerable<JobPost>> GetVisibleJobPostsAsync(DateTime utcNow);
    }
}

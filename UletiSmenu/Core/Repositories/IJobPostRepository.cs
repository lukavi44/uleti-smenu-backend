using Core.DTOs;
using Core.Models.Entities;

namespace Core.Repositories
{
    public interface IJobPostRepository : IRepository<JobPost>
    {
        Task AddJobPostAsync(JobPost jobPost);
        Task<JobPost?> GetJobPostByIdAsync(Guid id);
        Task<IEnumerable<JobPost>> GetAllByEmployerIdAsync(Guid employerId);
        Task<List<string>> GetDistinctPositionsByEmployerIdAsync(Guid employerId);
        Task<(List<JobPost> Items, int TotalCount)> GetByEmployerIdPagedAsync(
            Guid employerId,
            DateTime utcNow,
            int page,
            int pageSize,
            string? position = null,
            string? status = null,
            string? lifecycle = null,
            string? sortBy = null,
            string? sortDirection = null,
            bool? hasApplicants = null);
        Task<EmployerDashboardSummaryDTO> GetEmployerDashboardSummaryAsync(Guid employerId);
        Task DeleteJobPostAsync(JobPost jobPost);
        Task<IEnumerable<JobPost>> GetAllJobPostsAsync();
        Task<IEnumerable<JobPost>> GetVisibleJobPostsAsync(DateTime utcNow, string? sortBy = null, string? sortDirection = null);
        Task<int> CountActiveByEmployerIdAsync(Guid employerId);
    }
}

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
            bool? hasApplicants = null,
            string? city = null,
            Guid? restaurantLocationId = null,
            int? minSalary = null,
            int? maxSalary = null);
        Task<EmployerDashboardSummaryDTO> GetEmployerDashboardSummaryAsync(Guid employerId);
        Task DeleteJobPostAsync(JobPost jobPost);
        Task<IEnumerable<JobPost>> GetAllJobPostsAsync();
        Task<IEnumerable<JobPost>> GetVisibleJobPostsAsync(DateTime utcNow, string? sortBy = null, string? sortDirection = null);
        Task<(List<JobPost> Items, int TotalCount)> GetVisibleJobPostsPagedAsync(
            DateTime utcNow,
            int page,
            int pageSize,
            string? sortBy = null,
            string? sortDirection = null,
            string? city = null,
            Guid? restaurantLocationId = null,
            string? position = null,
            IReadOnlyList<string>? positions = null,
            int? minSalary = null,
            int? maxSalary = null,
            DateTime? shiftDateFrom = null,
            DateTime? shiftDateTo = null,
            Guid? employeeId = null,
            string? applicationFilter = null,
            bool? favouritesOnly = null);
        Task<List<JobPost>> GetCandidateRecommendedJobPostsAsync(
            Guid employeeId,
            string city,
            DateTime utcNow,
            int pageSize);
        Task<VisibleJobPostFilterOptionsDTO> GetVisibleJobPostFilterOptionsAsync(DateTime utcNow, string? city = null);
        Task<JobPost?> GetVisibleJobPostByIdAsync(Guid id, DateTime utcNow);
        Task<int> CountActiveByEmployerIdAsync(Guid employerId);
    }
}

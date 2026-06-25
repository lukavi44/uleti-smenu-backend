using Core.DTOs;
using Core.Models.Entities;
using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IJobPostService
    {
        Task<Result> CreateJobPostAsync(JobPost jobPost);
        Task<Result> UpdateJobPostAsync(
            Guid employerId,
            Guid jobPostId,
            string title,
            string description,
            string position,
            string status,
            int salary,
            DateTime startingDate,
            DateTime? visibleUntil,
            Guid restaurantLocationId);
        Task<IEnumerable<JobPost>> GetVisibleJobPostsAsync(string? sortBy = null, string? sortDirection = null);
        Task<PagedResultDTO<JobPost>> GetVisibleJobPostsPagedAsync(
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
        Task<VisibleJobPostFilterOptionsDTO> GetVisibleJobPostFilterOptionsAsync(string? city = null);
        Task<IEnumerable<JobPost>> GetMyJobPostsAsync(Guid employerId);
        Task<List<string>> GetMyJobPostPositionsAsync(Guid employerId);
        Task<PagedResultDTO<JobPost>> GetMyJobPostsPagedAsync(
            Guid employerId,
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
    }
}

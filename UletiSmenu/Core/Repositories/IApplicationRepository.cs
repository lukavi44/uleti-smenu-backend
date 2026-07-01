using Core.DTOs;
using Core.Models.Entities;

namespace Core.Repositories
{
    public interface IApplicationRepository
    {
        Task<bool> HasEmployeeAppliedAsync(Guid employeeId, Guid jobPostId);
        Task<int> GetApplicantCountByJobPostAsync(Guid jobPostId);
        Task<Dictionary<Guid, int>> GetApplicantCountsByJobPostIdsAsync(IEnumerable<Guid> jobPostIds);
        Task<Dictionary<Guid, List<RecentApplicantPreviewDTO>>> GetRecentApplicantsByJobPostIdsAsync(
            IEnumerable<Guid> jobPostIds,
            int limitPerPost = 3);
        Task<JobPostApplicationStatsDTO> GetApplicationStatsByJobPostIdAsync(Guid jobPostId);
        Task AddAsync(Application application);
        Task<Application?> GetByIdAsync(Guid applicationId);
        Task<List<Application>> GetPendingApplicationsByJobPostIdAsync(Guid jobPostId);
        Task<List<Application>> GetPendingApplicationsForEmployeeAsync(Guid employeeId);
        Task<List<ApplicationApplicantDTO>> GetApplicantsForJobPostAsync(Guid jobPostId);
        Task<List<EmployeeApplicationDTO>> GetEmployeeApplicationsAsync(Guid employeeId);
        Task<bool> EmployerCanViewEmployeeAsync(Guid employerId, Guid employeeId);
        Task<List<(Application Application, JobPost JobPost, Employer Employer, RestaurantLocation? Location)>> GetAcceptedApplicationsWithDetailsAsync(Guid employeeId);
        Task<int> CountArchivedPlatformShiftsForEmployeeAsync(Guid employeeId, DateTime utcNow);
        Task<DateTime?> GetEmployeeMemberSinceAsync(Guid employeeId);
        Task<List<(Application Application, JobPost JobPost, Employer Employer, RestaurantLocation? Location)>> GetArchivedPlatformShiftsForEmployeePagedAsync(
            Guid employeeId,
            DateTime utcNow,
            int page,
            int pageSize);
    }
}

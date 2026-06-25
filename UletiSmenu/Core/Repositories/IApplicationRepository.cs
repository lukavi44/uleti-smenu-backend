using Core.DTOs;
using Core.Models.Entities;

namespace Core.Repositories
{
    public interface IApplicationRepository
    {
        Task<bool> HasEmployeeAppliedAsync(Guid employeeId, Guid jobPostId);
        Task<int> GetApplicantCountByJobPostAsync(Guid jobPostId);
        Task<Dictionary<Guid, int>> GetApplicantCountsByJobPostIdsAsync(IEnumerable<Guid> jobPostIds);
        Task AddAsync(Application application);
        Task<Application?> GetByIdAsync(Guid applicationId);
        Task<List<Application>> GetPendingApplicationsByJobPostIdAsync(Guid jobPostId);
        Task<List<Application>> GetPendingApplicationsForEmployeeAsync(Guid employeeId);
        Task<List<ApplicationApplicantDTO>> GetApplicantsForJobPostAsync(Guid jobPostId);
        Task<List<EmployeeApplicationDTO>> GetEmployeeApplicationsAsync(Guid employeeId);
        Task<bool> EmployerCanViewEmployeeAsync(Guid employerId, Guid employeeId);
        Task<List<(Application Application, JobPost JobPost, Employer Employer, RestaurantLocation? Location)>> GetAcceptedApplicationsWithDetailsAsync(Guid employeeId);
    }
}

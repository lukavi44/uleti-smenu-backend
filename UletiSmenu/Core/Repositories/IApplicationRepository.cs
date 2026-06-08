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
        Task<List<ApplicationApplicantDTO>> GetApplicantsForJobPostAsync(Guid jobPostId);
        Task<List<EmployeeApplicationDTO>> GetEmployeeApplicationsAsync(Guid employeeId);
    }
}

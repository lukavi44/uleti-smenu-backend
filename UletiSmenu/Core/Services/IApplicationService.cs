using Core.DTOs;
using Core.Models.Enums;
using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IApplicationService
    {
        Task<Result> ApplyToJobPostAsync(Guid employeeId, Guid jobPostId);
        Task<Result<List<ApplicationApplicantDTO>>> GetApplicantsForEmployerJobPostAsync(
            Guid employerId,
            Guid jobPostId,
            bool includeContactInfo = false);
        Task<Result<List<EmployerDashboardPendingApplicantDTO>>> GetPendingApplicantsForEmployerDashboardAsync(
            Guid employerId,
            int limit = 12);
        Task<Result<List<EmployeeApplicationDTO>>> GetMyApplicationsAsync(Guid employeeId);
        Task<Result<EmployeeDashboardDTO>> GetMyDashboardAsync(Guid employeeId, int acceptedPreviewLimit = 12);
        Task<Result> UpdateApplicationStatusByEmployerAsync(Guid employerId, Guid applicationId, ApplicationStatusEnum newStatus);
        Task<Result> CancelMyApplicationAsync(Guid employeeId, Guid applicationId);
    }
}

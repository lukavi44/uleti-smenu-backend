using Core.DTOs;
using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IEmployeeProfileService
    {
        Task<Result<List<WorkExperienceDTO>>> GetMyWorkExperiencesAsync(Guid employeeId);
        Task<Result<WorkExperienceDTO>> CreateWorkExperienceAsync(Guid employeeId, string companyName, string position, DateTime startDate, DateTime? endDate, string? description);
        Task<Result<WorkExperienceDTO>> UpdateWorkExperienceAsync(Guid employeeId, Guid experienceId, string companyName, string position, DateTime startDate, DateTime? endDate, string? description);
        Task<Result> DeleteWorkExperienceAsync(Guid employeeId, Guid experienceId);
        Task<Result<List<EmployeePlatformShiftDTO>>> GetMyPlatformShiftsAsync(Guid employeeId);
        Task<Result<EmployeePublicProfileDTO>> GetEmployeeProfileForEmployerAsync(
            Guid employerId,
            Guid employeeId,
            bool includeContactInfo = false);
    }
}

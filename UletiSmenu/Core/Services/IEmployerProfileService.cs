using Core.DTOs;
using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IEmployerProfileService
    {
        Task<Result<EmployerPublicProfileDTO>> GetEmployerPublicProfileAsync(Guid employerId, Guid? employeeId);
    }
}

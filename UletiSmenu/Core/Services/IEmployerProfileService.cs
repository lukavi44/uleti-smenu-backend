using Core.DTOs;
using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IEmployerProfileService
    {
        Task<Result<EmployerPublicProfileDTO>> GetEmployerPublicProfileAsync(Guid employerId, Guid? employeeId);
        Task<Result<EmployerPublicProfileDTO>> GetEmployerPublicProfileBySlugAsync(string slug, Guid? employeeId);
        Task<Result<EmployerDirectoryPreviewDTO>> GetEmployerDirectoryPreviewAsync(Guid employerId);
        Task<Result<EmployerDirectoryPreviewDTO>> GetEmployerDirectoryPreviewBySlugAsync(string slug);
        Task<Result<string>> ResolveEmployerSlugAsync(Guid employerId);
        Task<PagedResultDTO<EmployerDirectoryListItemDTO>> GetEmployerDirectoryPagedAsync(
            string? city,
            string? search,
            int page,
            int pageSize,
            Guid? employeeId);
    }
}

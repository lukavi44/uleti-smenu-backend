using Core.DTOs.Admin;
using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IAdminService
    {
        Task<AdminDashboardDTO> GetDashboardAsync(DateTime? fromUtc, DateTime? toUtc);
        Task<AdminEmployerListResponseDTO> GetEmployersAsync(
            string? search,
            string? status,
            string? city,
            int page,
            int pageSize);
        Task<Result<AdminEmployerDetailDTO>> GetEmployerDetailAsync(Guid employerId);
        Task<Result<AdminEmployerDetailDTO>> SetEmployerVerificationAsync(
            Guid employerId,
            bool isVerified,
            Guid adminUserId);
        Task<Result<AdminEmployerDetailDTO>> SetEmployerSuspensionAsync(
            Guid employerId,
            bool isSuspended,
            Guid adminUserId);
        Task<Result<AdminEmployerDetailDTO>> SetEmployerAdminNotesAsync(
            Guid employerId,
            string? notes);
        Task<AdminPagedResponseDTO<AdminCandidateListItemDTO>> GetCandidatesAsync(
            string? search,
            string? city,
            int page,
            int pageSize);
        Task<AdminPagedResponseDTO<AdminRestaurantListItemDTO>> GetRestaurantsAsync(
            string? search,
            string? city,
            int page,
            int pageSize);
        Task<AdminPagedResponseDTO<AdminJobPostListItemDTO>> GetJobPostsAsync(
            string? search,
            string? status,
            int page,
            int pageSize);
        Task<Result<AdminJobPostDetailDTO>> GetJobPostDetailAsync(Guid jobPostId);
        Task<Result<AdminJobPostDetailDTO>> ArchiveJobPostAsync(Guid jobPostId);
        Task<AdminPagedResponseDTO<AdminApplicationListItemDTO>> GetApplicationsAsync(
            string? search,
            string? status,
            int page,
            int pageSize);
        Task<AdminPagedResponseDTO<AdminBillingListItemDTO>> GetBillingTransactionsAsync(
            string? search,
            int page,
            int pageSize);
        Task<AdminPagedResponseDTO<AdminUserListItemDTO>> GetUsersAsync(
            string? search,
            string? role,
            string? status,
            int page,
            int pageSize);
        Task<Result<AdminUserListItemDTO>> SetUserLockoutAsync(
            Guid userId,
            bool isLockedOut,
            Guid adminUserId);
    }
}

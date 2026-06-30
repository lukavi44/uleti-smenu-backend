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
        Task<AdminPagedResponseDTO<AdminApplicationListItemDTO>> GetApplicationsAsync(
            string? search,
            string? status,
            int page,
            int pageSize);
        Task<AdminPagedResponseDTO<AdminBillingListItemDTO>> GetBillingTransactionsAsync(
            string? search,
            int page,
            int pageSize);
    }
}

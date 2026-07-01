using Core.DTOs;
using Core.Models.Entities;

namespace Core.Repositories
{
    public interface IReviewRepository
    {
        Task<MatchReview?> GetByApplicationAndReviewerAsync(Guid applicationId, Guid reviewerId);
        Task AddAsync(MatchReview review);
        Task<List<ReviewDTO>> GetReviewsForEmployeeAsync(Guid employeeId);
        Task<(List<ReviewDTO> Items, int TotalCount)> GetReviewsForEmployeePagedAsync(
            Guid employeeId,
            int page,
            int pageSize);
        Task<CandidateReviewSummaryDTO> GetCandidateReviewSummaryAsync(Guid employeeId);
        Task<(List<CandidateReviewItemDTO> Items, int TotalCount)> GetCandidateReviewsPagedAsync(
            Guid employeeId,
            int page,
            int pageSize,
            string sort);
        Task<List<ReviewDTO>> GetReviewsForEmployerAsync(Guid employerId);
        Task<EmployerRestaurantReviewSummaryDTO> GetEmployerRestaurantReviewSummaryAsync(Guid employerId);
        Task<(List<EmployerRestaurantReviewItemDTO> Items, int TotalCount)> GetEmployerRestaurantReviewsPagedAsync(
            Guid employerId,
            int page,
            int pageSize,
            string sort);
        Task<ReviewSummaryDTO> GetEmployeeReviewSummaryAsync(Guid employeeId);
        Task<ReviewSummaryDTO> GetEmployerReviewSummaryAsync(Guid employerId);
        Task<Dictionary<Guid, ReviewSummaryDTO>> GetEmployeeReviewSummariesAsync(IEnumerable<Guid> employeeIds);
        Task<List<PendingReviewDTO>> GetPendingReviewsForEmployeeAsync(Guid employeeId, DateTime utcNow);
        Task<List<PendingReviewDTO>> GetPendingReviewsForEmployerAsync(Guid employerId, DateTime utcNow);
        Task<(Application Application, JobPost JobPost, Employer Employer, Employee Employee)?> GetReviewableMatchAsync(Guid applicationId, DateTime utcNow);
    }
}

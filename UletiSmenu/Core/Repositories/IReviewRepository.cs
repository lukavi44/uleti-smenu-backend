using Core.DTOs;
using Core.Models.Entities;

namespace Core.Repositories
{
    public interface IReviewRepository
    {
        Task<MatchReview?> GetByApplicationAndReviewerAsync(Guid applicationId, Guid reviewerId);
        Task AddAsync(MatchReview review);
        Task<List<ReviewDTO>> GetReviewsForEmployeeAsync(Guid employeeId);
        Task<ReviewSummaryDTO> GetEmployeeReviewSummaryAsync(Guid employeeId);
        Task<Dictionary<Guid, ReviewSummaryDTO>> GetEmployeeReviewSummariesAsync(IEnumerable<Guid> employeeIds);
        Task<List<PendingReviewDTO>> GetPendingReviewsForEmployeeAsync(Guid employeeId, DateTime utcNow);
        Task<List<PendingReviewDTO>> GetPendingReviewsForEmployerAsync(Guid employerId, DateTime utcNow);
        Task<(Application Application, JobPost JobPost, Employer Employer, Employee Employee)?> GetReviewableMatchAsync(Guid applicationId, DateTime utcNow);
    }
}

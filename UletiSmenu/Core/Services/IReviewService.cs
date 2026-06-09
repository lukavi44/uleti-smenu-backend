using Core.DTOs;
using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IReviewService
    {
        Task<Result<List<PendingReviewDTO>>> GetMyPendingReviewsAsync(Guid userId, string role);
        Task<Result<ReviewDTO>> SubmitReviewAsync(Guid reviewerId, Guid applicationId, int rating, string? comment);
        Task<Result<List<ReviewDTO>>> GetEmployeeReviewsAsync(Guid employeeId);
        Task<Result<ReviewSummaryDTO>> GetEmployeeReviewSummaryAsync(Guid employeeId);
    }
}

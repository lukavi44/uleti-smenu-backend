using Core.DTOs;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Core.Services;
using CSharpFunctionalExtensions;

namespace Infrastructure.Persistence.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IUserRepository _userRepository;
        private readonly IApplicationUnitOfWork _applicationUnitOfWork;

        public ReviewService(
            IReviewRepository reviewRepository,
            IUserRepository userRepository,
            IApplicationUnitOfWork applicationUnitOfWork)
        {
            _reviewRepository = reviewRepository;
            _userRepository = userRepository;
            _applicationUnitOfWork = applicationUnitOfWork;
        }

        public async Task<Result<List<PendingReviewDTO>>> GetMyPendingReviewsAsync(Guid userId, string role)
        {
            var utcNow = DateTime.UtcNow;

            if (role == UserRolesEnum.Employee.ToString())
                return Result.Success(await _reviewRepository.GetPendingReviewsForEmployeeAsync(userId, utcNow));

            if (role == UserRolesEnum.Employer.ToString())
                return Result.Success(await _reviewRepository.GetPendingReviewsForEmployerAsync(userId, utcNow));

            return Result.Failure<List<PendingReviewDTO>>("Only employers and employees can leave reviews.");
        }

        public async Task<Result<ReviewDTO>> SubmitReviewAsync(Guid reviewerId, Guid applicationId, int rating, string? comment)
        {
            var match = await _reviewRepository.GetReviewableMatchAsync(applicationId, DateTime.UtcNow);
            if (match == null)
                return Result.Failure<ReviewDTO>("Reviews are available only after a completed and expired shift.");

            var (_, jobPost, employer, employee) = match.Value;
            Guid revieweeId;

            if (reviewerId == employee.Id)
            {
                revieweeId = employer.Id;
            }
            else if (reviewerId == employer.Id)
            {
                revieweeId = employee.Id;
            }
            else
            {
                return Result.Failure<ReviewDTO>("You are not part of this match.");
            }

            var existingReview = await _reviewRepository.GetByApplicationAndReviewerAsync(applicationId, reviewerId);
            if (existingReview != null)
                return Result.Failure<ReviewDTO>("You have already submitted a review for this match.");

            var reviewResult = Core.Models.Entities.MatchReview.Create(
                applicationId,
                reviewerId,
                revieweeId,
                rating,
                comment,
                DateTime.UtcNow);

            if (reviewResult.IsFailure)
                return Result.Failure<ReviewDTO>(reviewResult.Error);

            await _reviewRepository.AddAsync(reviewResult.Value);
            await _applicationUnitOfWork.SaveChangesAsync();

            return Result.Success(new ReviewDTO
            {
                Id = reviewResult.Value.Id,
                ApplicationId = applicationId,
                Rating = rating,
                Comment = reviewResult.Value.Comment,
                JobPostTitle = jobPost.Title,
                CreatedAtUtc = reviewResult.Value.CreatedAtUtc,
                ReviewerName = reviewerId == employer.Id ? employer.Name : $"{employee.FirstName} {employee.LastName}"
            });
        }

        public async Task<Result<List<ReviewDTO>>> GetEmployeeReviewsAsync(Guid employeeId)
        {
            return Result.Success(await _reviewRepository.GetReviewsForEmployeeAsync(employeeId));
        }

        public async Task<Result<ReviewSummaryDTO>> GetEmployeeReviewSummaryAsync(Guid employeeId)
        {
            return Result.Success(await _reviewRepository.GetEmployeeReviewSummaryAsync(employeeId));
        }

        public async Task<Result<ReviewSummaryDTO>> GetEmployerReviewSummaryAsync(Guid employerId)
        {
            return Result.Success(await _reviewRepository.GetEmployerReviewSummaryAsync(employerId));
        }

        public async Task<Result<ReviewPageDTO>> GetEmployeeReviewPageAsync(Guid employeeId)
        {
            var employee = await _userRepository.GetByIdAsync<Employee>(employeeId);
            if (employee == null)
                return Result.Failure<ReviewPageDTO>("Employee not found.");

            return Result.Success(new ReviewPageDTO
            {
                SubjectId = employeeId,
                SubjectName = $"{employee.FirstName} {employee.LastName}",
                Summary = await _reviewRepository.GetEmployeeReviewSummaryAsync(employeeId),
                Reviews = await _reviewRepository.GetReviewsForEmployeeAsync(employeeId)
            });
        }

        public async Task<Result<ReviewPageDTO>> GetEmployerReviewPageAsync(Guid employerId)
        {
            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);
            if (employer == null)
                return Result.Failure<ReviewPageDTO>("Restaurant not found.");

            return Result.Success(new ReviewPageDTO
            {
                SubjectId = employerId,
                SubjectName = employer.Name,
                Summary = await _reviewRepository.GetEmployerReviewSummaryAsync(employerId),
                Reviews = await _reviewRepository.GetReviewsForEmployerAsync(employerId)
            });
        }
    }
}

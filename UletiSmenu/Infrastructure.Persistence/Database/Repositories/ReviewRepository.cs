using Core.DTOs;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Database.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly ApplicationDbContext _context;

        public ReviewRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<MatchReview?> GetByApplicationAndReviewerAsync(Guid applicationId, Guid reviewerId)
        {
            return await _context.MatchReviews
                .FirstOrDefaultAsync(review =>
                    review.ApplicationId == applicationId
                    && review.ReviewerId == reviewerId);
        }

        public async Task AddAsync(MatchReview review)
        {
            await _context.MatchReviews.AddAsync(review);
        }

        public async Task<List<ReviewDTO>> GetReviewsForEmployeeAsync(Guid employeeId)
        {
            var reviews = await (
                from review in _context.MatchReviews
                join application in _context.Applications on review.ApplicationId equals application.Id
                join jobPost in _context.JobPosts on application.JobPostId equals jobPost.Id
                join reviewerEmployer in _context.Users.OfType<Employer>() on review.ReviewerId equals reviewerEmployer.Id into employerReviewers
                from reviewerEmployer in employerReviewers.DefaultIfEmpty()
                join reviewerEmployee in _context.Users.OfType<Employee>() on review.ReviewerId equals reviewerEmployee.Id into employeeReviewers
                from reviewerEmployee in employeeReviewers.DefaultIfEmpty()
                where review.RevieweeId == employeeId
                orderby review.CreatedAtUtc descending
                select new
                {
                    review,
                    jobPost.Title,
                    ReviewerName = reviewerEmployer != null
                        ? reviewerEmployer.Name
                        : reviewerEmployee != null
                            ? reviewerEmployee.FirstName + " " + reviewerEmployee.LastName
                            : "Unknown"
                }).ToListAsync();

            return reviews
                .Select(item => new ReviewDTO
                {
                    Id = item.review.Id,
                    ApplicationId = item.review.ApplicationId,
                    ReviewerName = item.ReviewerName,
                    Rating = item.review.Rating,
                    Comment = item.review.Comment,
                    JobPostTitle = item.Title,
                    CreatedAtUtc = item.review.CreatedAtUtc
                })
                .ToList();
        }

        public async Task<ReviewSummaryDTO> GetEmployeeReviewSummaryAsync(Guid employeeId)
        {
            var summaries = await GetEmployeeReviewSummariesAsync(new[] { employeeId });
            return summaries.GetValueOrDefault(employeeId, new ReviewSummaryDTO());
        }

        public async Task<Dictionary<Guid, ReviewSummaryDTO>> GetEmployeeReviewSummariesAsync(IEnumerable<Guid> employeeIds)
        {
            var ids = employeeIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<Guid, ReviewSummaryDTO>();

            var grouped = await _context.MatchReviews
                .Where(review => ids.Contains(review.RevieweeId))
                .GroupBy(review => review.RevieweeId)
                .Select(group => new
                {
                    EmployeeId = group.Key,
                    AverageRating = group.Average(review => review.Rating),
                    ReviewCount = group.Count()
                })
                .ToListAsync();

            return grouped.ToDictionary(
                item => item.EmployeeId,
                item => new ReviewSummaryDTO
                {
                    AverageRating = Math.Round(item.AverageRating, 1),
                    ReviewCount = item.ReviewCount
                });
        }

        public async Task<List<PendingReviewDTO>> GetPendingReviewsForEmployeeAsync(Guid employeeId, DateTime utcNow)
        {
            var rows = await GetAcceptedApplicationsWithDetailsAsync(employeeId);
            var pending = new List<PendingReviewDTO>();

            foreach (var row in rows)
            {
                if (!row.JobPost.IsArchived(utcNow))
                    continue;

                var existingReview = await GetByApplicationAndReviewerAsync(row.Application.Id, employeeId);
                if (existingReview != null)
                    continue;

                pending.Add(new PendingReviewDTO
                {
                    ApplicationId = row.Application.Id,
                    JobPostId = row.JobPost.Id,
                    JobPostTitle = row.JobPost.Title,
                    RevieweeId = row.Employer.Id,
                    RevieweeName = row.Employer.Name,
                    ShiftDate = row.JobPost.StartingDate
                });
            }

            return pending;
        }

        public async Task<List<PendingReviewDTO>> GetPendingReviewsForEmployerAsync(Guid employerId, DateTime utcNow)
        {
            var rows = await (
                from application in _context.Applications
                join jobPost in _context.JobPosts on application.JobPostId equals jobPost.Id
                join employee in _context.Users.OfType<Employee>() on application.UserId equals employee.Id
                where jobPost.EmployerId == employerId && application.Status == ApplicationStatusEnum.Accepted
                orderby jobPost.StartingDate descending
                select new { application, jobPost, employee }).ToListAsync();

            var pending = new List<PendingReviewDTO>();

            foreach (var row in rows)
            {
                if (!row.jobPost.IsArchived(utcNow))
                    continue;

                var existingReview = await GetByApplicationAndReviewerAsync(row.application.Id, employerId);
                if (existingReview != null)
                    continue;

                pending.Add(new PendingReviewDTO
                {
                    ApplicationId = row.application.Id,
                    JobPostId = row.jobPost.Id,
                    JobPostTitle = row.jobPost.Title,
                    RevieweeId = row.employee.Id,
                    RevieweeName = $"{row.employee.FirstName} {row.employee.LastName}",
                    ShiftDate = row.jobPost.StartingDate
                });
            }

            return pending;
        }

        public async Task<(Application Application, JobPost JobPost, Employer Employer, Employee Employee)?> GetReviewableMatchAsync(
            Guid applicationId,
            DateTime utcNow)
        {
            var row = await (
                from application in _context.Applications
                join jobPost in _context.JobPosts on application.JobPostId equals jobPost.Id
                join employer in _context.Users.OfType<Employer>() on jobPost.EmployerId equals employer.Id
                join employee in _context.Users.OfType<Employee>() on application.UserId equals employee.Id
                where application.Id == applicationId && application.Status == ApplicationStatusEnum.Accepted
                select new { application, jobPost, employer, employee }).FirstOrDefaultAsync();

            if (row == null || !row.jobPost.IsArchived(utcNow))
                return null;

            return (row.application, row.jobPost, row.employer, row.employee);
        }

        private async Task<List<(Application Application, JobPost JobPost, Employer Employer)>> GetAcceptedApplicationsWithDetailsAsync(Guid employeeId)
        {
            var rows = await (
                from application in _context.Applications
                join jobPost in _context.JobPosts on application.JobPostId equals jobPost.Id
                join employer in _context.Users.OfType<Employer>() on jobPost.EmployerId equals employer.Id
                where application.UserId == employeeId && application.Status == ApplicationStatusEnum.Accepted
                orderby jobPost.StartingDate descending
                select new { application, jobPost, employer }).ToListAsync();

            return rows.Select(row => (row.application, row.jobPost, row.employer)).ToList();
        }
    }
}

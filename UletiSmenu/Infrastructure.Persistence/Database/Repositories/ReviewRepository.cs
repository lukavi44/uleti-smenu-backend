using Core.DTOs;
using Core.Helpers;
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

        public async Task<(List<ReviewDTO> Items, int TotalCount)> GetReviewsForEmployeePagedAsync(
            Guid employeeId,
            int page,
            int pageSize)
        {
            var query =
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
                };

            var totalCount = await query.CountAsync();
            var reviews = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (
                reviews
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
                    .ToList(),
                totalCount);
        }

        public async Task<CandidateReviewSummaryDTO> GetCandidateReviewSummaryAsync(Guid employeeId)
        {
            var employee = await _context.Users.OfType<Employee>()
                .Where(item => item.Id == employeeId)
                .Select(item => new { item.FirstName, item.LastName })
                .FirstOrDefaultAsync();

            var reviews = await _context.MatchReviews
                .Where(review => review.RevieweeId == employeeId)
                .Select(review => new { review.Rating, review.CreatedAtUtc })
                .ToListAsync();

            var candidateName = employee == null
                ? string.Empty
                : $"{employee.FirstName} {employee.LastName}".Trim();

            if (reviews.Count == 0)
            {
                return new CandidateReviewSummaryDTO
                {
                    CandidateName = candidateName
                };
            }

            return new CandidateReviewSummaryDTO
            {
                CandidateName = candidateName,
                AverageRating = Math.Round((decimal)reviews.Average(item => item.Rating), 1),
                ReviewCount = reviews.Count,
                RecommendationsCount = reviews.Count(item => item.Rating >= ReviewRecommendationRules.MinimumRating),
                LastReviewAtUtc = reviews.Max(item => item.CreatedAtUtc)
            };
        }

        public async Task<(List<CandidateReviewItemDTO> Items, int TotalCount)> GetCandidateReviewsPagedAsync(
            Guid employeeId,
            int page,
            int pageSize,
            string sort)
        {
            var query =
                from review in _context.MatchReviews
                join application in _context.Applications on review.ApplicationId equals application.Id
                join jobPost in _context.JobPosts on application.JobPostId equals jobPost.Id
                join reviewerEmployer in _context.Users.OfType<Employer>() on review.ReviewerId equals reviewerEmployer.Id into employerReviewers
                from reviewerEmployer in employerReviewers.DefaultIfEmpty()
                join reviewerEmployee in _context.Users.OfType<Employee>() on review.ReviewerId equals reviewerEmployee.Id into employeeReviewers
                from reviewerEmployee in employeeReviewers.DefaultIfEmpty()
                where review.RevieweeId == employeeId
                select new
                {
                    review,
                    jobPostTitle = jobPost.Title,
                    reviewerId = reviewerEmployer != null
                        ? reviewerEmployer.Id
                        : reviewerEmployee != null
                            ? reviewerEmployee.Id
                            : review.ReviewerId,
                    reviewerName = reviewerEmployer != null
                        ? reviewerEmployer.Name
                        : reviewerEmployee != null
                            ? reviewerEmployee.FirstName + " " + reviewerEmployee.LastName
                            : "Unknown",
                    reviewerProfilePhoto = reviewerEmployer != null
                        ? reviewerEmployer.ProfilePhoto
                        : reviewerEmployee != null
                            ? reviewerEmployee.ProfilePhoto
                            : null,
                    reviewerIsVerified = reviewerEmployer != null && reviewerEmployer.IsVerifiedEmployer
                };

            query = sort switch
            {
                "highest" => query
                    .OrderByDescending(item => item.review.Rating)
                    .ThenByDescending(item => item.review.CreatedAtUtc),
                "lowest" => query
                    .OrderBy(item => item.review.Rating)
                    .ThenByDescending(item => item.review.CreatedAtUtc),
                _ => query.OrderByDescending(item => item.review.CreatedAtUtc)
            };

            var totalCount = await _context.MatchReviews
                .Where(review => review.RevieweeId == employeeId)
                .CountAsync();

            var reviews = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (
                reviews.Select(item => new CandidateReviewItemDTO
                {
                    Id = item.review.Id,
                    ApplicationId = item.review.ApplicationId,
                    ReviewerId = item.reviewerId,
                    ReviewerName = item.reviewerName.Trim(),
                    ReviewerProfilePhoto = item.reviewerProfilePhoto,
                    ReviewerIsVerified = item.reviewerIsVerified,
                    Rating = item.review.Rating,
                    Comment = item.review.Comment,
                    JobPostTitle = item.jobPostTitle,
                    CreatedAtUtc = item.review.CreatedAtUtc,
                    Recommends = item.review.Rating >= ReviewRecommendationRules.MinimumRating
                }).ToList(),
                totalCount);
        }

        public async Task<List<ReviewDTO>> GetReviewsForEmployerAsync(Guid employerId)
        {
            var reviews = await (
                from review in _context.MatchReviews
                join application in _context.Applications on review.ApplicationId equals application.Id
                join jobPost in _context.JobPosts on application.JobPostId equals jobPost.Id
                join reviewerEmployee in _context.Users.OfType<Employee>() on review.ReviewerId equals reviewerEmployee.Id
                where review.RevieweeId == employerId
                orderby review.CreatedAtUtc descending
                select new
                {
                    review,
                    jobPost.Title,
                    ReviewerName = reviewerEmployee.FirstName + " " + reviewerEmployee.LastName
                })
                .ToListAsync();

            return reviews
                .Select(item => new ReviewDTO
                {
                    Id = item.review.Id,
                    ApplicationId = item.review.ApplicationId,
                    ReviewerName = item.ReviewerName.Trim(),
                    Rating = item.review.Rating,
                    Comment = item.review.Comment,
                    JobPostTitle = item.Title,
                    CreatedAtUtc = item.review.CreatedAtUtc
                })
                .ToList();
        }

        public async Task<EmployerRestaurantReviewSummaryDTO> GetEmployerRestaurantReviewSummaryAsync(Guid employerId)
        {
            var employer = await _context.Users.OfType<Employer>()
                .Where(item => item.Id == employerId)
                .Select(item => new { item.Name })
                .FirstOrDefaultAsync();

            var reviews = await _context.MatchReviews
                .Where(review => review.RevieweeId == employerId)
                .Select(review => new { review.Rating, review.CreatedAtUtc })
                .ToListAsync();

            if (reviews.Count == 0)
            {
                return new EmployerRestaurantReviewSummaryDTO
                {
                    RestaurantName = employer?.Name ?? string.Empty
                };
            }

            return new EmployerRestaurantReviewSummaryDTO
            {
                RestaurantName = employer?.Name ?? string.Empty,
                AverageRating = Math.Round((decimal)reviews.Average(item => item.Rating), 1),
                ReviewCount = reviews.Count,
                RecommendationsCount = reviews.Count(item => item.Rating >= ReviewRecommendationRules.MinimumRating),
                LastReviewAtUtc = reviews.Max(item => item.CreatedAtUtc)
            };
        }

        public async Task<(List<EmployerRestaurantReviewItemDTO> Items, int TotalCount)> GetEmployerRestaurantReviewsPagedAsync(
            Guid employerId,
            int page,
            int pageSize,
            string sort)
        {
            var query =
                from review in _context.MatchReviews
                join application in _context.Applications on review.ApplicationId equals application.Id
                join jobPost in _context.JobPosts on application.JobPostId equals jobPost.Id
                join reviewerEmployee in _context.Users.OfType<Employee>() on review.ReviewerId equals reviewerEmployee.Id
                where review.RevieweeId == employerId
                select new
                {
                    review,
                    reviewerId = reviewerEmployee.Id,
                    reviewerName = reviewerEmployee.FirstName + " " + reviewerEmployee.LastName,
                    reviewerProfilePhoto = reviewerEmployee.ProfilePhoto
                };

            query = sort switch
            {
                "highest" => query
                    .OrderByDescending(item => item.review.Rating)
                    .ThenByDescending(item => item.review.CreatedAtUtc),
                "lowest" => query
                    .OrderBy(item => item.review.Rating)
                    .ThenByDescending(item => item.review.CreatedAtUtc),
                _ => query.OrderByDescending(item => item.review.CreatedAtUtc)
            };

            var totalCount = await _context.MatchReviews
                .Where(review => review.RevieweeId == employerId)
                .CountAsync();

            var reviews = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (
                reviews.Select(item => new EmployerRestaurantReviewItemDTO
                {
                    Id = item.review.Id,
                    ApplicationId = item.review.ApplicationId,
                    ReviewerId = item.reviewerId,
                    ReviewerName = item.reviewerName.Trim(),
                    ReviewerProfilePhoto = item.reviewerProfilePhoto,
                    Rating = item.review.Rating,
                    Comment = item.review.Comment,
                    CreatedAtUtc = item.review.CreatedAtUtc,
                    Recommends = item.review.Rating >= ReviewRecommendationRules.MinimumRating
                }).ToList(),
                totalCount);
        }

        public async Task<ReviewSummaryDTO> GetEmployeeReviewSummaryAsync(Guid employeeId)
        {
            var summaries = await GetEmployeeReviewSummariesAsync(new[] { employeeId });
            return summaries.GetValueOrDefault(employeeId, new ReviewSummaryDTO());
        }

        public async Task<ReviewSummaryDTO> GetEmployerReviewSummaryAsync(Guid employerId)
        {
            return await GetReviewSummaryForRevieweeAsync(employerId);
        }

        private async Task<ReviewSummaryDTO> GetReviewSummaryForRevieweeAsync(Guid revieweeId)
        {
            var grouped = await _context.MatchReviews
                .Where(review => review.RevieweeId == revieweeId)
                .GroupBy(review => review.RevieweeId)
                .Select(group => new
                {
                    AverageRating = group.Average(review => review.Rating),
                    ReviewCount = group.Count()
                })
                .FirstOrDefaultAsync();

            if (grouped == null)
                return new ReviewSummaryDTO();

            return new ReviewSummaryDTO
            {
                AverageRating = Math.Round(grouped.AverageRating, 1),
                ReviewCount = grouped.ReviewCount
            };
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

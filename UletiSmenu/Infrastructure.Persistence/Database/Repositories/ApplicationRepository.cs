using Core.DTOs;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Database.Repositories
{
    public class ApplicationRepository : IApplicationRepository
    {
        private readonly ApplicationDbContext _context;

        public ApplicationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasEmployeeAppliedAsync(Guid employeeId, Guid jobPostId)
        {
            return await _context.Applications
                .AnyAsync(x => x.UserId == employeeId && x.JobPostId == jobPostId);
        }

        public async Task<int> GetApplicantCountByJobPostAsync(Guid jobPostId)
        {
            return await _context.Applications.CountAsync(x => x.JobPostId == jobPostId);
        }

        public async Task<Dictionary<Guid, int>> GetApplicantCountsByJobPostIdsAsync(IEnumerable<Guid> jobPostIds)
        {
            var ids = jobPostIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<Guid, int>();

            return await _context.Applications
                .Where(application => ids.Contains(application.JobPostId))
                .GroupBy(application => application.JobPostId)
                .Select(group => new { JobPostId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(x => x.JobPostId, x => x.Count);
        }

        public async Task<Dictionary<Guid, List<RecentApplicantPreviewDTO>>> GetRecentApplicantsByJobPostIdsAsync(
            IEnumerable<Guid> jobPostIds,
            int limitPerPost = 3)
        {
            var ids = jobPostIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<Guid, List<RecentApplicantPreviewDTO>>();

            var rows = await (
                from application in _context.Applications
                join user in _context.Users.OfType<Employee>() on application.UserId equals user.Id
                where ids.Contains(application.JobPostId)
                orderby application.DateTime descending
                select new
                {
                    application.JobPostId,
                    UserId = user.Id,
                    user.ProfilePhoto,
                    user.FirstName,
                    user.LastName,
                    application.DateTime
                }).ToListAsync();

            return rows
                .GroupBy(row => row.JobPostId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(row => row.DateTime)
                        .Take(limitPerPost)
                        .Select(row => new RecentApplicantPreviewDTO
                        {
                            UserId = row.UserId,
                            ProfilePhoto = row.ProfilePhoto,
                            FirstName = row.FirstName,
                            LastName = row.LastName
                        })
                        .ToList());
        }

        public async Task<JobPostApplicationStatsDTO> GetApplicationStatsByJobPostIdAsync(Guid jobPostId)
        {
            var grouped = await _context.Applications
                .Where(application => application.JobPostId == jobPostId)
                .GroupBy(application => application.Status)
                .Select(group => new { Status = group.Key, Count = group.Count() })
                .ToListAsync();

            var accepted = grouped
                .Where(item => item.Status == ApplicationStatusEnum.Accepted)
                .Sum(item => item.Count);
            var pending = grouped
                .Where(item => item.Status == ApplicationStatusEnum.Applied)
                .Sum(item => item.Count);
            var denied = grouped
                .Where(item => item.Status == ApplicationStatusEnum.Denied)
                .Sum(item => item.Count);

            return new JobPostApplicationStatsDTO
            {
                TotalApplications = grouped.Sum(item => item.Count),
                Accepted = accepted,
                Pending = pending,
                Denied = denied
            };
        }

        public async Task<int> CountDistinctAcceptedCandidatesByEmployerAsync(Guid employerId)
        {
            return await (
                from application in _context.Applications
                join jobPost in _context.JobPosts on application.JobPostId equals jobPost.Id
                where jobPost.EmployerId == employerId
                      && application.Status == ApplicationStatusEnum.Accepted
                select application.UserId)
                .Distinct()
                .CountAsync();
        }

        public async Task AddAsync(Application application)
        {
            await _context.Applications.AddAsync(application);
        }

        public async Task<Application?> GetByIdAsync(Guid applicationId)
        {
            return await _context.Applications.FirstOrDefaultAsync(x => x.Id == applicationId);
        }

        public async Task<List<Application>> GetPendingApplicationsByJobPostIdAsync(Guid jobPostId)
        {
            return await _context.Applications
                .Where(application =>
                    application.JobPostId == jobPostId &&
                    application.Status == ApplicationStatusEnum.Applied)
                .ToListAsync();
        }

        public async Task<List<Application>> GetPendingApplicationsForEmployeeAsync(Guid employeeId)
        {
            return await _context.Applications
                .Where(application =>
                    application.UserId == employeeId &&
                    application.Status == ApplicationStatusEnum.Applied)
                .ToListAsync();
        }

        public async Task<List<ApplicationApplicantDTO>> GetApplicantsForJobPostAsync(Guid jobPostId)
        {
            var applicants = await (from application in _context.Applications
                          join user in _context.Users.OfType<Employee>() on application.UserId equals user.Id
                          where application.JobPostId == jobPostId
                          select new ApplicationApplicantDTO
                          {
                              ApplicationId = application.Id,
                              UserId = user.Id,
                              FirstName = user.FirstName,
                              LastName = user.LastName,
                              Email = user.Email!,
                              PhoneNumber = user.PhoneNumber!,
                              ProfilePhoto = user.ProfilePhoto,
                              City = user.City,
                              Status = application.Status.ToString(),
                              AppliedAt = application.DateTime
                          }).ToListAsync();

            if (applicants.Count == 0)
                return applicants;

            var employeeIds = applicants.Select(applicant => applicant.UserId).ToList();
            var reviewSummaries = await _context.MatchReviews
                .Where(review => employeeIds.Contains(review.RevieweeId))
                .GroupBy(review => review.RevieweeId)
                .Select(group => new
                {
                    EmployeeId = group.Key,
                    AverageRating = group.Average(review => review.Rating),
                    ReviewCount = group.Count()
                })
                .ToDictionaryAsync(
                    item => item.EmployeeId,
                    item => new { item.AverageRating, item.ReviewCount });

            foreach (var applicant in applicants)
            {
                if (reviewSummaries.TryGetValue(applicant.UserId, out var summary))
                {
                    applicant.AverageRating = Math.Round(summary.AverageRating, 1);
                    applicant.ReviewCount = summary.ReviewCount;
                }
            }

            return applicants;
        }

        public async Task<List<EmployeeApplicationDTO>> GetEmployeeApplicationsAsync(Guid employeeId)
        {
            return await (from application in _context.Applications
                          join jobPost in _context.JobPosts on application.JobPostId equals jobPost.Id
                          join employer in _context.Users.OfType<Employer>() on jobPost.EmployerId equals employer.Id
                          join location in _context.RestaurantLocations on jobPost.RestaurantLocationId equals location.Id into locationGroup
                          from location in locationGroup.DefaultIfEmpty()
                          where application.UserId == employeeId
                          orderby application.DateTime descending
                          select new EmployeeApplicationDTO
                          {
                              ApplicationId = application.Id,
                              JobPostId = jobPost.Id,
                              JobPostTitle = jobPost.Title,
                              Position = jobPost.Position,
                              EmployerName = employer.Name,
                              RestaurantLocationName = location != null ? location.Name : null,
                              RestaurantLocationCity = location != null ? location.City : null,
                              StartingDate = jobPost.StartingDate,
                              Salary = jobPost.Salary,
                              Status = application.Status.ToString(),
                              AppliedAt = application.DateTime
                          }).ToListAsync();
        }

        public async Task<bool> EmployerCanViewEmployeeAsync(Guid employerId, Guid employeeId)
        {
            return await (
                from application in _context.Applications
                join jobPost in _context.JobPosts on application.JobPostId equals jobPost.Id
                where application.UserId == employeeId && jobPost.EmployerId == employerId
                select application.Id).AnyAsync();
        }

        public async Task<List<(Application Application, JobPost JobPost, Employer Employer, RestaurantLocation? Location)>> GetAcceptedApplicationsWithDetailsAsync(Guid employeeId)
        {
            var rows = await (
                from application in _context.Applications
                join jobPost in _context.JobPosts on application.JobPostId equals jobPost.Id
                join employer in _context.Users.OfType<Employer>() on jobPost.EmployerId equals employer.Id
                join location in _context.RestaurantLocations on jobPost.RestaurantLocationId equals location.Id into locationGroup
                from location in locationGroup.DefaultIfEmpty()
                where application.UserId == employeeId && application.Status == ApplicationStatusEnum.Accepted
                orderby jobPost.StartingDate descending
                select new { application, jobPost, employer, location }).ToListAsync();

            return rows
                .Select(row => (row.application, row.jobPost, row.employer, row.location))
                .ToList();
        }

        public async Task<int> CountArchivedPlatformShiftsForEmployeeAsync(Guid employeeId, DateTime utcNow)
        {
            var archiveCutoff = utcNow.AddHours(-1);

            return await (
                from application in _context.Applications
                join jobPost in _context.JobPosts on application.JobPostId equals jobPost.Id
                where application.UserId == employeeId
                      && application.Status == ApplicationStatusEnum.Accepted
                      && (jobPost.Status == JobStatusEnum.Cancelled
                          || jobPost.Status == JobStatusEnum.Completed
                          || jobPost.Status == JobStatusEnum.Expired
                          || jobPost.StartingDate < archiveCutoff)
                select application.Id).CountAsync();
        }

        public async Task<DateTime?> GetEmployeeMemberSinceAsync(Guid employeeId)
        {
            return await _context.Applications
                .Where(application => application.UserId == employeeId)
                .OrderBy(application => application.DateTime)
                .Select(application => (DateTime?)application.DateTime)
                .FirstOrDefaultAsync();
        }

        public async Task<List<(Application Application, JobPost JobPost, Employer Employer, RestaurantLocation? Location)>> GetArchivedPlatformShiftsForEmployeePagedAsync(
            Guid employeeId,
            DateTime utcNow,
            int page,
            int pageSize)
        {
            var archiveCutoff = utcNow.AddHours(-1);

            var rows = await (
                from application in _context.Applications
                join jobPost in _context.JobPosts on application.JobPostId equals jobPost.Id
                join employer in _context.Users.OfType<Employer>() on jobPost.EmployerId equals employer.Id
                join location in _context.RestaurantLocations on jobPost.RestaurantLocationId equals location.Id into locationGroup
                from location in locationGroup.DefaultIfEmpty()
                where application.UserId == employeeId
                      && application.Status == ApplicationStatusEnum.Accepted
                      && (jobPost.Status == JobStatusEnum.Cancelled
                          || jobPost.Status == JobStatusEnum.Completed
                          || jobPost.Status == JobStatusEnum.Expired
                          || jobPost.StartingDate < archiveCutoff)
                orderby jobPost.StartingDate descending
                select new { application, jobPost, employer, location })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return rows
                .Select(row => (row.application, row.jobPost, row.employer, row.location))
                .ToList();
        }
    }
}

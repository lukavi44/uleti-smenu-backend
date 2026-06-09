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

        public async Task AddAsync(Application application)
        {
            await _context.Applications.AddAsync(application);
        }

        public async Task<Application?> GetByIdAsync(Guid applicationId)
        {
            return await _context.Applications.FirstOrDefaultAsync(x => x.Id == applicationId);
        }

        public async Task<List<ApplicationApplicantDTO>> GetApplicantsForJobPostAsync(Guid jobPostId)
        {
            return await (from application in _context.Applications
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
                              Status = application.Status.ToString(),
                              AppliedAt = application.DateTime
                          }).ToListAsync();
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
    }
}

using Core.DTOs;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Database.Repositories
{
    public class JobPostRepository : Repository<JobPost>, IJobPostRepository
    {
        public JobPostRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task AddJobPostAsync(JobPost jobPost)
        {
            await _context.JobPosts.AddAsync(jobPost);
        }

        public async Task DeleteJobPostAsync(JobPost jobPost)
        {
            _context.JobPosts.Remove(jobPost);
        }

        public async Task<IEnumerable<JobPost>> GetAllByEmployerIdAsync(Guid employerId)
        {
            return await _context.JobPosts
              .Include(jp => jp.Employer)
              .Include(jp => jp.RestaurantLocation)
              .Where(jp => jp.EmployerId == employerId)
              .ToListAsync();
        }

        public async Task<int> CountActiveByEmployerIdAsync(Guid employerId)
        {
            return await _context.JobPosts
                .CountAsync(jp => jp.EmployerId == employerId && jp.Status == JobStatusEnum.Active);
        }

        public async Task<List<string>> GetDistinctPositionsByEmployerIdAsync(Guid employerId)
        {
            return await _context.JobPosts
                .Where(jp => jp.EmployerId == employerId)
                .Select(jp => jp.Position)
                .Distinct()
                .OrderBy(position => position)
                .ToListAsync();
        }

        public async Task<(List<JobPost> Items, int TotalCount)> GetByEmployerIdPagedAsync(
            Guid employerId,
            DateTime utcNow,
            int page,
            int pageSize,
            string? position = null,
            string? status = null,
            string? lifecycle = null,
            string? sortBy = null,
            string? sortDirection = null,
            bool? hasApplicants = null,
            string? city = null,
            Guid? restaurantLocationId = null,
            int? minSalary = null,
            int? maxSalary = null)
        {
            var query = _context.JobPosts
                .Include(jp => jp.Employer)
                .Include(jp => jp.RestaurantLocation)
                .Where(jp => jp.EmployerId == employerId);

            if (!string.IsNullOrWhiteSpace(position))
            {
                var normalizedPosition = position.Trim();
                query = query.Where(jp => jp.Position == normalizedPosition);
            }

            if (!string.IsNullOrWhiteSpace(status)
                && Enum.TryParse<JobStatusEnum>(status, true, out var parsedStatus))
            {
                query = query.Where(jp => jp.Status == parsedStatus);
            }

            var normalizedLifecycle = lifecycle?.Trim().ToLowerInvariant();
            if (normalizedLifecycle == "active")
            {
                query = query.Where(jp =>
                    jp.Status != JobStatusEnum.Cancelled
                    && jp.Status != JobStatusEnum.Completed
                    && jp.Status != JobStatusEnum.Expired
                    && jp.StartingDate.AddHours(1) >= utcNow);
            }
            else if (normalizedLifecycle == "archived")
            {
                query = query.Where(jp =>
                    jp.Status == JobStatusEnum.Cancelled
                    || jp.Status == JobStatusEnum.Completed
                    || jp.Status == JobStatusEnum.Expired
                    || jp.StartingDate.AddHours(1) < utcNow);
            }

            if (hasApplicants == true)
            {
                query = query.Where(jp => _context.Applications.Any(application => application.JobPostId == jp.Id));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                var normalizedCity = city.Trim();
                query = query.Where(jp =>
                    jp.RestaurantLocation != null
                    && jp.RestaurantLocation.City == normalizedCity);
            }

            if (restaurantLocationId.HasValue)
            {
                query = query.Where(jp => jp.RestaurantLocationId == restaurantLocationId.Value);
            }

            if (minSalary.HasValue)
            {
                query = query.Where(jp => jp.Salary >= minSalary.Value);
            }

            if (maxSalary.HasValue)
            {
                query = query.Where(jp => jp.Salary <= maxSalary.Value);
            }

            var isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

            query = normalizedSortBy switch
            {
                "position" => isAscending
                    ? query.OrderBy(jp => jp.Position).ThenByDescending(jp => jp.CreatedAtUtc)
                    : query.OrderByDescending(jp => jp.Position).ThenByDescending(jp => jp.CreatedAtUtc),
                "startingdate" => isAscending
                    ? query.OrderBy(jp => jp.StartingDate).ThenByDescending(jp => jp.CreatedAtUtc)
                    : query.OrderByDescending(jp => jp.StartingDate).ThenByDescending(jp => jp.CreatedAtUtc),
                "applicantcount" => isAscending
                    ? query.OrderBy(jp => _context.Applications.Count(application => application.JobPostId == jp.Id))
                        .ThenByDescending(jp => jp.CreatedAtUtc)
                    : query.OrderByDescending(jp => _context.Applications.Count(application => application.JobPostId == jp.Id))
                        .ThenByDescending(jp => jp.CreatedAtUtc),
                _ => isAscending
                    ? query.OrderBy(jp => jp.CreatedAtUtc).ThenBy(jp => jp.StartingDate)
                    : query.OrderByDescending(jp => jp.CreatedAtUtc).ThenByDescending(jp => jp.StartingDate)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<EmployerDashboardSummaryDTO> GetEmployerDashboardSummaryAsync(Guid employerId)
        {
            var utcNow = DateTime.UtcNow;
            var activePostsQuery = _context.JobPosts
                .Where(jobPost =>
                    jobPost.EmployerId == employerId
                    && jobPost.Status != JobStatusEnum.Cancelled
                    && jobPost.Status != JobStatusEnum.Completed
                    && jobPost.Status != JobStatusEnum.Expired
                    && jobPost.StartingDate.AddHours(1) >= utcNow);

            var activeJobPostsCount = await activePostsQuery.CountAsync();

            var pendingApplicantsCount = await (
                from application in _context.Applications
                join jobPost in _context.JobPosts on application.JobPostId equals jobPost.Id
                where jobPost.EmployerId == employerId
                    && application.Status == ApplicationStatusEnum.Applied
                select application).CountAsync();

            var activePostsByLocationId = await activePostsQuery
                .Where(jobPost => jobPost.RestaurantLocationId != null)
                .GroupBy(jobPost => jobPost.RestaurantLocationId!.Value)
                .Select(group => new { LocationId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(x => x.LocationId, x => x.Count);

            return new EmployerDashboardSummaryDTO
            {
                ActiveJobPostsCount = activeJobPostsCount,
                PendingApplicantsCount = pendingApplicantsCount,
                ActivePostsByLocationId = activePostsByLocationId
            };
        }

        public async Task<IEnumerable<JobPost>> GetAllJobPostsAsync()
        {
            var response = await _context.JobPosts
            .Include(jp => jp.Employer)
            .Include(jp => jp.RestaurantLocation)
            .ToListAsync();

            return response;
        }

        public async Task<IEnumerable<JobPost>> GetVisibleJobPostsAsync(DateTime utcNow, string? sortBy = null, string? sortDirection = null)
        {
            var query = _context.JobPosts
                .Include(jp => jp.Employer)
                .Include(jp => jp.RestaurantLocation)
                .Where(jp =>
                    jp.Status == Core.Models.Enums.JobStatusEnum.Active
                    && (jp.VisibleUntil >= utcNow || jp.StartingDate.AddHours(1) >= utcNow));

            var isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

            query = normalizedSortBy switch
            {
                "salary" => isAscending
                    ? query.OrderBy(jp => jp.Salary).ThenBy(jp => jp.CreatedAtUtc)
                    : query.OrderByDescending(jp => jp.Salary).ThenByDescending(jp => jp.CreatedAtUtc),
                _ => isAscending
                    ? query.OrderBy(jp => jp.CreatedAtUtc).ThenBy(jp => jp.StartingDate)
                    : query.OrderByDescending(jp => jp.CreatedAtUtc).ThenByDescending(jp => jp.StartingDate)
            };

            return await query.ToListAsync();
        }

        public async Task<(List<JobPost> Items, int TotalCount)> GetVisibleJobPostsPagedAsync(
            DateTime utcNow,
            int page,
            int pageSize,
            string? sortBy = null,
            string? sortDirection = null,
            string? city = null,
            Guid? restaurantLocationId = null,
            string? position = null,
            IReadOnlyList<string>? positions = null,
            int? minSalary = null,
            int? maxSalary = null,
            DateTime? shiftDateFrom = null,
            DateTime? shiftDateTo = null,
            Guid? employeeId = null,
            string? applicationFilter = null,
            bool? favouritesOnly = null)
        {
            var query = _context.JobPosts
                .Include(jp => jp.Employer)
                .Include(jp => jp.RestaurantLocation)
                .Where(jp =>
                    jp.Status == JobStatusEnum.Active
                    && (jp.VisibleUntil >= utcNow || jp.StartingDate.AddHours(1) >= utcNow));

            if (!string.IsNullOrWhiteSpace(city))
            {
                var normalizedCity = city.Trim();
                query = query.Where(jp =>
                    jp.RestaurantLocation != null
                    && jp.RestaurantLocation.City == normalizedCity);
            }

            if (restaurantLocationId.HasValue)
            {
                query = query.Where(jp => jp.RestaurantLocationId == restaurantLocationId.Value);
            }

            if (positions is { Count: > 0 })
            {
                var normalizedPositions = positions
                    .Select(jobPosition => jobPosition.Trim())
                    .Where(jobPosition => !string.IsNullOrWhiteSpace(jobPosition))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (normalizedPositions.Count > 0)
                {
                    query = query.Where(jp => normalizedPositions.Contains(jp.Position));
                }
            }
            else if (!string.IsNullOrWhiteSpace(position))
            {
                var normalizedPosition = position.Trim();
                query = query.Where(jp => jp.Position == normalizedPosition);
            }

            if (minSalary.HasValue)
            {
                query = query.Where(jp => jp.Salary >= minSalary.Value);
            }

            if (maxSalary.HasValue)
            {
                query = query.Where(jp => jp.Salary <= maxSalary.Value);
            }

            if (shiftDateFrom.HasValue)
            {
                query = query.Where(jp => jp.StartingDate >= shiftDateFrom.Value);
            }

            if (shiftDateTo.HasValue)
            {
                query = query.Where(jp => jp.StartingDate <= shiftDateTo.Value);
            }

            if (employeeId.HasValue)
            {
                var normalizedApplicationFilter = applicationFilter?.Trim().ToLowerInvariant();
                if (normalizedApplicationFilter == "applied")
                {
                    query = query.Where(jp =>
                        _context.Applications.Any(application =>
                            application.JobPostId == jp.Id
                            && application.UserId == employeeId.Value));
                }
                else if (normalizedApplicationFilter == "notapplied")
                {
                    query = query.Where(jp =>
                        !_context.Applications.Any(application =>
                            application.JobPostId == jp.Id
                            && application.UserId == employeeId.Value));
                }

                if (favouritesOnly == true)
                {
                    query = query.Where(jp =>
                        _context.Favourites.Any(favourite =>
                            favourite.EmployeeId == employeeId.Value
                            && favourite.EmployerId == jp.EmployerId));
                }
            }

            var isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();

            query = normalizedSortBy switch
            {
                "salary" => isAscending
                    ? query.OrderBy(jp => jp.Salary).ThenBy(jp => jp.CreatedAtUtc)
                    : query.OrderByDescending(jp => jp.Salary).ThenByDescending(jp => jp.CreatedAtUtc),
                _ => isAscending
                    ? query.OrderBy(jp => jp.CreatedAtUtc).ThenBy(jp => jp.StartingDate)
                    : query.OrderByDescending(jp => jp.CreatedAtUtc).ThenByDescending(jp => jp.StartingDate)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<List<JobPost>> GetCandidateRecommendedJobPostsAsync(
            Guid employeeId,
            string city,
            DateTime utcNow,
            int pageSize)
        {
            var normalizedCity = city.Trim();

            return await _context.JobPosts
                .Include(jp => jp.Employer)
                .Include(jp => jp.RestaurantLocation)
                .Where(jp =>
                    jp.Status == JobStatusEnum.Active
                    && jp.StartingDate > utcNow
                    && jp.RestaurantLocation != null
                    && jp.RestaurantLocation.City == normalizedCity
                    && !_context.Applications.Any(application =>
                        application.JobPostId == jp.Id
                        && application.UserId == employeeId))
                .OrderBy(jp => jp.StartingDate)
                .ThenByDescending(jp => jp.CreatedAtUtc)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<VisibleJobPostFilterOptionsDTO> GetVisibleJobPostFilterOptionsAsync(DateTime utcNow, string? city = null)
        {
            var baseQuery = _context.JobPosts
                .Include(jp => jp.RestaurantLocation)
                .Where(jp =>
                    jp.Status == JobStatusEnum.Active
                    && (jp.VisibleUntil >= utcNow || jp.StartingDate.AddHours(1) >= utcNow)
                    && jp.RestaurantLocation != null);

            var cities = await baseQuery
                .Select(jp => jp.RestaurantLocation!.City)
                .Where(locationCity => locationCity != null && locationCity != "")
                .Distinct()
                .OrderBy(locationCity => locationCity)
                .ToListAsync();

            var locationsQuery = baseQuery.Where(jp => jp.RestaurantLocationId.HasValue);
            if (!string.IsNullOrWhiteSpace(city))
            {
                var normalizedCity = city.Trim();
                locationsQuery = locationsQuery.Where(jp => jp.RestaurantLocation!.City == normalizedCity);
            }

            var locations = await locationsQuery
                .Select(jp => new VisibleJobPostLocationOptionDTO
                {
                    Id = jp.RestaurantLocationId!.Value,
                    Name = jp.RestaurantLocation!.Name,
                    City = jp.RestaurantLocation.City
                })
                .Distinct()
                .OrderBy(location => location.Name)
                .ThenBy(location => location.City)
                .ToListAsync();

            var positions = await baseQuery
                .Select(jp => jp.Position)
                .Distinct()
                .OrderBy(jobPosition => jobPosition)
                .ToListAsync();

            var salaryBounds = await baseQuery
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    MinSalary = group.Min(jp => jp.Salary),
                    MaxSalary = group.Max(jp => jp.Salary)
                })
                .FirstOrDefaultAsync();

            return new VisibleJobPostFilterOptionsDTO
            {
                Cities = cities,
                Locations = locations,
                Positions = positions,
                MinSalary = salaryBounds?.MinSalary,
                MaxSalary = salaryBounds?.MaxSalary
            };
        }

        public async Task<JobPost?> GetJobPostByIdAsync(Guid id)
        {
            return await _context.JobPosts
              .Include(jp => jp.Employer)
              .Include(jp => jp.RestaurantLocation)
              .FirstOrDefaultAsync(jp => jp.Id == id);
        }

        public async Task<JobPost?> GetVisibleJobPostByIdAsync(Guid id, DateTime utcNow)
        {
            return await _context.JobPosts
                .Include(jp => jp.Employer)
                .Include(jp => jp.RestaurantLocation)
                .FirstOrDefaultAsync(jp =>
                    jp.Id == id
                    && jp.Status == JobStatusEnum.Active
                    && (jp.VisibleUntil >= utcNow || jp.StartingDate.AddHours(1) >= utcNow));
        }
    }
}

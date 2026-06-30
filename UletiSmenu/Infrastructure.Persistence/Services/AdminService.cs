using Core.DTOs.Admin;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Services;
using CSharpFunctionalExtensions;
using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Services
{
    public class AdminService : IAdminService
    {
        private const string ApplicationAcceptedNotificationType = "ApplicationAccepted";

        private readonly ApplicationDbContext _context;

        public AdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardDTO> GetDashboardAsync(DateTime? fromUtc, DateTime? toUtc)
        {
            var utcNow = DateTime.UtcNow;
            var rangeStart = fromUtc?.Date ?? utcNow.Date.AddDays(-6);
            var rangeEnd = (toUtc?.Date ?? utcNow.Date).AddDays(1).AddTicks(-1);

            var monthStart = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var totalCandidates = await _context.Users.OfType<Employee>().CountAsync();
            var totalEmployers = await _context.Users.OfType<Employer>().CountAsync();
            var activeJobPosts = await _context.JobPosts
                .CountAsync(post => post.Status == JobStatusEnum.Active);
            var acceptedAllTime = await _context.Applications
                .CountAsync(application => application.Status == ApplicationStatusEnum.Accepted);
            var completedShiftsAllTime = await _context.JobPosts
                .CountAsync(post => post.Status == JobStatusEnum.Completed);

            var walletTopUpsThisMonth = await _context.WalletTransactions
                .Where(transaction =>
                    transaction.Type == WalletTransactionType.TopUp &&
                    transaction.CreatedAtUtc >= monthStart)
                .SumAsync(transaction => (decimal?)transaction.Amount) ?? 0m;

            var applicationDates = await _context.Applications
                .Where(application =>
                    application.DateTime >= rangeStart &&
                    application.DateTime <= rangeEnd)
                .Select(application => application.DateTime)
                .ToListAsync();

            var applicationsInRange = applicationDates
                .GroupBy(date => date.Date)
                .ToDictionary(group => group.Key, group => group.Count());

            var chartPoints = new List<AdminDashboardChartPointDTO>();
            for (var day = rangeStart.Date; day <= rangeEnd.Date; day = day.AddDays(1))
            {
                applicationsInRange.TryGetValue(day, out var count);
                chartPoints.Add(new AdminDashboardChartPointDTO { Date = day, Count = count });
            }

            var recentActivities = await BuildRecentActivitiesAsync(utcNow);

            return new AdminDashboardDTO
            {
                TotalCandidates = totalCandidates,
                TotalEmployers = totalEmployers,
                ActiveJobPosts = activeJobPosts,
                ReportsCount = 0,
                WalletTopUpsThisMonth = walletTopUpsThisMonth,
                AcceptedCandidatesAllTime = acceptedAllTime,
                CompletedShiftsAllTime = completedShiftsAllTime,
                ApplicationsChart = chartPoints,
                RecentActivities = recentActivities
            };
        }

        public async Task<AdminEmployerListResponseDTO> GetEmployersAsync(
            string? search,
            string? status,
            string? city,
            int page,
            int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.Users.OfType<Employer>().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(employer =>
                    employer.Name.Contains(term) ||
                    (employer.Email != null && employer.Email.Contains(term)) ||
                    employer.PIB.Value.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                var cityTerm = city.Trim();
                query = query.Where(employer => employer.Address.City.Name.Contains(cityTerm));
            }

            var employers = await query
                .OrderByDescending(employer => employer.Name)
                .ToListAsync();

            var employerIds = employers.Select(employer => employer.Id).ToList();
            var createdAtLookup = await _context.JobPosts
                .Where(post => employerIds.Contains(post.EmployerId))
                .GroupBy(post => post.EmployerId)
                .Select(group => new
                {
                    EmployerId = group.Key,
                    CreatedAtUtc = group.Min(post => post.CreatedAtUtc)
                })
                .ToDictionaryAsync(item => item.EmployerId, item => item.CreatedAtUtc);

            var items = employers
                .Select(employer =>
                {
                    var employerStatus = ResolveEmployerStatus(employer);
                    return new AdminEmployerListItemDTO
                    {
                        Id = employer.Id,
                        Name = employer.Name,
                        Email = employer.Email ?? string.Empty,
                        PIB = employer.PIB.Value,
                        City = employer.Address.City.Name,
                        Status = employerStatus,
                        IsVerifiedEmployer = employer.IsVerifiedEmployer,
                        CreatedAtUtc = createdAtLookup.TryGetValue(employer.Id, out var createdAt)
                            ? createdAt
                            : employer.SubscriptionStart,
                        ProfilePhoto = employer.ProfilePhoto
                    };
                })
                .Where(item => string.IsNullOrWhiteSpace(status) ||
                               status.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                               item.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var totalCount = items.Count;
            var pageItems = items
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new AdminEmployerListResponseDTO
            {
                Items = pageItems,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<Result<AdminEmployerDetailDTO>> GetEmployerDetailAsync(Guid employerId)
        {
            var employer = await _context.Users.OfType<Employer>()
                .FirstOrDefaultAsync(item => item.Id == employerId);

            if (employer == null)
                return Result.Failure<AdminEmployerDetailDTO>("Employer not found.");

            var detail = await MapEmployerDetailAsync(employer);
            return Result.Success(detail);
        }

        public async Task<Result<AdminEmployerDetailDTO>> SetEmployerVerificationAsync(
            Guid employerId,
            bool isVerified,
            Guid adminUserId)
        {
            var employer = await _context.Users.OfType<Employer>()
                .FirstOrDefaultAsync(item => item.Id == employerId);

            if (employer == null)
                return Result.Failure<AdminEmployerDetailDTO>("Employer not found.");

            var verificationResult = employer.SetVerification(isVerified, isVerified ? adminUserId : null, DateTime.UtcNow);
            if (verificationResult.IsFailure)
                return Result.Failure<AdminEmployerDetailDTO>(verificationResult.Error);

            await _context.SaveChangesAsync();

            var detail = await MapEmployerDetailAsync(employer);
            return Result.Success(detail);
        }

        public async Task<AdminPagedResponseDTO<AdminCandidateListItemDTO>> GetCandidatesAsync(
            string? search,
            string? city,
            int page,
            int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.Users.OfType<Employee>().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(employee =>
                    employee.FirstName.Contains(term) ||
                    employee.LastName.Contains(term) ||
                    (employee.Email != null && employee.Email.Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                var cityTerm = city.Trim();
                query = query.Where(employee => employee.City != null && employee.City.Contains(cityTerm));
            }

            var totalCount = await query.CountAsync();
            var employees = await query
                .OrderByDescending(employee => employee.LastName)
                .ThenBy(employee => employee.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var employeeIds = employees.Select(employee => employee.Id).ToList();
            var applicationCounts = await _context.Applications
                .Where(application => employeeIds.Contains(application.UserId))
                .GroupBy(application => application.UserId)
                .Select(group => new { UserId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.UserId, item => item.Count);

            return new AdminPagedResponseDTO<AdminCandidateListItemDTO>
            {
                Items = employees.Select(employee => new AdminCandidateListItemDTO
                {
                    Id = employee.Id,
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    Email = employee.Email ?? string.Empty,
                    PhoneNumber = employee.PhoneNumber ?? string.Empty,
                    City = employee.City,
                    ProfilePhoto = employee.ProfilePhoto,
                    ApplicationsCount = applicationCounts.GetValueOrDefault(employee.Id)
                }).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<AdminPagedResponseDTO<AdminRestaurantListItemDTO>> GetRestaurantsAsync(
            string? search,
            string? city,
            int page,
            int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.RestaurantLocations.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(location =>
                    location.Name.Contains(term) ||
                    location.Employer.Name.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                var cityTerm = city.Trim();
                query = query.Where(location => location.City.Contains(cityTerm));
            }

            var totalCount = await query.CountAsync();
            var locations = await query
                .OrderBy(location => location.Employer.Name)
                .ThenBy(location => location.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(location => new AdminRestaurantListItemDTO
                {
                    Id = location.Id,
                    EmployerId = location.EmployerId,
                    EmployerName = location.Employer.Name,
                    Name = location.Name,
                    City = location.City,
                    PhoneNumber = location.PhoneNumber
                })
                .ToListAsync();

            return new AdminPagedResponseDTO<AdminRestaurantListItemDTO>
            {
                Items = locations,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<AdminPagedResponseDTO<AdminJobPostListItemDTO>> GetJobPostsAsync(
            string? search,
            string? status,
            int page,
            int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.JobPosts.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(post =>
                    post.Title.Contains(term) ||
                    post.Position.Contains(term) ||
                    post.Employer.Name.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<JobStatusEnum>(status, true, out var parsedStatus))
            {
                query = query.Where(post => post.Status == parsedStatus);
            }

            var totalCount = await query.CountAsync();
            var posts = await query
                .OrderByDescending(post => post.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(post => new AdminJobPostListItemDTO
                {
                    Id = post.Id,
                    Title = post.Title,
                    Position = post.Position,
                    EmployerName = post.Employer.Name,
                    LocationName = post.RestaurantLocation != null ? post.RestaurantLocation.Name : null,
                    Status = post.Status.ToString(),
                    ApplicationsCount = _context.Applications.Count(application => application.JobPostId == post.Id),
                    CreatedAtUtc = post.CreatedAtUtc
                })
                .ToListAsync();

            return new AdminPagedResponseDTO<AdminJobPostListItemDTO>
            {
                Items = posts,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<AdminPagedResponseDTO<AdminApplicationListItemDTO>> GetApplicationsAsync(
            string? search,
            string? status,
            int page,
            int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.Applications.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<ApplicationStatusEnum>(status, true, out var parsedStatus))
            {
                query = query.Where(application => application.Status == parsedStatus);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(application =>
                    _context.Users.OfType<Employee>().Any(employee =>
                        employee.Id == application.UserId &&
                        (employee.FirstName.Contains(term) || employee.LastName.Contains(term))) ||
                    _context.JobPosts.Any(post =>
                        post.Id == application.JobPostId &&
                        (post.Title.Contains(term) || post.Employer.Name.Contains(term))));
            }

            var totalCount = await query.CountAsync();
            var applications = await query
                .OrderByDescending(application => application.DateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userIds = applications.Select(application => application.UserId).Distinct().ToList();
            var jobPostIds = applications.Select(application => application.JobPostId).Distinct().ToList();

            var employees = await _context.Users.OfType<Employee>()
                .Where(employee => userIds.Contains(employee.Id))
                .ToDictionaryAsync(employee => employee.Id);

            var jobPosts = await _context.JobPosts
                .Where(post => jobPostIds.Contains(post.Id))
                .Select(post => new { post.Id, post.Title, EmployerName = post.Employer.Name })
                .ToDictionaryAsync(post => post.Id);

            return new AdminPagedResponseDTO<AdminApplicationListItemDTO>
            {
                Items = applications.Select(application =>
                {
                    employees.TryGetValue(application.UserId, out var employee);
                    jobPosts.TryGetValue(application.JobPostId, out var jobPost);
                    return new AdminApplicationListItemDTO
                    {
                        Id = application.Id,
                        CandidateName = employee == null
                            ? "—"
                            : $"{employee.FirstName} {employee.LastName}".Trim(),
                        JobTitle = jobPost?.Title ?? "—",
                        EmployerName = jobPost?.EmployerName ?? "—",
                        Status = application.Status.ToString(),
                        AppliedAtUtc = application.DateTime
                    };
                }).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<AdminPagedResponseDTO<AdminBillingListItemDTO>> GetBillingTransactionsAsync(
            string? search,
            int page,
            int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.WalletTransactions.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(transaction =>
                    _context.Users.OfType<Employer>().Any(employer =>
                        employer.Id == transaction.EmployerId && employer.Name.Contains(term)));
            }

            var totalCount = await query.CountAsync();
            var transactions = await query
                .OrderByDescending(transaction => transaction.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Join(
                    _context.Users.OfType<Employer>(),
                    transaction => transaction.EmployerId,
                    employer => employer.Id,
                    (transaction, employer) => new AdminBillingListItemDTO
                    {
                        Id = transaction.Id,
                        EmployerName = employer.Name,
                        Amount = transaction.Amount,
                        Type = transaction.Type.ToString(),
                        Description = transaction.Description,
                        CreatedAtUtc = transaction.CreatedAtUtc
                    })
                .ToListAsync();

            return new AdminPagedResponseDTO<AdminBillingListItemDTO>
            {
                Items = transactions,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        private async Task<AdminEmployerDetailDTO> MapEmployerDetailAsync(Employer employer)
        {
            var employerId = employer.Id;

            var activeJobPostsCount = await _context.JobPosts
                .CountAsync(post => post.EmployerId == employerId && post.Status == JobStatusEnum.Active);
            var totalJobPostsCount = await _context.JobPosts
                .CountAsync(post => post.EmployerId == employerId);
            var completedShiftsCount = await _context.JobPosts
                .CountAsync(post => post.EmployerId == employerId && post.Status == JobStatusEnum.Completed);
            var acceptedCandidatesAllTime = await _context.Applications
                .CountAsync(application =>
                    application.Status == ApplicationStatusEnum.Accepted &&
                    _context.JobPosts.Any(post => post.Id == application.JobPostId && post.EmployerId == employerId));

            var reviewStats = await _context.MatchReviews
                .Where(review => review.RevieweeId == employerId)
                .GroupBy(review => review.RevieweeId)
                .Select(group => new
                {
                    AverageRating = group.Average(review => (double)review.Rating),
                    Count = group.Count()
                })
                .FirstOrDefaultAsync();

            var createdAtUtc = await _context.JobPosts
                .Where(post => post.EmployerId == employerId)
                .MinAsync(post => (DateTime?)post.CreatedAtUtc);

            string? verifiedByLabel = null;
            if (employer.VerifiedByUserId.HasValue)
            {
                var verifier = await _context.Users
                    .Where(user => user.Id == employer.VerifiedByUserId.Value)
                    .Select(user => user.Email)
                    .FirstOrDefaultAsync();
                verifiedByLabel = string.IsNullOrWhiteSpace(verifier) ? "Admin" : verifier;
            }

            string? subscriptionPlanName = null;
            if (employer.SubscriptionId.HasValue)
            {
                subscriptionPlanName = await _context.Subscriptions
                    .Where(plan => plan.Id == employer.SubscriptionId.Value)
                    .Select(plan => plan.Title)
                    .FirstOrDefaultAsync();
            }

            return new AdminEmployerDetailDTO
            {
                Id = employer.Id,
                Name = employer.Name,
                Email = employer.Email ?? string.Empty,
                PhoneNumber = employer.PhoneNumber ?? string.Empty,
                ProfilePhoto = employer.ProfilePhoto,
                PIB = employer.PIB.Value,
                MB = employer.MB.Value,
                StreetName = employer.Address.Street.Name,
                StreetNumber = employer.Address.Street.Number,
                City = employer.Address.City.Name,
                PostalCode = employer.Address.City.PostalCode.Value,
                Country = employer.Address.City.Country.Name,
                Region = employer.Address.City.Region.Name,
                Status = ResolveEmployerStatus(employer),
                IsVerifiedEmployer = employer.IsVerifiedEmployer,
                VerifiedAtUtc = employer.VerifiedAtUtc,
                VerifiedByLabel = verifiedByLabel,
                BillingStatus = employer.BillingStatus.ToString(),
                SubscriptionPlanName = subscriptionPlanName,
                SubscriptionStop = employer.SubscriptionStop,
                WalletBalance = employer.WalletBalance,
                ActiveJobPostsCount = activeJobPostsCount,
                TotalJobPostsCount = totalJobPostsCount,
                CompletedShiftsCount = completedShiftsCount,
                AcceptedCandidatesAllTime = acceptedCandidatesAllTime,
                AverageRating = reviewStats?.AverageRating,
                ReviewCount = reviewStats?.Count ?? 0,
                CreatedAtUtc = createdAtUtc ?? employer.SubscriptionStart
            };
        }

        private static string ResolveEmployerStatus(Employer employer)
        {
            if (employer.LockoutEnabled && employer.LockoutEnd.HasValue && employer.LockoutEnd.Value > DateTimeOffset.UtcNow)
                return "Suspended";

            return "Active";
        }

        private async Task<List<AdminRecentActivityDTO>> BuildRecentActivitiesAsync(DateTime utcNow)
        {
            var since = utcNow.AddDays(-14);
            var activities = new List<AdminRecentActivityDTO>();

            var recentEmployerRows = await _context.JobPosts
                .Where(post => post.CreatedAtUtc >= since)
                .GroupBy(post => post.EmployerId)
                .Select(group => new
                {
                    EmployerId = group.Key,
                    FirstCreated = group.Min(post => post.CreatedAtUtc)
                })
                .OrderByDescending(row => row.FirstCreated)
                .Take(10)
                .ToListAsync();

            if (recentEmployerRows.Count > 0)
            {
                var employerIds = recentEmployerRows.Select(row => row.EmployerId).ToList();
                var employers = await _context.Users.OfType<Employer>()
                    .Where(employer => employerIds.Contains(employer.Id))
                    .ToDictionaryAsync(employer => employer.Id);

                activities.AddRange(recentEmployerRows
                    .Where(row => employers.ContainsKey(row.EmployerId))
                    .Select(row =>
                    {
                        var employer = employers[row.EmployerId];
                        return new AdminRecentActivityDTO
                        {
                            Type = "EmployerRegistered",
                            Title = employer.Name,
                            Subtitle = employer.Address.City.Name,
                            OccurredAtUtc = row.FirstCreated,
                            RelatedEntityId = employer.Id
                        };
                    }));
            }

            var recentJobPostRows = await _context.JobPosts
                .Where(post => post.CreatedAtUtc >= since)
                .OrderByDescending(post => post.CreatedAtUtc)
                .Take(10)
                .Select(post => new
                {
                    post.Id,
                    post.Title,
                    post.CreatedAtUtc,
                    EmployerName = post.Employer.Name
                })
                .ToListAsync();

            activities.AddRange(recentJobPostRows.Select(row => new AdminRecentActivityDTO
            {
                Type = "JobPostCreated",
                Title = row.Title,
                Subtitle = row.EmployerName,
                OccurredAtUtc = row.CreatedAtUtc,
                RelatedEntityId = row.Id
            }));

            var acceptedNotificationRows = await _context.Notifications
                .Where(notification =>
                    notification.Type == ApplicationAcceptedNotificationType &&
                    notification.CreatedAtUtc >= since)
                .OrderByDescending(notification => notification.CreatedAtUtc)
                .Take(10)
                .Join(
                    _context.JobPosts,
                    notification => notification.JobPostId,
                    jobPost => jobPost.Id,
                    (notification, jobPost) => new
                    {
                        notification.Id,
                        notification.CreatedAtUtc,
                        jobPost.Title,
                        EmployerName = jobPost.Employer.Name
                    })
                .ToListAsync();

            activities.AddRange(acceptedNotificationRows.Select(row => new AdminRecentActivityDTO
            {
                Type = "CandidateAccepted",
                Title = row.Title,
                Subtitle = row.EmployerName,
                OccurredAtUtc = row.CreatedAtUtc,
                RelatedEntityId = row.Id
            }));

            var walletTopUpRows = await _context.WalletTransactions
                .Where(transaction =>
                    transaction.Type == WalletTransactionType.TopUp &&
                    transaction.CreatedAtUtc >= since)
                .OrderByDescending(transaction => transaction.CreatedAtUtc)
                .Take(10)
                .Join(
                    _context.Users.OfType<Employer>(),
                    transaction => transaction.EmployerId,
                    employer => employer.Id,
                    (transaction, employer) => new
                    {
                        transaction.Id,
                        transaction.Amount,
                        transaction.CreatedAtUtc,
                        employer.Name
                    })
                .ToListAsync();

            activities.AddRange(walletTopUpRows.Select(row => new AdminRecentActivityDTO
            {
                Type = "WalletTopUp",
                Title = row.Name,
                Subtitle = $"{row.Amount:N0} RSD",
                OccurredAtUtc = row.CreatedAtUtc,
                RelatedEntityId = row.Id
            }));

            return activities
                .OrderByDescending(activity => activity.OccurredAtUtc)
                .Take(20)
                .ToList();
        }
    }
}

using Core.DTOs;
using Core.Helpers;
using Core.Models.Entities;
using Core.Repositories;
using Core.Services;
using CSharpFunctionalExtensions;

namespace Infrastructure.Persistence.Services
{
    public class EmployerProfileService : IEmployerProfileService
    {
        private const int ActiveJobPostsLimit = 20;

        private readonly IUserRepository _userRepository;
        private readonly IRestaurantLocationRepository _restaurantLocationRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IJobPostRepository _jobPostRepository;
        private readonly IApplicationUnitOfWork _applicationUnitOfWork;

        public EmployerProfileService(
            IUserRepository userRepository,
            IRestaurantLocationRepository restaurantLocationRepository,
            IReviewRepository reviewRepository,
            IJobPostRepository jobPostRepository,
            IApplicationUnitOfWork applicationUnitOfWork)
        {
            _userRepository = userRepository;
            _restaurantLocationRepository = restaurantLocationRepository;
            _reviewRepository = reviewRepository;
            _jobPostRepository = jobPostRepository;
            _applicationUnitOfWork = applicationUnitOfWork;
        }

        public Task<Result<EmployerPublicProfileDTO>> GetEmployerPublicProfileAsync(Guid employerId, Guid? employeeId) =>
            BuildEmployerPublicProfileAsync(employerId, employeeId);

        public async Task<Result<EmployerPublicProfileDTO>> GetEmployerPublicProfileBySlugAsync(string slug, Guid? employeeId)
        {
            var employer = await _userRepository.FindEmployerByPublicSlugAsync(slug);
            if (employer == null)
                return Result.Failure<EmployerPublicProfileDTO>("Employer not found.");

            return await BuildEmployerPublicProfileAsync(employer.Id, employeeId);
        }

        public Task<Result<EmployerDirectoryPreviewDTO>> GetEmployerDirectoryPreviewAsync(Guid employerId) =>
            BuildEmployerDirectoryPreviewAsync(employerId);

        public async Task<Result<EmployerDirectoryPreviewDTO>> GetEmployerDirectoryPreviewBySlugAsync(string slug)
        {
            var employer = await _userRepository.FindEmployerByPublicSlugAsync(slug);
            if (employer == null)
                return Result.Failure<EmployerDirectoryPreviewDTO>("Employer not found.");

            return await BuildEmployerDirectoryPreviewAsync(employer.Id);
        }

        public async Task<Result<string>> ResolveEmployerSlugAsync(Guid employerId)
        {
            var employerResult = await _userRepository.GetEmployerByIdAsync(employerId);
            if (employerResult.IsFailure)
                return Result.Failure<string>(employerResult.Error);

            var employer = employerResult.Value;
            await EnsurePublicSlugAsync(employer);
            return Result.Success(employer.PublicSlug);
        }

        private async Task<Result<EmployerPublicProfileDTO>> BuildEmployerPublicProfileAsync(Guid employerId, Guid? employeeId)
        {
            var employerResult = await _userRepository.GetEmployerByIdAsync(employerId);
            if (employerResult.IsFailure)
                return Result.Failure<EmployerPublicProfileDTO>(employerResult.Error);

            var employer = employerResult.Value;
            await EnsurePublicSlugAsync(employer);

            var locations = await _restaurantLocationRepository.GetByEmployerIdAsync(employerId);
            var reviewSummary = await _reviewRepository.GetEmployerReviewSummaryAsync(employerId);

            var utcNow = DateTime.UtcNow;
            var (jobPosts, _) = await _jobPostRepository.GetByEmployerIdPagedAsync(
                employerId,
                utcNow,
                page: 1,
                pageSize: ActiveJobPostsLimit,
                lifecycle: "active",
                sortBy: "startingDate",
                sortDirection: "asc");

            bool? isFavourite = null;
            if (employeeId.HasValue)
            {
                var favourite = await _applicationUnitOfWork.Favourites.GetByIdAsync(employeeId.Value, employerId);
                isFavourite = favourite.HasValue;
            }

            return Result.Success(new EmployerPublicProfileDTO
            {
                EmployerId = employer.Id,
                Name = employer.Name,
                ProfilePhoto = employer.ProfilePhoto,
                PhoneNumber = employer.PhoneNumber ?? string.Empty,
                PublicSlug = employer.PublicSlug,
                IsFavourite = isFavourite,
                Locations = locations.Select(MapLocation).ToList(),
                ReviewSummary = reviewSummary,
                ActiveJobPosts = jobPosts.Select(MapJobPost).ToList()
            });
        }

        private async Task<Result<EmployerDirectoryPreviewDTO>> BuildEmployerDirectoryPreviewAsync(Guid employerId)
        {
            var employerResult = await _userRepository.GetEmployerByIdAsync(employerId);
            if (employerResult.IsFailure)
                return Result.Failure<EmployerDirectoryPreviewDTO>(employerResult.Error);

            var employer = employerResult.Value;
            await EnsurePublicSlugAsync(employer);

            var locations = await _restaurantLocationRepository.GetByEmployerIdAsync(employerId);
            var reviewSummary = await _reviewRepository.GetEmployerReviewSummaryAsync(employerId);

            var utcNow = DateTime.UtcNow;
            var (_, activeJobPostsCount) = await _jobPostRepository.GetByEmployerIdPagedAsync(
                employerId,
                utcNow,
                page: 1,
                pageSize: 1,
                lifecycle: "active",
                sortBy: "startingDate",
                sortDirection: "asc");

            var cities = locations
                .Select(location => location.City)
                .Where(city => !string.IsNullOrWhiteSpace(city))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(city => city)
                .ToList();

            return Result.Success(new EmployerDirectoryPreviewDTO
            {
                EmployerId = employer.Id,
                Name = employer.Name,
                ProfilePhoto = employer.ProfilePhoto,
                PublicSlug = employer.PublicSlug,
                City = cities.Count > 0 ? string.Join(", ", cities) : string.Empty,
                ReviewSummary = reviewSummary,
                ActiveJobPostsCount = activeJobPostsCount
            });
        }

        private async Task EnsurePublicSlugAsync(Employer employer)
        {
            if (!string.IsNullOrWhiteSpace(employer.PublicSlug))
                return;

            var baseSlug = EmployerSlugHelper.Slugify(employer.Name);
            var slug = baseSlug;
            var suffix = 2;

            while (await _userRepository.PublicSlugExistsAsync(slug, employer.Id))
            {
                slug = $"{baseSlug}-{suffix}";
                suffix++;
            }

            var setSlugResult = employer.SetPublicSlug(slug);
            if (setSlugResult.IsFailure)
                throw new InvalidOperationException(setSlugResult.Error);

            await _applicationUnitOfWork.SaveChangesAsync();
        }

        private static EmployerLocationDTO MapLocation(RestaurantLocation location)
        {
            return new EmployerLocationDTO
            {
                Id = location.Id,
                Name = location.Name,
                PhoneNumber = location.PhoneNumber,
                StreetName = location.StreetName,
                StreetNumber = location.StreetNumber,
                City = location.City,
                PostalCode = location.PostalCode,
                Country = location.Country,
                Region = location.Region
            };
        }

        private static EmployerJobPostSummaryDTO MapJobPost(JobPost jobPost)
        {
            return new EmployerJobPostSummaryDTO
            {
                Id = jobPost.Id,
                Title = jobPost.Title,
                Position = jobPost.Position,
                Salary = jobPost.Salary,
                StartingDate = jobPost.StartingDate,
                RestaurantLocationName = jobPost.RestaurantLocation?.Name,
                RestaurantLocationCity = jobPost.RestaurantLocation?.City
            };
        }

        public async Task<PagedResultDTO<EmployerDirectoryListItemDTO>> GetEmployerDirectoryPagedAsync(
            string? city,
            string? search,
            int page,
            int pageSize,
            Guid? employeeId)
        {
            var safePage = page < 1 ? 1 : page;
            var safePageSize = pageSize < 1 ? 9 : Math.Min(pageSize, 50);

            var employers = (await _userRepository.GetAllEmployersAsync()).ToList();
            employers = (await FilterEmployersByCityAsync(employers, city)).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim();
                employers = employers
                    .Where(employer =>
                        employer.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            employers = employers
                .OrderBy(employer => employer.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var totalCount = employers.Count;
            var pagedEmployers = employers
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .ToList();

            var favouriteEmployerIds = employeeId.HasValue
                ? (await _applicationUnitOfWork.Favourites
                    .GetEmployerIdsFavouritedByEmployeeAsync(employeeId.Value))
                    .ToHashSet()
                : new HashSet<Guid>();

            var items = new List<EmployerDirectoryListItemDTO>();
            foreach (var employer in pagedEmployers)
            {
                var previewResult = await BuildEmployerDirectoryPreviewAsync(employer.Id);
                if (previewResult.IsFailure)
                {
                    continue;
                }

                var preview = previewResult.Value;
                items.Add(new EmployerDirectoryListItemDTO
                {
                    EmployerId = preview.EmployerId,
                    Name = preview.Name,
                    ProfilePhoto = preview.ProfilePhoto,
                    PublicSlug = preview.PublicSlug,
                    City = preview.City,
                    ReviewSummary = preview.ReviewSummary,
                    ActiveJobPostsCount = preview.ActiveJobPostsCount,
                    IsFavourite = favouriteEmployerIds.Contains(preview.EmployerId)
                });
            }

            return new PagedResultDTO<EmployerDirectoryListItemDTO>
            {
                Items = items,
                TotalCount = totalCount,
                Page = safePage,
                PageSize = safePageSize
            };
        }

        private async Task<IEnumerable<Employer>> FilterEmployersByCityAsync(IEnumerable<Employer> employers, string? city)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return employers;
            }

            var normalizedCity = city.Trim();
            var employerIdsWithBranch = (await _restaurantLocationRepository
                .GetEmployerIdsByCityAsync(normalizedCity))
                .ToHashSet();

            return employers.Where(employer =>
                employerIdsWithBranch.Contains(employer.Id) ||
                string.Equals(employer.Address?.City?.Name, normalizedCity, StringComparison.OrdinalIgnoreCase));
        }
    }
}

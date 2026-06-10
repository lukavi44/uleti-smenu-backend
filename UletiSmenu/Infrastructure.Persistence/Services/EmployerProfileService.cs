using Core.DTOs;
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

        public async Task<Result<EmployerPublicProfileDTO>> GetEmployerPublicProfileAsync(Guid employerId, Guid? employeeId)
        {
            var employer = await _userRepository.GetByIdAsync<Employer>(employerId);
            if (employer == null)
                return Result.Failure<EmployerPublicProfileDTO>("Employer not found.");
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
                IsFavourite = isFavourite,
                Locations = locations.Select(MapLocation).ToList(),
                ReviewSummary = reviewSummary,
                ActiveJobPosts = jobPosts.Select(MapJobPost).ToList()
            });
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
    }
}

using Core.DTOs;
using Core.Models.Entities;
using Core.Models.ValueObjects;
using Core.Repositories;
using Core.Services;
using Infrastructure.Persistence.Services;
using Moq;
using UletiSmenu.Tests.TestHelpers;

namespace UletiSmenu.Tests.Services
{
    public class EmployerProfileServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IRestaurantLocationRepository> _restaurantLocationRepositoryMock = new();
        private readonly Mock<IReviewRepository> _reviewRepositoryMock = new();
        private readonly Mock<IJobPostRepository> _jobPostRepositoryMock = new();
        private readonly Mock<IApplicationRepository> _applicationRepositoryMock = new();
        private readonly Mock<IApplicationUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IFavouriteRepository> _favouriteRepositoryMock = new();
        private readonly EmployerProfileService _service;

        public EmployerProfileServiceTests()
        {
            _unitOfWorkMock.Setup(unitOfWork => unitOfWork.Favourites).Returns(_favouriteRepositoryMock.Object);
            _unitOfWorkMock.Setup(unitOfWork => unitOfWork.SaveChangesAsync()).Returns(Task.CompletedTask);

            _service = new EmployerProfileService(
                _userRepositoryMock.Object,
                _restaurantLocationRepositoryMock.Object,
                _reviewRepositoryMock.Object,
                _jobPostRepositoryMock.Object,
                _applicationRepositoryMock.Object,
                _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task GetEmployerDirectoryPagedAsync_ShouldPageInRepository_AndBatchLoadCardData()
        {
            var employeeId = Guid.NewGuid();
            var first = CreateEmployer("Alpha Grill", "Beograd", "alpha-grill");
            var second = CreateEmployer("Beta Bistro", "Niš", "beta-bistro");
            var employers = new List<Employer> { first, second };
            var locations = new List<RestaurantLocation>
            {
                CreateLocation(first.Id, "Novi Sad"),
                CreateLocation(second.Id, "Niš")
            };

            _userRepositoryMock
                .Setup(repository => repository.GetEmployerDirectoryPagedAsync("Novi Sad", "grill", 2, 9))
                .ReturnsAsync((employers, 12));
            _restaurantLocationRepositoryMock
                .Setup(repository => repository.GetByEmployerIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(locations);
            _reviewRepositoryMock
                .Setup(repository => repository.GetEmployerReviewSummariesAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(new Dictionary<Guid, ReviewSummaryDTO>
                {
                    [first.Id] = new() { AverageRating = 4.5, ReviewCount = 2 }
                });
            _jobPostRepositoryMock
                .Setup(repository => repository.GetDirectoryActiveJobCountsByEmployerIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<DateTime>()))
                .ReturnsAsync(new Dictionary<Guid, int> { [first.Id] = 3 });
            _favouriteRepositoryMock
                .Setup(repository => repository.GetEmployerIdsFavouritedByEmployeeAsync(employeeId))
                .ReturnsAsync(new List<Guid> { first.Id });

            var result = await _service.GetEmployerDirectoryPagedAsync("Novi Sad", "grill", 2, 9, employeeId);

            Assert.Equal(12, result.TotalCount);
            Assert.Equal(2, result.Page);
            Assert.Equal(9, result.PageSize);
            Assert.Equal(2, result.Items.Count);

            var firstItem = result.Items.Single(item => item.EmployerId == first.Id);
            Assert.Equal("Novi Sad", firstItem.City);
            Assert.Equal(4.5, firstItem.ReviewSummary.AverageRating);
            Assert.Equal(2, firstItem.ReviewSummary.ReviewCount);
            Assert.Equal(3, firstItem.ActiveJobPostsCount);
            Assert.True(firstItem.IsFavourite);

            var secondItem = result.Items.Single(item => item.EmployerId == second.Id);
            Assert.Equal("Niš", secondItem.City);
            Assert.Equal(0, secondItem.ReviewSummary.ReviewCount);
            Assert.Equal(0, secondItem.ActiveJobPostsCount);
            Assert.False(secondItem.IsFavourite);

            _userRepositoryMock.Verify(repository => repository.GetAllEmployersAsync(), Times.Never);
            _userRepositoryMock.Verify(repository => repository.GetEmployerByIdAsync(It.IsAny<Guid>()), Times.Never);
            _restaurantLocationRepositoryMock.Verify(
                repository => repository.GetByEmployerIdAsync(It.IsAny<Guid>()),
                Times.Never);
            _restaurantLocationRepositoryMock.Verify(
                repository => repository.GetEmployerIdsByCityAsync(It.IsAny<string>()),
                Times.Never);
            _reviewRepositoryMock.Verify(
                repository => repository.GetEmployerReviewSummaryAsync(It.IsAny<Guid>()),
                Times.Never);
            _jobPostRepositoryMock.Verify(
                repository => repository.CountActiveByEmployerIdAsync(It.IsAny<Guid>()),
                Times.Never);
            _jobPostRepositoryMock.Verify(
                repository => repository.GetByEmployerIdPagedAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<string?>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>()),
                Times.Never);
            _restaurantLocationRepositoryMock.Verify(
                repository => repository.GetByEmployerIdsAsync(It.IsAny<IEnumerable<Guid>>()),
                Times.Once);
            _reviewRepositoryMock.Verify(
                repository => repository.GetEmployerReviewSummariesAsync(It.IsAny<IEnumerable<Guid>>()),
                Times.Once);
            _jobPostRepositoryMock.Verify(
                repository => repository.GetDirectoryActiveJobCountsByEmployerIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<DateTime>()),
                Times.Once);
        }

        [Fact]
        public async Task GetEmployerDirectoryPagedAsync_ShouldClampPagination_AndSkipFavouritesWhenAnonymous()
        {
            _userRepositoryMock
                .Setup(repository => repository.GetEmployerDirectoryPagedAsync(null, null, 1, 50))
                .ReturnsAsync((new List<Employer>(), 0));
            _restaurantLocationRepositoryMock
                .Setup(repository => repository.GetByEmployerIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(new List<RestaurantLocation>());
            _reviewRepositoryMock
                .Setup(repository => repository.GetEmployerReviewSummariesAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(new Dictionary<Guid, ReviewSummaryDTO>());
            _jobPostRepositoryMock
                .Setup(repository => repository.GetDirectoryActiveJobCountsByEmployerIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<DateTime>()))
                .ReturnsAsync(new Dictionary<Guid, int>());

            var result = await _service.GetEmployerDirectoryPagedAsync(null, null, 0, 100, employeeId: null);

            Assert.Equal(1, result.Page);
            Assert.Equal(50, result.PageSize);
            _userRepositoryMock.Verify(
                repository => repository.GetEmployerDirectoryPagedAsync(null, null, 1, 50),
                Times.Once);
            _favouriteRepositoryMock.Verify(
                repository => repository.GetEmployerIdsFavouritedByEmployeeAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task GetEmployerDirectoryPagedAsync_ShouldEnsureSlugOnlyForPagedEmployersMissingOne()
        {
            var missingSlug = CreateEmployer("Needs Slug", "Beograd", publicSlug: null);
            var hasSlug = CreateEmployer("Has Slug", "Beograd", "has-slug");

            _userRepositoryMock
                .Setup(repository => repository.GetEmployerDirectoryPagedAsync(null, null, 1, 9))
                .ReturnsAsync((new List<Employer> { missingSlug, hasSlug }, 2));
            _userRepositoryMock
                .Setup(repository => repository.PublicSlugExistsAsync("needs-slug", missingSlug.Id))
                .ReturnsAsync(false);
            _restaurantLocationRepositoryMock
                .Setup(repository => repository.GetByEmployerIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(new List<RestaurantLocation>());
            _reviewRepositoryMock
                .Setup(repository => repository.GetEmployerReviewSummariesAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(new Dictionary<Guid, ReviewSummaryDTO>());
            _jobPostRepositoryMock
                .Setup(repository => repository.GetDirectoryActiveJobCountsByEmployerIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<DateTime>()))
                .ReturnsAsync(new Dictionary<Guid, int>());

            var result = await _service.GetEmployerDirectoryPagedAsync(null, null, 1, 9, employeeId: null);

            Assert.Equal("needs-slug", result.Items.Single(item => item.EmployerId == missingSlug.Id).PublicSlug);
            Assert.Equal("has-slug", result.Items.Single(item => item.EmployerId == hasSlug.Id).PublicSlug);
            _userRepositoryMock.Verify(
                repository => repository.PublicSlugExistsAsync("needs-slug", missingSlug.Id),
                Times.Once);
            _userRepositoryMock.Verify(
                repository => repository.PublicSlugExistsAsync(It.IsAny<string>(), hasSlug.Id),
                Times.Never);
            _unitOfWorkMock.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
        }

        private static Employer CreateEmployer(string name, string cityName, string? publicSlug)
        {
            var street = HelperMethods.EnsureSuccess(Street.Create("Ulica", "1"));
            var postalCode = HelperMethods.EnsureSuccess(PostalCode.Create("11000"));
            var country = HelperMethods.EnsureSuccess(Country.Create("Srbija"));
            var region = HelperMethods.EnsureSuccess(Region.Create("Beograd"));
            var city = HelperMethods.EnsureSuccess(City.Create(cityName, postalCode, country, region));
            var address = HelperMethods.EnsureSuccess(Address.Create(street, city));

            var employer = HelperMethods.EnsureSuccess(Employer.Create(
                Guid.NewGuid(),
                name,
                $"{Guid.NewGuid():N}@example.com",
                "employer",
                "060111222",
                null,
                HelperMethods.EnsureSuccess(PIB.Create("123456789")),
                HelperMethods.EnsureSuccess(MB.Create("12345678")),
                null,
                null,
                null,
                address));

            if (!string.IsNullOrWhiteSpace(publicSlug))
                employer.SetPublicSlug(publicSlug);

            return employer;
        }

        private static RestaurantLocation CreateLocation(Guid employerId, string city)
        {
            return HelperMethods.EnsureSuccess(RestaurantLocation.Create(
                Guid.NewGuid(),
                employerId,
                "Lokacija",
                "060111222",
                "123456789",
                "12345678",
                "Ulica",
                "1",
                city,
                "11000",
                "Srbija",
                "Beograd",
                "RS",
                "89010",
                "802824"));
        }
    }
}

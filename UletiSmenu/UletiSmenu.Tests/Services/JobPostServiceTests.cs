using Core.Interfaces;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Core.Services;
using Infrastructure.Persistence.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace UletiSmenu.Tests.Services
{
    public class JobPostServiceTests
    {
        [Fact]
        public async Task CreateJobPostAsync_ShouldFail_WhenLocationBelongsToAnotherEmployer()
        {
            // Arrange
            var jobPostRepositoryMock = new Mock<IJobPostRepository>();
            var locationRepositoryMock = new Mock<IRestaurantLocationRepository>();
            var unitOfWorkMock = new Mock<IApplicationUnitOfWork>();
            var billingServiceMock = new Mock<IBillingService>();
            var emailServiceMock = new Mock<IEmailService>();
            var loggerMock = new Mock<ILogger<JobPostService>>();

            var employerId = Guid.NewGuid();
            var otherEmployerId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            var startsAt = DateTime.UtcNow.AddHours(5);

            var jobPost = JobPost.Create(
                Guid.NewGuid(),
                "Konobar",
                "Smenski rad",
                JobStatusEnum.Active,
                startsAt,
                startsAt.AddMinutes(30),
                employerId,
                locationId,
                5000,
                "Konobar").Value;

            var location = RestaurantLocation.Create(
                locationId,
                otherEmployerId,
                "Branch B",
                "0600000000",
                "123456789",
                "12345678",
                "Street",
                "1",
                "Novi Sad",
                "21000",
                "Serbia",
                "Vojvodina").Value;

            billingServiceMock
                .Setup(x => x.ValidateEmployerCanCreatePostAsync(employerId))
                .ReturnsAsync(CSharpFunctionalExtensions.Result.Success());

            locationRepositoryMock
                .Setup(x => x.GetByIdAsync(locationId))
                .ReturnsAsync(location);

            var sut = new JobPostService(
                jobPostRepositoryMock.Object,
                locationRepositoryMock.Object,
                unitOfWorkMock.Object,
                billingServiceMock.Object,
                emailServiceMock.Object,
                loggerMock.Object);

            // Act
            var result = await sut.CreateJobPostAsync(jobPost);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Selected location does not belong to this brand account.", result.Error);
            jobPostRepositoryMock.Verify(x => x.AddAsync(It.IsAny<JobPost>()), Times.Never);
        }
    }
}

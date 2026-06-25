using Core.Interfaces;
using Core.Models.Entities;
using Core.Repositories;
using Core.Services;
using Infrastructure.Persistence.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using UletiSmenu.Tests.TestHelpers;
using API.DTOs;

namespace UletiSmenu.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IRestaurantLocationRepository> _restaurantLocationRepositoryMock;
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<SignInManager<User>> _signInManagerMock;
        private readonly Mock<IApplicationUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<IBillingService> _billingServiceMock;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
        private readonly Mock<ILogger<UserService>> _loggerMock;

        private readonly UserService _userService;

        public UserServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _restaurantLocationRepositoryMock = new Mock<IRestaurantLocationRepository>();
            _userManagerMock = MockHelper.CreateUserManagerMock();
            _signInManagerMock = MockHelper.CreateSignInManagerMock();
            _unitOfWorkMock = new Mock<IApplicationUnitOfWork>();
            _emailServiceMock = new Mock<IEmailService>();
            _configurationMock = new Mock<IConfiguration>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _billingServiceMock = new Mock<IBillingService>();
            _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            _loggerMock = new Mock<ILogger<UserService>>();

            _billingServiceMock
                .Setup(service => service.GrantRegistrationBonus(It.IsAny<Employer>()))
                .Returns(CSharpFunctionalExtensions.Result.Success());

            _userService = new UserService(
                _userRepositoryMock.Object,
                _restaurantLocationRepositoryMock.Object,
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _unitOfWorkMock.Object,
                _emailServiceMock.Object,
                _configurationMock.Object,
                _httpContextAccessorMock.Object,
                _billingServiceMock.Object,
                _serviceScopeFactoryMock.Object,
                _loggerMock.Object);
        }


        [Fact]
        public async Task GetAllEmployersAsync_ShouldReturnAllEmployers()
        {
            // Arrange
            var fakeEmployer = TestDataFactory.CreateFakeRegisterEmployer();
            var employers = new List<Employer> { fakeEmployer };

            _userRepositoryMock
                .Setup(r => r.GetAllEmployersAsync())
                .ReturnsAsync(employers);

            //Act
            var result = await _userService.GetAllEmployersAsync();

            //Asserts
            Assert.NotNull(result);
            Assert.Contains(result, x => x.Email == "testemployer@example.com");

            _userRepositoryMock.Verify(repo => repo.GetAllEmployersAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateEmployerLocationAsync_ShouldSucceed_WhenPibAndMbDifferFromEmployerAccount()
        {
            // Arrange
            var employerId = Guid.NewGuid();
            var employer = TestDataFactory.CreateFakeRegisterEmployer();

            _userRepositoryMock
                .Setup(repo => repo.GetByIdAsync<Employer>(employerId))
                .ReturnsAsync(employer);

            RestaurantLocation? savedLocation = null;
            _restaurantLocationRepositoryMock
                .Setup(repo => repo.AddAsync(It.IsAny<RestaurantLocation>()))
                .Callback<RestaurantLocation>(location => savedLocation = location)
                .ReturnsAsync(CSharpFunctionalExtensions.Result.Success());

            // Act
            var result = await _userService.CreateEmployerLocationAsync(
                employerId,
                "Branch 2",
                "0609990000",
                "123456789",
                "87654321",
                "Street",
                "1",
                "City",
                "21000",
                "Country",
                "Region");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(savedLocation);
            Assert.Equal("123456789", savedLocation!.PIB);
            Assert.Equal("87654321", savedLocation.MB);
            _restaurantLocationRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<RestaurantLocation>()), Times.Once);
        }

        [Fact]
        public async Task CreateEmployerLocationAsync_ShouldFail_WhenPibIsInvalid()
        {
            // Arrange
            var employerId = Guid.NewGuid();
            var employer = TestDataFactory.CreateFakeRegisterEmployer();

            _userRepositoryMock
                .Setup(repo => repo.GetByIdAsync<Employer>(employerId))
                .ReturnsAsync(employer);

            // Act
            var result = await _userService.CreateEmployerLocationAsync(
                employerId,
                "Branch 2",
                "0609990000",
                "12345",
                employer.MB.Value,
                "Street",
                "1",
                "City",
                "21000",
                "Country",
                "Region");

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("PIB is invalid.", result.Error);
            _restaurantLocationRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<RestaurantLocation>()), Times.Never);
        }
    }
}

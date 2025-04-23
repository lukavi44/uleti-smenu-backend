using Core.Interfaces;
using Core.Models.Entities;
using Core.Repositories;
using Core.Services;
using Infrastructure.Persistence.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using UletiSmenu.Tests.TestHelpers;
using API.DTOs;

namespace UletiSmenu.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<SignInManager<User>> _signInManagerMock;
        private readonly Mock<IApplicationUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<ILogger<UserService>> _loggerMock;

        private readonly UserService _userService;

        public UserServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _userManagerMock = MockHelper.CreateUserManagerMock();
            _signInManagerMock = MockHelper.CreateSignInManagerMock();
            _unitOfWorkMock = new Mock<IApplicationUnitOfWork>();
            _emailServiceMock = new Mock<IEmailService>();
            _configurationMock = new Mock<IConfiguration>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _loggerMock = new Mock<ILogger<UserService>>();

            _userService = new UserService(
                _userRepositoryMock.Object,
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _unitOfWorkMock.Object,
                _emailServiceMock.Object,
                _configurationMock.Object,
                _httpContextAccessorMock.Object,
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
    }
}

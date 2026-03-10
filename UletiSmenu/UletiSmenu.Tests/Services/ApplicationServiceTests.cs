using Core.Models.Entities;
using Core.Models.Enums;
using Core.Repositories;
using Core.Services;
using Infrastructure.Persistence.Services;
using Moq;

namespace UletiSmenu.Tests.Services
{
    public class ApplicationServiceTests
    {
        private readonly Mock<IApplicationRepository> _applicationRepositoryMock;
        private readonly Mock<IJobPostRepository> _jobPostRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IApplicationUnitOfWork> _unitOfWorkMock;
        private readonly ApplicationService _applicationService;

        public ApplicationServiceTests()
        {
            _applicationRepositoryMock = new Mock<IApplicationRepository>();
            _jobPostRepositoryMock = new Mock<IJobPostRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _unitOfWorkMock = new Mock<IApplicationUnitOfWork>();

            _applicationService = new ApplicationService(
                _applicationRepositoryMock.Object,
                _jobPostRepositoryMock.Object,
                _userRepositoryMock.Object,
                _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task ApplyToJobPostAsync_ShouldReturnSuccess_AndPersistApplication()
        {
            // Arrange
            var employeeId = Guid.NewGuid();
            var jobPostId = Guid.NewGuid();
            var employee = CreateEmployee(employeeId);
            var jobPost = CreateJobPost(
                jobPostId,
                status: JobStatusEnum.Active,
                startingDate: DateTime.UtcNow.AddHours(3),
                visibleUntil: DateTime.UtcNow.AddHours(3).AddMinutes(30));

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync<Employee>(employeeId))
                .ReturnsAsync(employee);

            _jobPostRepositoryMock
                .Setup(r => r.GetJobPostByIdAsync(jobPostId))
                .ReturnsAsync(jobPost);

            _applicationRepositoryMock
                .Setup(r => r.HasEmployeeAppliedAsync(employeeId, jobPostId))
                .ReturnsAsync(false);

            _applicationRepositoryMock
                .Setup(r => r.GetApplicantCountByJobPostAsync(jobPostId))
                .ReturnsAsync(2);

            _applicationRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Application>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _applicationService.ApplyToJobPostAsync(employeeId, jobPostId);

            // Assert
            Assert.True(result.IsSuccess);
            _applicationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Application>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task ApplyToJobPostAsync_ShouldFail_WhenEmployeeAlreadyApplied()
        {
            // Arrange
            var employeeId = Guid.NewGuid();
            var jobPostId = Guid.NewGuid();
            var employee = CreateEmployee(employeeId);
            var jobPost = CreateJobPost(
                jobPostId,
                status: JobStatusEnum.Active,
                startingDate: DateTime.UtcNow.AddHours(2),
                visibleUntil: DateTime.UtcNow.AddHours(2).AddMinutes(30));

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync<Employee>(employeeId))
                .ReturnsAsync(employee);

            _jobPostRepositoryMock
                .Setup(r => r.GetJobPostByIdAsync(jobPostId))
                .ReturnsAsync(jobPost);

            _applicationRepositoryMock
                .Setup(r => r.HasEmployeeAppliedAsync(employeeId, jobPostId))
                .ReturnsAsync(true);

            // Act
            var result = await _applicationService.ApplyToJobPostAsync(employeeId, jobPostId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("You have already applied to this job post.", result.Error);
            _applicationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Application>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task ApplyToJobPostAsync_ShouldFail_WhenJobPostIsNotActive()
        {
            // Arrange
            var employeeId = Guid.NewGuid();
            var jobPostId = Guid.NewGuid();
            var employee = CreateEmployee(employeeId);
            var inactiveJobPost = CreateJobPost(
                jobPostId,
                status: JobStatusEnum.Completed,
                startingDate: DateTime.UtcNow.AddHours(4),
                visibleUntil: DateTime.UtcNow.AddHours(4).AddMinutes(30));

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync<Employee>(employeeId))
                .ReturnsAsync(employee);

            _jobPostRepositoryMock
                .Setup(r => r.GetJobPostByIdAsync(jobPostId))
                .ReturnsAsync(inactiveJobPost);

            // Act
            var result = await _applicationService.ApplyToJobPostAsync(employeeId, jobPostId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("You can only apply to active job posts.", result.Error);
            _applicationRepositoryMock.Verify(r => r.HasEmployeeAppliedAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
            _applicationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Application>()), Times.Never);
        }

        private static Employee CreateEmployee(Guid employeeId)
        {
            var result = Employee.Create(
                employeeId,
                "employee@test.com",
                "employee@test.com",
                "0601231234",
                string.Empty,
                new List<Application>(),
                "Test",
                "Employee");

            Assert.True(result.IsSuccess, result.IsFailure ? result.Error : string.Empty);
            return result.Value;
        }

        private static JobPost CreateJobPost(
            Guid jobPostId,
            JobStatusEnum status,
            DateTime startingDate,
            DateTime visibleUntil)
        {
            var result = JobPost.Create(
                jobPostId,
                "Shift title",
                "Shift description",
                status,
                startingDate,
                visibleUntil,
                Guid.NewGuid(),
                Guid.NewGuid(),
                3000,
                "Waiter");

            Assert.True(result.IsSuccess, result.IsFailure ? result.Error : string.Empty);
            return result.Value;
        }
    }
}

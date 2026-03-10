using Core.Models.Entities;
using Core.Models.Enums;

namespace UletiSmenu.Tests.Domain
{
    public class JobPostLifecycleTests
    {
        [Fact]
        public void Create_ShouldFail_WhenVisibleUntilBeforeStartingDate()
        {
            // Arrange
            var startingDate = DateTime.UtcNow.AddHours(2);
            var visibleUntil = startingDate.AddMinutes(-10);

            // Act
            var result = JobPost.Create(
                Guid.NewGuid(),
                "Evening shift",
                "Need one waiter for evening shift.",
                JobStatusEnum.Active,
                startingDate,
                visibleUntil,
                Guid.NewGuid(),
                Guid.NewGuid(),
                3500,
                "Waiter");

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("VisibleUntil cannot be before StartingDate.", result.Error);
        }

        [Fact]
        public void Create_ShouldFail_WhenVisibleUntilAfterOneHourPastStartingDate()
        {
            // Arrange
            var startingDate = DateTime.UtcNow.AddHours(2);
            var visibleUntil = startingDate.AddHours(1).AddMinutes(1);

            // Act
            var result = JobPost.Create(
                Guid.NewGuid(),
                "Morning shift",
                "Need a helper for morning prep.",
                JobStatusEnum.Active,
                startingDate,
                visibleUntil,
                Guid.NewGuid(),
                Guid.NewGuid(),
                3200,
                "Kitchen assistant");

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("VisibleUntil cannot be later than one hour after StartingDate.", result.Error);
        }

        [Fact]
        public void ValidateCanApply_ShouldFail_WhenShiftHasAlreadyStarted()
        {
            // Arrange
            var startingDate = DateTime.UtcNow.AddHours(1);
            var jobPost = CreateValidJobPost(JobStatusEnum.Active, startingDate, startingDate.AddMinutes(30));
            var nowAfterStart = startingDate.AddMinutes(5);

            // Act
            var result = jobPost.ValidateCanApply(nowAfterStart);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Applications are closed because this shift has already started.", result.Error);
        }

        [Fact]
        public void IsVisible_ShouldReturnFalse_WhenUtcNowIsAfterVisibleUntil()
        {
            // Arrange
            var startingDate = DateTime.UtcNow.AddHours(2);
            var visibleUntil = startingDate.AddMinutes(10);
            var jobPost = CreateValidJobPost(JobStatusEnum.Active, startingDate, visibleUntil);
            var nowAfterVisibilityWindow = visibleUntil.AddMinutes(1);

            // Act
            var isVisible = jobPost.IsVisible(nowAfterVisibilityWindow);

            // Assert
            Assert.False(isVisible);
        }

        [Fact]
        public void IsArchived_ShouldReturnTrue_WhenUtcNowIsMoreThanOneHourAfterStart()
        {
            // Arrange
            var startingDate = DateTime.UtcNow.AddHours(2);
            var jobPost = CreateValidJobPost(JobStatusEnum.Active, startingDate, startingDate.AddMinutes(30));
            var archivedCheckTime = startingDate.AddHours(1).AddMinutes(1);

            // Act
            var isArchived = jobPost.IsArchived(archivedCheckTime);

            // Assert
            Assert.True(isArchived);
        }

        [Fact]
        public void IsArchived_ShouldReturnTrue_ForTerminalStatuses()
        {
            // Arrange
            var startingDate = DateTime.UtcNow.AddHours(2);
            var jobPost = CreateValidJobPost(JobStatusEnum.Completed, startingDate, startingDate.AddMinutes(15));

            // Act
            var isArchived = jobPost.IsArchived(DateTime.UtcNow);

            // Assert
            Assert.True(isArchived);
        }

        private static JobPost CreateValidJobPost(
            JobStatusEnum status,
            DateTime startingDate,
            DateTime visibleUntil)
        {
            var result = JobPost.Create(
                Guid.NewGuid(),
                "Shift",
                "Shift description",
                status,
                startingDate,
                visibleUntil,
                Guid.NewGuid(),
                Guid.NewGuid(),
                3000,
                "Server");

            Assert.True(result.IsSuccess, result.IsFailure ? result.Error : string.Empty);
            return result.Value;
        }
    }
}

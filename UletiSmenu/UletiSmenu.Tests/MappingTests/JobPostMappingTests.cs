using API.DTOs;
using API.RequestHelper;
using AutoMapper;
using Core.Models.Entities;
using Core.Models.Enums;
using Xunit;

namespace Tests.Mapping
{
    public class JobPostMappingTests
    {
        private readonly IMapper _mapper;

        public JobPostMappingTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });

            _mapper = config.CreateMapper();
        }

        [Fact]
        public void Should_Map_JobPostCreateDTO_To_JobPost_With_EmployerId_And_VisibleUntil()
        {
            // Arrange
            var startingDate = DateTime.UtcNow.AddHours(3);
            var visibleUntil = startingDate.AddMinutes(45);
            var restaurantLocationId = Guid.NewGuid();
            var dto = new JobPostCreateDTO
            {
                Title = "Bartender",
                Description = "Serve drinks with a smile.",
                Position = "Evening shift",
                Status = "Active",
                Salary = 1500,
                StartingDate = startingDate,
                VisibleUntil = visibleUntil,
                RestaurantLocationId = restaurantLocationId
            };
            var expectedEmployerId = Guid.NewGuid();

            // Act
            var result = _mapper.Map<JobPost>(dto, opt =>
            {
                opt.Items["EmployerId"] = expectedEmployerId;
            });

            // Assert
            Assert.Equal(dto.Title, result.Title);
            Assert.Equal(dto.Description, result.Description);
            Assert.Equal(dto.Position, result.Position);
            Assert.Equal(dto.Salary, result.Salary);
            Assert.Equal(dto.StartingDate, result.StartingDate);
            Assert.Equal(dto.VisibleUntil, result.VisibleUntil);
            Assert.Equal(dto.RestaurantLocationId, result.RestaurantLocationId);
            Assert.Equal(JobStatusEnum.Active, result.Status);
            Assert.Equal(expectedEmployerId, result.EmployerId);
        }
    }
}

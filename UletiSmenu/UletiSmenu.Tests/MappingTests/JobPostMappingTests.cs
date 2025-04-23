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
        public void Should_Map_JobPostDTO_To_JobPost_With_EmployerId()
        {
            // Arrange
            var dto = new JobPostDTO(
                Title: "Bartender",
                Description: "Serve drinks with a smile.",
                Position: "Evening shift",
                Status: "Active",
                Salary: 1500,
                StartingDate: new DateTime(2025, 5, 1)
            );

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
            Assert.Equal(JobStatusEnum.Active, result.Status);
            Assert.Equal(expectedEmployerId, result.EmployerId);
        }
    }
}

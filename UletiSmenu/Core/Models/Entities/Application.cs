using Core.Models.Enums;
using CSharpFunctionalExtensions;
using System.ComponentModel.DataAnnotations;

namespace Core.Models.Entities
{
    public class Application
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public Guid JobPostId { get; private set; }
        public ApplicationStatusEnum Status { get; private set; }
        public int NumberOfApplicants { get; private set; }
        public DateTime DateTime { get; private set; } = DateTime.UtcNow;

        private Application() { }

        private Application(Guid id, Guid userId, Guid jobPostId, ApplicationStatusEnum status, int numberOfApplicants, DateTime dateTime)
        {
            Id = id;
            UserId = userId;
            JobPostId = jobPostId;
            Status = status;
            NumberOfApplicants = numberOfApplicants;
            DateTime = dateTime;
        }

        public static Result<Application> Create(Guid id, Guid userId, Guid jobPostId, ApplicationStatusEnum status, int numberOfApplicants, DateTime dateTime)
        {
            if (id == Guid.Empty)
            {
                return Result.Failure<Application>("ID cannot be empty.");
            }

            if (userId == Guid.Empty)
            {
                return Result.Failure<Application>("User ID cannot be empty.");
            }

            if (jobPostId == Guid.Empty)
            {
                return Result.Failure<Application>("Job post ID cannot be empty.");
            }

            if (status == default)
                return Result.Failure<Application>("Status cannot be empty.");

            if (numberOfApplicants < 0)
                return Result.Failure<Application>("Number of applicants cannot be less than zero.");

            return Result.Success(new Application(id, userId, jobPostId, status, numberOfApplicants, dateTime));
        }
    }
}

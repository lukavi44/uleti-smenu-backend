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

            if (!Enum.IsDefined(typeof(ApplicationStatusEnum), status))
                return Result.Failure<Application>("Status is invalid.");

            if (numberOfApplicants < 0)
                return Result.Failure<Application>("Number of applicants cannot be less than zero.");

            return Result.Success(new Application(id, userId, jobPostId, status, numberOfApplicants, dateTime));
        }

        public Result SetEmployerDecision(ApplicationStatusEnum newStatus)
        {
            if (Status != ApplicationStatusEnum.Applied)
                return Result.Failure("Only applications in Applied status can be accepted or denied.");

            if (newStatus != ApplicationStatusEnum.Accepted && newStatus != ApplicationStatusEnum.Denied)
                return Result.Failure("Employer can set only Accepted or Denied status.");

            Status = newStatus;
            return Result.Success();
        }

        public Result CancelByEmployee()
        {
            if (Status != ApplicationStatusEnum.Applied)
                return Result.Failure("Only applications in Applied status can be cancelled.");

            Status = ApplicationStatusEnum.Cancelled;
            return Result.Success();
        }

        public Result ExpireDueToInactiveJobPost()
        {
            if (Status != ApplicationStatusEnum.Applied)
                return Result.Failure("Only pending applications can expire.");

            Status = ApplicationStatusEnum.Expired;
            return Result.Success();
        }
    }
}

using Core.Models.Enums;
using CSharpFunctionalExtensions;

namespace Core.Models.Entities
{
    public class JobPost
    {
        public Guid Id { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string Position { get; private set; }
        public JobStatusEnum Status { get; private set; }
        public int Salary { get; private set; }
        public DateTime StartingDate { get; private set; }
        public DateTime VisibleUntil { get; private set; }
        public Guid EmployerId { get; private set; }
        public Employer Employer { get; private set; }
        public Guid? RestaurantLocationId { get; private set; }
        public RestaurantLocation? RestaurantLocation { get; private set; }

        private JobPost(
            Guid id,
            string title,
            string description,
            JobStatusEnum status,
            DateTime startingDate,
            DateTime visibleUntil,
            Guid employerId,
            Guid restaurantLocationId,
            int salary,
            string position)
        {
            Id = id;
            Title = title;
            Description = description;
            Status = status;
            StartingDate = startingDate;
            VisibleUntil = visibleUntil;
            EmployerId = employerId;
            RestaurantLocationId = restaurantLocationId;
            Salary = salary;
            Position = position;
        }

        private JobPost() { }

        public static Result<JobPost> Create(
            Guid id,
            string title,
            string description,
            JobStatusEnum status,
            DateTime startingDate,
            DateTime visibleUntil,
            Guid employerId,
            Guid restaurantLocationId,
            int salary,
            string position)
        {
            if (id == Guid.Empty) return Result.Failure<JobPost>("ID cannot be empty.");

            if (string.IsNullOrWhiteSpace(title))
                return Result.Failure<JobPost>("Title name cannot be empty.");

            if (string.IsNullOrWhiteSpace(description))
                return Result.Failure<JobPost>("Description cannot be empty.");

            if (startingDate == default || startingDate <= DateTime.UtcNow)
                return Result.Failure<JobPost>("Starting date must be set and cannot be in the past.");

            if (visibleUntil == default)
                return Result.Failure<JobPost>("VisibleUntil must be set.");

            if (visibleUntil < startingDate)
                return Result.Failure<JobPost>("VisibleUntil cannot be before StartingDate.");

            if (visibleUntil > startingDate.AddHours(1))
                return Result.Failure<JobPost>("VisibleUntil cannot be later than one hour after StartingDate.");

            if (restaurantLocationId == Guid.Empty)
                return Result.Failure<JobPost>("Restaurant location must be selected.");

            if (salary <= 0)
                return Result.Failure<JobPost>("Salary must be greater than zero.");

            if (string.IsNullOrWhiteSpace(position))
                return Result.Failure<JobPost>("Position cannot be empty");

            return Result.Success<JobPost>(new JobPost(id, title, description, status, startingDate, visibleUntil, employerId, restaurantLocationId, salary, position));
        }

        public bool IsArchived(DateTime utcNow)
        {
            if (Status is JobStatusEnum.Cancelled or JobStatusEnum.Completed or JobStatusEnum.Expired)
                return true;

            return utcNow > StartingDate.AddHours(1);
        }

        public bool IsVisible(DateTime utcNow)
        {
            if (IsArchived(utcNow))
                return false;

            return Status == JobStatusEnum.Active && utcNow <= VisibleUntil;
        }

        public Result ValidateCanApply(DateTime utcNow)
        {
            if (Status != JobStatusEnum.Active)
                return Result.Failure("You can only apply to active job posts.");

            if (utcNow >= StartingDate)
                return Result.Failure("You cannot apply after the shift start time has passed.");

            if (!IsVisible(utcNow))
                return Result.Failure("This job post is no longer visible.");

            return Result.Success();
        }
    }
}

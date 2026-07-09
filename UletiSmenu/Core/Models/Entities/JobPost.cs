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
        public DateTime CreatedAtUtc { get; private set; }
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
            DateTime createdAtUtc,
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
            CreatedAtUtc = createdAtUtc;
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
            DateTime? visibleUntil,
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

            var resolvedVisibleUntil = visibleUntil ?? startingDate;

            if (resolvedVisibleUntil < startingDate)
                return Result.Failure<JobPost>("VisibleUntil cannot be before StartingDate.");

            if (resolvedVisibleUntil > startingDate.AddHours(1))
                return Result.Failure<JobPost>("VisibleUntil cannot be later than one hour after StartingDate.");

            if (restaurantLocationId == Guid.Empty)
                return Result.Failure<JobPost>("Restaurant location must be selected.");

            if (salary <= 0)
                return Result.Failure<JobPost>("Salary must be greater than zero.");

            if (string.IsNullOrWhiteSpace(position))
                return Result.Failure<JobPost>("Position cannot be empty");

            return Result.Success<JobPost>(new JobPost(id, title, description, status, DateTime.UtcNow, startingDate, resolvedVisibleUntil, employerId, restaurantLocationId, salary, position));
        }

        public Result Update(
            string title,
            string description,
            JobStatusEnum status,
            DateTime startingDate,
            DateTime? visibleUntil,
            Guid restaurantLocationId,
            int salary,
            string position)
        {
            if (string.IsNullOrWhiteSpace(title))
                return Result.Failure("Title name cannot be empty.");

            if (string.IsNullOrWhiteSpace(description))
                return Result.Failure("Description cannot be empty.");

            if (startingDate == default)
                return Result.Failure("Starting date must be set.");

            var resolvedVisibleUntil = visibleUntil ?? startingDate;
            if (resolvedVisibleUntil < startingDate)
                return Result.Failure("VisibleUntil cannot be before StartingDate.");

            if (resolvedVisibleUntil > startingDate.AddHours(1))
                return Result.Failure("VisibleUntil cannot be later than one hour after StartingDate.");

            if (restaurantLocationId == Guid.Empty)
                return Result.Failure("Restaurant location must be selected.");

            if (salary <= 0)
                return Result.Failure("Salary must be greater than zero.");

            if (string.IsNullOrWhiteSpace(position))
                return Result.Failure("Position cannot be empty");

            Title = title;
            Description = description;
            Status = status;
            StartingDate = startingDate;
            VisibleUntil = resolvedVisibleUntil;
            RestaurantLocationId = restaurantLocationId;
            Salary = salary;
            Position = position;

            return Result.Success();
        }

        public Result Archive()
        {
            if (Status == JobStatusEnum.Cancelled)
                return Result.Success();

            Status = JobStatusEnum.Cancelled;
            return Result.Success();
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
            if (Status == JobStatusEnum.Draft)
                return Result.Failure("You can only apply to active job posts.");

            if (Status != JobStatusEnum.Active)
                return Result.Failure("You can only apply to active job posts.");

            if (utcNow >= StartingDate)
                return Result.Failure("Applications are closed because this shift has already started.");

            return Result.Success();
        }

        public bool AcceptsEmployerApplicationDecisions(DateTime utcNow)
        {
            if (Status != JobStatusEnum.Active)
                return false;

            if (IsArchived(utcNow))
                return false;

            if (utcNow >= StartingDate)
                return false;

            return true;
        }
    }
}

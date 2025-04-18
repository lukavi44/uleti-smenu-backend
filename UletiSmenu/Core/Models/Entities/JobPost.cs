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
        public Guid EmployerId { get; private set; }
        public Employer Employer { get; private set; }

        private JobPost(Guid id, string title, string description, JobStatusEnum status, DateTime startingDate, Guid employerId, int salary, string position)
        {
            Id = id;
            Title = title;
            Description = description;
            Status = status;
            StartingDate = startingDate;
            EmployerId = employerId;
            Salary = salary;
            Position = position;
        }

        private JobPost() { }

        public static Result<JobPost> Create(Guid id, string title, string description, JobStatusEnum status, DateTime startingDate, Guid employerId, int salary, string position)
        {
            if (id == Guid.Empty) return Result.Failure<JobPost>("ID cannot be empty.");

            if (string.IsNullOrWhiteSpace(title))
                return Result.Failure<JobPost>("Title name cannot be empty.");

            if (string.IsNullOrWhiteSpace(description))
                return Result.Failure<JobPost>("Description cannot be empty.");

            if (status == default)
                return Result.Failure<JobPost>("Status cannot be empty.");

            if (startingDate == default || startingDate <= DateTime.UtcNow)
                return Result.Failure<JobPost>("Starting date must be set and cannot be in the past.");

            if (salary <= 0)
                return Result.Failure<JobPost>("Salary must be greater than zero.");

            if (string.IsNullOrWhiteSpace(position))
                return Result.Failure<JobPost>("Position cannot be empty");

            return Result.Success<JobPost>(new JobPost(id, title, description, status, startingDate, employerId, salary, position));
        }
    }
}

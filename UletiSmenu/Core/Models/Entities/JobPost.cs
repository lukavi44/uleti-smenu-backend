using Core.Models.ValueObjects;
using CSharpFunctionalExtensions;

namespace Core.Models.Entities
{
    public class JobPost
    {
        public Guid Id { get; }
        public string Title { get; }
        public string Description { get; }
        public string Position { get; }
        public int Salary { get; }
        public DateTime StartingDate { get; }
        public Address Address { get; }

        private JobPost(Guid id, string title, string description, DateTime startingDate, Address address, int salary, string position)
        {
            Id = id;
            Title = title;
            Description = description;
            StartingDate = startingDate;
            Address = address;
            Salary = salary;
            Position = position;
        }

        public JobPost() { }

        public static Result<JobPost> Create(Guid id, string title, string description, DateTime startingDate, Address address, int salary, string position)
        {
            if (id == Guid.Empty) return Result.Failure<JobPost>("ID cannot be empty.");

            if (string.IsNullOrWhiteSpace(title))
                return Result.Failure<JobPost>("Last name cannot be empty.");

            if (string.IsNullOrWhiteSpace(description))
                return Result.Failure<JobPost>("Email cannot be empty.");

            if (startingDate == default || startingDate < DateTime.Now)
                return Result.Failure<JobPost>("Starting datemust be set and cannot be in past.");

            if (salary == default)
                return Result.Failure<JobPost>("Salary cannot be empty.");

            if (!string.IsNullOrWhiteSpace(position))
                return Result.Failure<JobPost>("Position cannot be empty");

            return Result.Success<JobPost>(new JobPost(id, title, description, startingDate, address, salary, position));
        }
    }
}

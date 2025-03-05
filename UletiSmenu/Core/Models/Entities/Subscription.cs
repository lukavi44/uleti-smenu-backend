using CSharpFunctionalExtensions;

namespace Core.Models.Entities
{
    public class Subscription
    {
        public Guid Id { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public decimal Cost { get; private set; }
        public int DurationInDays { get; private set; }
        public int NumberOfPosts { get; private set; }

        private Subscription() { }

        private Subscription(Guid id, string title, string description, decimal cost, int durationInDays, int numberOfPosts)
        {
            Id = id;
            Title = title;
            Description = description;
            Cost = cost;
            DurationInDays = durationInDays;
            NumberOfPosts = numberOfPosts;
        }

        public static Result<Subscription> Create(Guid id, string title, string description, decimal cost, int durationInDays, int numberOfPosts)
        {
            if (id == Guid.Empty) return Result.Failure<Subscription>("ID cannot be empty.");

            if (string.IsNullOrWhiteSpace(title)) return Result.Failure<Subscription>("Title cannot be empty.");

            if (string.IsNullOrWhiteSpace(description)) return Result.Failure<Subscription>("Description cannot be empty.");

            if (cost <= 0) return Result.Failure<Subscription>("Cost must be greater than zero.");

            if (durationInDays <= 0) return Result.Failure<Subscription>("Duration in days must be greater than zero.");

            if (numberOfPosts < 0) return Result.Failure<Subscription>("Number of posts cannot be negative.");

            return Result.Success<Subscription>(new Subscription(id, title, description, cost, durationInDays, numberOfPosts));
        }
    }
}

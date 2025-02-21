using CSharpFunctionalExtensions;

namespace Core.Models.Entities
{
    public class Subscription
    {
        public Guid Id { get; }
        public string Title { get; }
        public string Description { get; }
        public decimal Cost { get; }
        public int DurationInDays { get; }
        public int NumberOfPosts { get; }

        private Subscription(Guid id, string title, string description, decimal cost, int durationInDays, int numberOfPosts)
        {
            Id = id;
            Title = title;
            Description = description;
            Cost = cost;
            DurationInDays = durationInDays;
            NumberOfPosts = numberOfPosts;
        }

        public Subscription() { }

        public static Result<Subscription> Create(Guid id, string title, string description, decimal cost, int durationInDays, int numberOfPosts)
        {
            if (id == Guid.Empty) return Result.Failure<Subscription>("ID cannot be empty.");

            if (string.IsNullOrWhiteSpace(title)) return Result.Failure<Subscription>("Title cannot be empty.");

            if (string.IsNullOrWhiteSpace(description)) return Result.Failure<Subscription>("Description cannot be empty.");

            if (cost == default) return Result.Failure<Subscription>("Cost cannot be empty.");

            if (durationInDays == default) return Result.Failure<Subscription>("Duration in days cannot be empty.");

            if (numberOfPosts == default) return Result.Failure<Subscription>("Number of posts cannot be empty.");

            return Result.Success<Subscription>(new Subscription(id, title, description, cost, durationInDays, numberOfPosts));
        }
    }
}

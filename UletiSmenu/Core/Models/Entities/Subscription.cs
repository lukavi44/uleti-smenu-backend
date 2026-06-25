using Core.Models.Enums;
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
        public PlanKind PlanKind { get; private set; }

        private Subscription() { }

        private Subscription(Guid id, string title, string description, decimal cost, int durationInDays, int numberOfPosts, PlanKind planKind)
        {
            Id = id;
            Title = title;
            Description = description;
            Cost = cost;
            DurationInDays = durationInDays;
            NumberOfPosts = numberOfPosts;
            PlanKind = planKind;
        }

        public static Result<Subscription> Create(
            Guid id,
            string title,
            string description,
            decimal cost,
            int durationInDays,
            int numberOfPosts,
            PlanKind planKind)
        {
            if (id == Guid.Empty) return Result.Failure<Subscription>("ID cannot be empty.");

            if (string.IsNullOrWhiteSpace(title)) return Result.Failure<Subscription>("Title cannot be empty.");

            if (string.IsNullOrWhiteSpace(description)) return Result.Failure<Subscription>("Description cannot be empty.");

            if (cost < 0) return Result.Failure<Subscription>("Cost cannot be negative.");

            if (durationInDays < 0) return Result.Failure<Subscription>("Duration in days cannot be negative.");

            if (numberOfPosts < 0) return Result.Failure<Subscription>("Number of posts cannot be negative.");

            return Result.Success(new Subscription(id, title, description, cost, durationInDays, numberOfPosts, planKind));
        }

        public void UpdatePlan(
            string title,
            string description,
            decimal cost,
            int durationInDays,
            int numberOfPosts,
            PlanKind planKind)
        {
            Title = title;
            Description = description;
            Cost = cost;
            DurationInDays = durationInDays;
            NumberOfPosts = numberOfPosts;
            PlanKind = planKind;
        }
    }
}

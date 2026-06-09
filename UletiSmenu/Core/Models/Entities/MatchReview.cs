using CSharpFunctionalExtensions;

namespace Core.Models.Entities
{
    public class MatchReview
    {
        public const int MinRating = 1;
        public const int MaxRating = 5;
        public const int MaxCommentLength = 1000;

        public Guid Id { get; private set; }
        public Guid ApplicationId { get; private set; }
        public Guid ReviewerId { get; private set; }
        public Guid RevieweeId { get; private set; }
        public int Rating { get; private set; }
        public string? Comment { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }

        private MatchReview() { }

        public static Result<MatchReview> Create(
            Guid applicationId,
            Guid reviewerId,
            Guid revieweeId,
            int rating,
            string? comment,
            DateTime createdAtUtc)
        {
            if (applicationId == Guid.Empty)
                return Result.Failure<MatchReview>("Application ID cannot be empty.");

            if (reviewerId == Guid.Empty || revieweeId == Guid.Empty)
                return Result.Failure<MatchReview>("Reviewer and reviewee are required.");

            if (reviewerId == revieweeId)
                return Result.Failure<MatchReview>("You cannot review yourself.");

            if (rating < MinRating || rating > MaxRating)
                return Result.Failure<MatchReview>($"Rating must be between {MinRating} and {MaxRating}.");

            var normalizedComment = comment?.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedComment) && normalizedComment.Length > MaxCommentLength)
                return Result.Failure<MatchReview>($"Comment cannot exceed {MaxCommentLength} characters.");

            return Result.Success(new MatchReview
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                ReviewerId = reviewerId,
                RevieweeId = revieweeId,
                Rating = rating,
                Comment = string.IsNullOrWhiteSpace(normalizedComment) ? null : normalizedComment,
                CreatedAtUtc = createdAtUtc
            });
        }
    }
}

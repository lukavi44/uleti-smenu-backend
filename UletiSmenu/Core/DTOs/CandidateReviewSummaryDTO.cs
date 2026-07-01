namespace Core.DTOs
{
    public class CandidateReviewSummaryDTO
    {
        public string CandidateName { get; set; } = string.Empty;
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int RecommendationsCount { get; set; }
        public DateTime? LastReviewAtUtc { get; set; }
    }
}

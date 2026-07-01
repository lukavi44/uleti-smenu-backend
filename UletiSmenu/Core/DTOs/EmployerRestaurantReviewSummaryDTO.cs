namespace Core.DTOs
{
    public class EmployerRestaurantReviewSummaryDTO
    {
        public string RestaurantName { get; set; } = string.Empty;
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int RecommendationsCount { get; set; }
        public DateTime? LastReviewAtUtc { get; set; }
    }
}

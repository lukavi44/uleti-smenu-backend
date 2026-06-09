namespace Core.DTOs
{
    public class ReviewDTO
    {
        public Guid Id { get; set; }
        public Guid ApplicationId { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string JobPostTitle { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }
}

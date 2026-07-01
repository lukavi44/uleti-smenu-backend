namespace Core.DTOs
{
    public class CandidateReviewItemDTO
    {
        public Guid Id { get; set; }
        public Guid ApplicationId { get; set; }
        public Guid ReviewerId { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public string? ReviewerProfilePhoto { get; set; }
        public bool ReviewerIsVerified { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string JobPostTitle { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public bool Recommends { get; set; }
    }
}

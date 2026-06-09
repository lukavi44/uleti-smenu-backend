namespace Core.DTOs
{
    public class PendingReviewDTO
    {
        public Guid ApplicationId { get; set; }
        public Guid JobPostId { get; set; }
        public string JobPostTitle { get; set; } = string.Empty;
        public Guid RevieweeId { get; set; }
        public string RevieweeName { get; set; } = string.Empty;
        public DateTime ShiftDate { get; set; }
    }
}

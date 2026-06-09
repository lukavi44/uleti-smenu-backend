namespace API.DTOs
{
    public class SubmitReviewDTO
    {
        public Guid ApplicationId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}

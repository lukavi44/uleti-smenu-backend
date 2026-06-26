namespace Core.DTOs
{
    public class RecentApplicantPreviewDTO
    {
        public Guid UserId { get; set; }
        public string? ProfilePhoto { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}

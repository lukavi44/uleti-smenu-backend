namespace Core.DTOs
{
    public class UserNotificationDTO
    {
        public Guid Id { get; set; }
        public Guid EmployerId { get; set; }
        public Guid JobPostId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}

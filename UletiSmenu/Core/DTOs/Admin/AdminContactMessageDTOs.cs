namespace Core.DTOs.Admin
{
    public class AdminContactMessageListItemDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool EmailSent { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public class AdminContactMessageDetailDTO : AdminContactMessageListItemDTO
    {
        public string Message { get; set; } = string.Empty;
        public DateTime? ResolvedAtUtc { get; set; }
        public Guid? ResolvedByAdminId { get; set; }
        public string? AdminNotes { get; set; }
    }
}

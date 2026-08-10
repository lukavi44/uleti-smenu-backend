namespace Core.DTOs.Admin
{
    public class AdminReportListItemDTO
    {
        public Guid Id { get; set; }
        public Guid ReporterUserId { get; set; }
        public string ReporterEmail { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public Guid TargetId { get; set; }
        public string TargetLabel { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }

    public class AdminReportDetailDTO : AdminReportListItemDTO
    {
        public string? Details { get; set; }
        public DateTime? ResolvedAtUtc { get; set; }
        public Guid? ResolvedByAdminId { get; set; }
        public string? AdminNotes { get; set; }
    }
}

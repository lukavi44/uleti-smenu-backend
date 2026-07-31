namespace Core.DTOs.Admin
{
    public class AdminJobPostDetailDTO
    {
        public Guid Id { get; set; }
        public Guid EmployerId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string EmployerName { get; set; } = string.Empty;
        public string? LocationName { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Salary { get; set; }
        public int ApplicationsCount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime StartingDate { get; set; }
        public DateTime VisibleUntil { get; set; }
        public bool CanArchive { get; set; }
        public List<AdminApplicationListItemDTO> Applications { get; set; } = new();
    }
}

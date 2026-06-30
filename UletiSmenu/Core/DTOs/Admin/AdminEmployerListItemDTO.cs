namespace Core.DTOs.Admin
{
    public class AdminEmployerListItemDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PIB { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public bool IsVerifiedEmployer { get; set; }
        public DateTime? CreatedAtUtc { get; set; }
        public string? ProfilePhoto { get; set; }
    }

    public class AdminEmployerListResponseDTO
    {
        public List<AdminEmployerListItemDTO> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}

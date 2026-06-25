namespace Core.DTOs
{
    public class EmployerDirectoryListItemDTO
    {
        public Guid EmployerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ProfilePhoto { get; set; }
        public string PublicSlug { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public ReviewSummaryDTO ReviewSummary { get; set; } = new();
        public int ActiveJobPostsCount { get; set; }
        public bool IsFavourite { get; set; }
    }
}

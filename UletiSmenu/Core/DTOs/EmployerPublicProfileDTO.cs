namespace Core.DTOs
{
    public class EmployerPublicProfileDTO
    {
        public Guid EmployerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ProfilePhoto { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public bool? IsFavourite { get; set; }
        public string PublicSlug { get; set; } = string.Empty;
        public List<EmployerLocationDTO> Locations { get; set; } = new();
        public ReviewSummaryDTO ReviewSummary { get; set; } = new();
        public List<EmployerJobPostSummaryDTO> ActiveJobPosts { get; set; } = new();
    }
}

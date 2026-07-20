namespace API.DTOs
{
    public class EmployerPublicSummaryDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProfilePhoto { get; set; } = string.Empty;
        public string PublicSlug { get; set; } = string.Empty;
        public bool IsVerifiedEmployer { get; set; }
    }
}

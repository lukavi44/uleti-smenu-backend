namespace Core.DTOs
{
    public class EmployeeApplicationDTO
    {
        public Guid ApplicationId { get; set; }
        public Guid JobPostId { get; set; }
        public string JobPostTitle { get; set; } = string.Empty;
        public string EmployerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
    }
}

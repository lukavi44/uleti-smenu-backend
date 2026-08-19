namespace Core.DTOs
{
    public class EmployerDashboardPendingApplicantDTO : ApplicationApplicantDTO
    {
        public Guid JobPostId { get; set; }
        public string JobPostTitle { get; set; } = string.Empty;
        public string JobPostLocation { get; set; } = string.Empty;
    }
}

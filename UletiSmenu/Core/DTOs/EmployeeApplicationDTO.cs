namespace Core.DTOs
{
    public class EmployeeApplicationDTO
    {
        public Guid ApplicationId { get; set; }
        public Guid JobPostId { get; set; }
        public string JobPostTitle { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string EmployerName { get; set; } = string.Empty;
        public string? RestaurantLocationName { get; set; }
        public string? RestaurantLocationCity { get; set; }
        public DateTime StartingDate { get; set; }
        public int Salary { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
    }
}

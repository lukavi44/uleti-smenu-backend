using Core.DTOs;

namespace API.DTOs
{
    public class JobPostDTO
    {
        public Guid Id { get; set; }
        public Guid EmployerId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Position { get; set; }
        public string Status { get; set; }
        public int Salary { get; set; }
        public DateTime StartingDate { get; set; }
        public DateTime VisibleUntil { get; set; }
        public Guid? RestaurantLocationId { get; set; }
        public string? RestaurantLocationName { get; set; }
        public string? RestaurantLocationCity { get; set; }
        public bool IsArchived { get; set; }
        public int ApplicantCount { get; set; }
        public List<RecentApplicantPreviewDTO> RecentApplicants { get; set; } = new();
        public EmployerDTO Employer { get; set; }
    }
}
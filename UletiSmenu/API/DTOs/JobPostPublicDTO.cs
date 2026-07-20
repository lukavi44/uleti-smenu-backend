namespace API.DTOs
{
    public class JobPostPublicDTO
    {
        public Guid Id { get; set; }
        public Guid EmployerId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Salary { get; set; }
        public DateTime StartingDate { get; set; }
        public DateTime VisibleUntil { get; set; }
        public Guid? RestaurantLocationId { get; set; }
        public string? RestaurantLocationName { get; set; }
        public string? RestaurantLocationCity { get; set; }
        public bool IsArchived { get; set; }
        public EmployerPublicSummaryDTO? Employer { get; set; }
    }
}

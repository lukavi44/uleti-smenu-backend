namespace Core.DTOs
{
    public class EmployerJobPostSummaryDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public DateTime StartingDate { get; set; }
        public string? RestaurantLocationName { get; set; }
        public string? RestaurantLocationCity { get; set; }
    }
}

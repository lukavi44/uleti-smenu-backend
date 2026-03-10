namespace API.DTOs
{
    public class JobPostCreateDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Position { get; set; }
        public string Status { get; set; }
        public int Salary { get; set; }
        public DateTime StartingDate { get; set; }
        public DateTime? VisibleUntil { get; set; }
        public Guid RestaurantLocationId { get; set; }
    }
}

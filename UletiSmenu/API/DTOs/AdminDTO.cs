namespace API.DTOs
{
    public class AdminDTO
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? ProfilePhoto { get; set; }
        public string Role { get; set; } = "Admin";
    }
}

namespace API.DTOs
{
    public class RestaurantLocationDTO
    {
        public Guid Id { get; set; }
        public Guid EmployerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string StreetName { get; set; } = string.Empty;
        public string StreetNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
    }
}

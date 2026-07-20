namespace API.DTOs
{
    public class EmployerListItemDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProfilePhoto { get; set; } = string.Empty;
        public string PublicSlug { get; set; } = string.Empty;
        public AddressDTO? Address { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string RegionCode { get; set; } = string.Empty;
        public string CityCode { get; set; } = string.Empty;
        public bool IsVerifiedEmployer { get; set; }
    }
}

using Core.DTOs;

namespace API.DTOs
{
    public class EmployerDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ProfilePhoto { get; set; } = string.Empty;
        public string PublicSlug { get; set; } = string.Empty;
        public string PIB { get; set; } = string.Empty;
        public string MB { get; set; } = string.Empty;
        public AddressDTO? Address { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string RegionCode { get; set; } = string.Empty;
        public string CityCode { get; set; } = string.Empty;
        public bool IsFavourite { get; set; }
        public bool IsVerifiedEmployer { get; set; }
        public EmployerSubscriptionDTO? Subscription { get; set; }
    }
}

//public class EmployerDTO
//{
//    public string Name { get; set; }
//    public string Email { get; set; }
//    public string Phone { get; set; }
//    public string ProfilePhoto { get; set; }
//    public string PIB { get; set; }
//    public string MB { get; set; }
//    public string StreetName { get; set; }
//    public string StreetNumber { get; set; }
//    public string City { get; set; }
//    public string PostalCode { get; set; }
//    public string Country { get; set; }
//    public string Region { get; set; }
//}

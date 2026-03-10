namespace API.DTOs
{
    public class EmployerDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfilePhoto { get; set; }
        public bool IsFavourite { get; set; }
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

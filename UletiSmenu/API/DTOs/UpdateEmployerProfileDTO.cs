namespace API.DTOs
{
    public record UpdateEmployerProfileDTO(
        string Name,
        string PhoneNumber,
        string StreetName,
        string StreetNumber,
        string City,
        string PostalCode,
        string Country,
        string Region);
}

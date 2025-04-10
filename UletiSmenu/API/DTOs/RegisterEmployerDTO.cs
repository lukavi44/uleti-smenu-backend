namespace API.DTOs
{
    public record RegisterEmployerDTO(
        string CompanyName,
        string Email,
        string PhoneNumber,
        string Password,
        string PIB,
        string MB,
        string StreetName,
        string StreetNumber,
        string City,
        string PostalCode,
        string Country,
        string Region);
}

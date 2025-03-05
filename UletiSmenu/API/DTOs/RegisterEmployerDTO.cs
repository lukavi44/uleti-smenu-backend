namespace API.DTOs
{
    public record RegisterEmployerDTO(
        string Name,
        string Email,
        string Username,
        string PhoneNumber,
        string Password,
        string CompanyName,
        string PIB,
        string MB,
        string StreetName,
        string StreetNumber,
        string City,
        string PostalCode,
        string Country,
        string Region);
}

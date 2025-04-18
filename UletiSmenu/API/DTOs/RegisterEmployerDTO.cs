namespace API.DTOs
{
    public record RegisterEmployerDTO(
        string Name,
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

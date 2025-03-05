namespace API.DTOs
{
    public record RegisterEmployerDTO(
        string Name,
        string Email,
        string Username,
        string PhoneNumber,
        string Password,
        string ProfilePhoto,
        string CompanyName,
        string PIB,
        string MB,
        string StreetName,
        string SteetNumber,
        string City,
        string PostalCode,
        string Country,
        string Region);
}

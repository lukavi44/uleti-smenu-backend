namespace API.DTOs
{
    public record UpdateEmployerProfileDTO(
        string Name,
        string PhoneNumber,
        string PIB,
        string MB,
        string StreetName,
        string StreetNumber,
        string PostalCode,
        string CountryCode,
        string RegionCode,
        string CityCode);
}

namespace Core.DTOs
{
    public record GeographyCountryDTO(string Code, string Name, string NativeName);

    public record GeographyRegionDTO(
        string Code,
        string CountryCode,
        string Name,
        string NativeName);

    public record GeographyCityDTO(
        string Code,
        string RegionCode,
        string Name,
        string NativeName);

    public record GeographySelectionDTO(
        string CountryCode,
        string CountryName,
        string RegionCode,
        string RegionName,
        string CityCode,
        string CityName);
}

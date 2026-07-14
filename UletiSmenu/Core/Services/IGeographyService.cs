using Core.DTOs;
using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface IGeographyService
    {
        Task<IReadOnlyList<GeographyCountryDTO>> GetCountriesAsync();
        Task<IReadOnlyList<GeographyRegionDTO>> GetRegionsAsync(string countryCode);
        Task<IReadOnlyList<GeographyCityDTO>> GetCitiesAsync(string countryCode, string regionCode);
        Task<Result<GeographySelectionDTO>> ValidateSelectionAsync(
            string countryCode,
            string regionCode,
            string cityCode);
    }
}

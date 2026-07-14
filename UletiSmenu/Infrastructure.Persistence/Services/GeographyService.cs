using Core.DTOs;
using Core.Services;
using CSharpFunctionalExtensions;
using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Services
{
    public class GeographyService : IGeographyService
    {
        private readonly ApplicationDbContext _context;

        public GeographyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<GeographyCountryDTO>> GetCountriesAsync() =>
            await _context.GeographyCountries
                .AsNoTracking()
                .Where(country => country.IsActive)
                .OrderBy(country => country.Name)
                .Select(country => new GeographyCountryDTO(
                    country.Code,
                    country.Name,
                    country.NativeName))
                .ToListAsync();

        public async Task<IReadOnlyList<GeographyRegionDTO>> GetRegionsAsync(string countryCode)
        {
            var normalizedCountryCode = NormalizeCountryCode(countryCode);
            return await _context.GeographyRegions
                .AsNoTracking()
                .Where(region =>
                    region.IsActive &&
                    region.CountryCode == normalizedCountryCode)
                .OrderBy(region => region.Name)
                .Select(region => new GeographyRegionDTO(
                    region.Code,
                    region.CountryCode,
                    region.Name,
                    region.NativeName))
                .ToListAsync();
        }

        public async Task<IReadOnlyList<GeographyCityDTO>> GetCitiesAsync(
            string countryCode,
            string regionCode)
        {
            var normalizedCountryCode = NormalizeCountryCode(countryCode);
            var normalizedRegionCode = NormalizeCode(regionCode);

            return await _context.GeographyCities
                .AsNoTracking()
                .Where(city =>
                    city.IsActive &&
                    city.Region.IsActive &&
                    city.Region.Country.IsActive &&
                    city.RegionCode == normalizedRegionCode &&
                    city.Region.CountryCode == normalizedCountryCode)
                .OrderBy(city => city.Name)
                .Select(city => new GeographyCityDTO(
                    city.Code,
                    city.RegionCode,
                    city.Name,
                    city.NativeName))
                .ToListAsync();
        }

        public async Task<Result<GeographySelectionDTO>> ValidateSelectionAsync(
            string countryCode,
            string regionCode,
            string cityCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
                return Result.Failure<GeographySelectionDTO>("Country is required.");

            if (string.IsNullOrWhiteSpace(regionCode))
                return Result.Failure<GeographySelectionDTO>("Region is required.");

            if (string.IsNullOrWhiteSpace(cityCode))
                return Result.Failure<GeographySelectionDTO>("City is required.");

            var normalizedCountryCode = NormalizeCountryCode(countryCode);
            var normalizedRegionCode = NormalizeCode(regionCode);
            var normalizedCityCode = NormalizeCode(cityCode);

            var selection = await _context.GeographyCities
                .AsNoTracking()
                .Where(city =>
                    city.IsActive &&
                    city.Region.IsActive &&
                    city.Region.Country.IsActive &&
                    city.Code == normalizedCityCode)
                .Select(city => new GeographySelectionDTO(
                    city.Region.Country.Code,
                    city.Region.Country.Name,
                    city.Region.Code,
                    city.Region.Name,
                    city.Code,
                    city.Name))
                .SingleOrDefaultAsync();

            if (selection == null)
                return Result.Failure<GeographySelectionDTO>("Selected city does not exist.");

            if (selection.CountryCode != normalizedCountryCode)
                return Result.Failure<GeographySelectionDTO>(
                    "Selected region does not belong to the selected country.");

            if (selection.RegionCode != normalizedRegionCode)
                return Result.Failure<GeographySelectionDTO>(
                    "Selected city does not belong to the selected region.");

            return Result.Success(selection);
        }

        private static string NormalizeCountryCode(string value) =>
            value.Trim().ToUpperInvariant();

        private static string NormalizeCode(string value) => value.Trim();
    }
}

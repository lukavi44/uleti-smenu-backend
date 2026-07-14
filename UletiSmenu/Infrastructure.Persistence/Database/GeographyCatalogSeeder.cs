using System.Reflection;
using System.Text.Json;
using Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Database
{
    public static class GeographyCatalogSeeder
    {
        private const string ResourceName =
            "Infrastructure.Persistence.Data.serbia-geography.json";

        public static async Task SeedAsync(
            ApplicationDbContext context,
            CancellationToken cancellationToken = default)
        {
            var catalog = await LoadCatalogAsync(cancellationToken);

            var countries = await context.GeographyCountries
                .ToDictionaryAsync(country => country.Code, cancellationToken);

            if (countries.TryGetValue(catalog.Country.Code, out var country))
            {
                country.UpdateNames(catalog.Country.Name, catalog.Country.NativeName);
            }
            else
            {
                context.GeographyCountries.Add(new GeographyCountry(
                    catalog.Country.Code,
                    catalog.Country.Name,
                    catalog.Country.NativeName));
            }

            var regions = await context.GeographyRegions
                .ToDictionaryAsync(region => region.Code, cancellationToken);
            var regionCodes = catalog.Regions
                .Select(region => region.Code)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var sourceRegion in catalog.Regions)
            {
                if (regions.TryGetValue(sourceRegion.Code, out var region))
                {
                    region.Update(
                        catalog.Country.Code,
                        sourceRegion.Name,
                        sourceRegion.NativeName);
                }
                else
                {
                    context.GeographyRegions.Add(new GeographyRegion(
                        sourceRegion.Code,
                        catalog.Country.Code,
                        sourceRegion.Name,
                        sourceRegion.NativeName));
                }
            }

            foreach (var existingRegion in regions.Values)
            {
                if (existingRegion.CountryCode == catalog.Country.Code &&
                    !regionCodes.Contains(existingRegion.Code))
                {
                    existingRegion.Deactivate();
                }
            }

            var cities = await context.GeographyCities
                .Include(city => city.Region)
                .ToDictionaryAsync(city => city.Code, cancellationToken);
            var cityCodes = catalog.Cities
                .Select(city => city.Code)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var sourceCity in catalog.Cities)
            {
                if (cities.TryGetValue(sourceCity.Code, out var city))
                {
                    city.Update(
                        sourceCity.RegionCode,
                        sourceCity.Name,
                        sourceCity.NativeName);
                }
                else
                {
                    context.GeographyCities.Add(new GeographyCity(
                        sourceCity.Code,
                        sourceCity.RegionCode,
                        sourceCity.Name,
                        sourceCity.NativeName));
                }
            }

            foreach (var existingCity in cities.Values)
            {
                if (existingCity.Region.CountryCode == catalog.Country.Code &&
                    !cityCodes.Contains(existingCity.Code))
                {
                    existingCity.Deactivate();
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            await BackfillLegacyAddressesAsync(context, catalog, cancellationToken);
        }

        private static async Task BackfillLegacyAddressesAsync(
            ApplicationDbContext context,
            GeographyCatalog catalog,
            CancellationToken cancellationToken)
        {
            var regionsByCode = catalog.Regions.ToDictionary(region => region.Code);
            var citiesByName = catalog.Cities
                .GroupBy(city => NormalizeName(city.Name))
                .ToDictionary(group => group.Key, group => group.ToList());

            var employers = await context.Set<Employer>()
                .Where(employer => employer.GeographyCityCode == null)
                .ToListAsync(cancellationToken);

            foreach (var employer in employers)
            {
                var match = MatchLegacyCity(
                    employer.Address?.City?.Name,
                    employer.Address?.City?.Region?.Name,
                    citiesByName,
                    regionsByCode);
                if (match != null)
                {
                    employer.SetGeographyCodes(
                        catalog.Country.Code,
                        match.RegionCode,
                        match.Code);
                }
            }

            var locations = await context.RestaurantLocations
                .Where(location => location.GeographyCityCode == null)
                .ToListAsync(cancellationToken);

            foreach (var location in locations)
            {
                var match = MatchLegacyCity(
                    location.City,
                    location.Region,
                    citiesByName,
                    regionsByCode);
                if (match != null)
                {
                    location.SetGeographyCodes(
                        catalog.Country.Code,
                        match.RegionCode,
                        match.Code);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        private static GeographyCityData? MatchLegacyCity(
            string? cityName,
            string? regionName,
            IReadOnlyDictionary<string, List<GeographyCityData>> citiesByName,
            IReadOnlyDictionary<string, GeographyRegionData> regionsByCode)
        {
            if (string.IsNullOrWhiteSpace(cityName) ||
                !citiesByName.TryGetValue(NormalizeName(cityName), out var candidates))
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(regionName))
            {
                var normalizedRegionName = NormalizeName(regionName);
                var exactRegionMatches = candidates
                    .Where(city =>
                        regionsByCode.TryGetValue(city.RegionCode, out var region) &&
                        (NormalizeName(region.Name) == normalizedRegionName ||
                         NormalizeName(region.NativeName) == normalizedRegionName))
                    .ToList();

                if (exactRegionMatches.Count == 1)
                    return exactRegionMatches[0];
            }

            return candidates.Count == 1 ? candidates[0] : null;
        }

        private static string NormalizeName(string value) =>
            string.Join(
                    ' ',
                    value.Trim().Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToUpperInvariant();

        private static async Task<GeographyCatalog> LoadCatalogAsync(
            CancellationToken cancellationToken)
        {
            await using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException(
                    $"Embedded geography catalog '{ResourceName}' was not found.");

            return await JsonSerializer.DeserializeAsync<GeographyCatalog>(
                       stream,
                       new JsonSerializerOptions
                       {
                           PropertyNameCaseInsensitive = true
                       },
                       cancellationToken)
                   ?? throw new InvalidOperationException(
                       "The embedded geography catalog is empty or invalid.");
        }

        private sealed class GeographyCatalog
        {
            public GeographyCountryData Country { get; set; } = new();
            public List<GeographyRegionData> Regions { get; set; } = [];
            public List<GeographyCityData> Cities { get; set; } = [];
        }

        private sealed class GeographyCountryData
        {
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string NativeName { get; set; } = string.Empty;
        }

        private sealed class GeographyRegionData
        {
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string NativeName { get; set; } = string.Empty;
        }

        private sealed class GeographyCityData
        {
            public string Code { get; set; } = string.Empty;
            public string RegionCode { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string NativeName { get; set; } = string.Empty;
        }
    }
}

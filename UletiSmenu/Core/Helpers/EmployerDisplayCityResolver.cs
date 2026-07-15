using Core.Models.Entities;

namespace Core.Helpers
{
    public static class EmployerDisplayCityResolver
    {
        /// <summary>
        /// Resolves the city shown on restaurant cards/profile headers.
        /// Priority: main location → employer profile → first branch with a city.
        /// Empty cities never override a later valid source.
        /// </summary>
        public static string Resolve(Employer employer, IReadOnlyList<RestaurantLocation> locations)
        {
            var orderedLocations = locations
                .OrderBy(location => location.Id)
                .ToList();

            var mainCity = ResolveLocationCity(orderedLocations.FirstOrDefault());
            if (!string.IsNullOrWhiteSpace(mainCity))
                return mainCity;

            var employerCity = ResolveEmployerProfileCity(employer);
            if (!string.IsNullOrWhiteSpace(employerCity))
                return employerCity;

            foreach (var branch in orderedLocations.Skip(1))
            {
                var branchCity = ResolveLocationCity(branch);
                if (!string.IsNullOrWhiteSpace(branchCity))
                    return branchCity;
            }

            return string.Empty;
        }

        public static string ResolveLocationCity(RestaurantLocation? location)
        {
            if (location == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(location.City))
                return location.City.Trim();

            if (!string.IsNullOrWhiteSpace(location.GeographyCity?.NativeName))
                return location.GeographyCity.NativeName.Trim();

            if (!string.IsNullOrWhiteSpace(location.GeographyCity?.Name))
                return location.GeographyCity.Name.Trim();

            return string.Empty;
        }

        private static string ResolveEmployerProfileCity(Employer employer)
        {
            if (!string.IsNullOrWhiteSpace(employer.Address?.City?.Name))
                return employer.Address.City.Name.Trim();

            if (!string.IsNullOrWhiteSpace(employer.GeographyCity?.NativeName))
                return employer.GeographyCity.NativeName.Trim();

            if (!string.IsNullOrWhiteSpace(employer.GeographyCity?.Name))
                return employer.GeographyCity.Name.Trim();

            return string.Empty;
        }
    }
}

using Core.Helpers;
using Core.Models.Entities;
using Core.Models.ValueObjects;
using UletiSmenu.Tests.TestHelpers;

namespace UletiSmenu.Tests.Helpers
{
    public class EmployerDisplayCityResolverTests
    {
        [Fact]
        public void Resolve_ShouldPreferMainLocationCity()
        {
            var employer = CreateEmployerWithProfileCity("Beograd");
            var mainId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var branchId = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var main = CreateLocation(mainId, employer.Id, "Novi Sad");
            var branch = CreateLocation(branchId, employer.Id, "Niš");

            var city = EmployerDisplayCityResolver.Resolve(employer, [main, branch]);

            Assert.Equal("Novi Sad", city);
        }

        [Fact]
        public void Resolve_ShouldUseEmployerProfileCity_WhenMainLocationCityMissing()
        {
            var employer = CreateEmployerWithProfileCity("Beograd");
            var main = CreateLocation(Guid.Parse("00000000-0000-0000-0000-000000000001"), employer.Id, "placeholder");
            ClearLocationCity(main);

            var city = EmployerDisplayCityResolver.Resolve(employer, [main]);

            Assert.Equal("Beograd", city);
        }

        [Fact]
        public void Resolve_ShouldUseFirstBranchCity_WhenMainAndProfileCitiesMissing()
        {
            var employer = CreateEmployerWithProfileCity("");
            var main = CreateLocation(Guid.Parse("00000000-0000-0000-0000-000000000001"), employer.Id, "placeholder");
            ClearLocationCity(main);
            var branch = CreateLocation(Guid.Parse("00000000-0000-0000-0000-000000000002"), employer.Id, "Subotica");

            var city = EmployerDisplayCityResolver.Resolve(employer, [main, branch]);

            Assert.Equal("Subotica", city);
        }

        [Fact]
        public void Resolve_ShouldReturnEmpty_WhenNoCityAnywhere()
        {
            var employer = CreateEmployerWithProfileCity("");
            var main = CreateLocation(Guid.Parse("00000000-0000-0000-0000-000000000001"), employer.Id, "placeholder");
            ClearLocationCity(main);
            var branch = CreateLocation(Guid.Parse("00000000-0000-0000-0000-000000000002"), employer.Id, "placeholder");
            ClearLocationCity(branch);

            var city = EmployerDisplayCityResolver.Resolve(employer, [main, branch]);

            Assert.Equal(string.Empty, city);
        }

        [Fact]
        public void Resolve_ShouldNotLetEmptyMainCityOverwriteEmployerCity()
        {
            var employer = CreateEmployerWithProfileCity("Kragujevac");
            var main = CreateLocation(Guid.Parse("00000000-0000-0000-0000-000000000001"), employer.Id, "placeholder");
            ClearLocationCity(main);
            var branch = CreateLocation(Guid.Parse("00000000-0000-0000-0000-000000000002"), employer.Id, "Čačak");

            var city = EmployerDisplayCityResolver.Resolve(employer, [main, branch]);

            Assert.Equal("Kragujevac", city);
        }

        private static Employer CreateEmployerWithProfileCity(string cityName)
        {
            var street = HelperMethods.EnsureSuccess(Street.Create("Main Street", "1"));
            var postalCode = HelperMethods.EnsureSuccess(PostalCode.Create("11000"));
            var country = HelperMethods.EnsureSuccess(Country.Create("Srbija"));
            var region = HelperMethods.EnsureSuccess(Region.Create("Beograd"));
            var city = string.IsNullOrWhiteSpace(cityName)
                ? City.Empty()
                : HelperMethods.EnsureSuccess(City.Create(cityName, postalCode, country, region));
            var address = HelperMethods.EnsureSuccess(Address.Create(street, city));

            return HelperMethods.EnsureSuccess(Employer.Create(
                Guid.NewGuid(),
                "Restoran Test",
                "employer@example.com",
                "employer",
                "060111222",
                null,
                HelperMethods.EnsureSuccess(PIB.Create("123456789")),
                HelperMethods.EnsureSuccess(MB.Create("12345678")),
                null,
                null,
                null,
                address));
        }

        private static RestaurantLocation CreateLocation(Guid id, Guid employerId, string city)
        {
            return HelperMethods.EnsureSuccess(RestaurantLocation.Create(
                id,
                employerId,
                "Lokacija",
                "060111222",
                "123456789",
                "12345678",
                "Ulica",
                "1",
                city,
                "11000",
                "Srbija",
                "Beograd",
                "RS",
                "89010",
                "802824"));
        }

        private static void ClearLocationCity(RestaurantLocation location)
        {
            typeof(RestaurantLocation)
                .GetProperty(nameof(RestaurantLocation.City))!
                .SetValue(location, string.Empty);
        }
    }
}

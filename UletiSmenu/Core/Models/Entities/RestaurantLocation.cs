using CSharpFunctionalExtensions;

namespace Core.Models.Entities
{
    public class RestaurantLocation
    {
        public Guid Id { get; private set; }
        public Guid EmployerId { get; private set; }
        public Employer Employer { get; private set; }
        public string Name { get; private set; }
        public string PhoneNumber { get; private set; }
        public string PIB { get; private set; } = string.Empty;
        public string MB { get; private set; } = string.Empty;
        public string StreetName { get; private set; }
        public string StreetNumber { get; private set; }
        public string City { get; private set; }
        public string PostalCode { get; private set; }
        public string Country { get; private set; }
        public string Region { get; private set; }
        public string? GeographyCountryCode { get; private set; }
        public string? GeographyRegionCode { get; private set; }
        public string? GeographyCityCode { get; private set; }
        public GeographyCountry? GeographyCountry { get; private set; }
        public GeographyRegion? GeographyRegion { get; private set; }
        public GeographyCity? GeographyCity { get; private set; }

        private RestaurantLocation() { }

        private RestaurantLocation(
            Guid id,
            Guid employerId,
            string name,
            string phoneNumber,
            string pib,
            string mb,
            string streetName,
            string streetNumber,
            string city,
            string postalCode,
            string country,
            string region,
            string geographyCountryCode,
            string geographyRegionCode,
            string geographyCityCode)
        {
            Id = id;
            EmployerId = employerId;
            Name = name;
            PhoneNumber = phoneNumber;
            PIB = pib;
            MB = mb;
            StreetName = streetName;
            StreetNumber = streetNumber;
            City = city;
            PostalCode = postalCode;
            Country = country;
            Region = region;
            GeographyCountryCode = geographyCountryCode;
            GeographyRegionCode = geographyRegionCode;
            GeographyCityCode = geographyCityCode;
        }

        public static Result<RestaurantLocation> Create(
            Guid id,
            Guid employerId,
            string name,
            string phoneNumber,
            string pib,
            string mb,
            string streetName,
            string streetNumber,
            string city,
            string postalCode,
            string country,
            string region,
            string geographyCountryCode,
            string geographyRegionCode,
            string geographyCityCode)
        {
            if (id == Guid.Empty)
                return Result.Failure<RestaurantLocation>("Location ID cannot be empty.");

            if (employerId == Guid.Empty)
                return Result.Failure<RestaurantLocation>("Employer ID cannot be empty.");

            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure<RestaurantLocation>("Location name cannot be empty.");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                return Result.Failure<RestaurantLocation>("Phone number cannot be empty.");

            if (global::PIB.Create(pib).IsFailure)
                return Result.Failure<RestaurantLocation>("PIB is invalid.");

            if (global::MB.Create(mb).IsFailure)
                return Result.Failure<RestaurantLocation>("MB is invalid.");

            if (string.IsNullOrWhiteSpace(streetName))
                return Result.Failure<RestaurantLocation>("Street name cannot be empty.");

            if (string.IsNullOrWhiteSpace(streetNumber))
                return Result.Failure<RestaurantLocation>("Street number cannot be empty.");

            if (string.IsNullOrWhiteSpace(city))
                return Result.Failure<RestaurantLocation>("City cannot be empty.");

            if (string.IsNullOrWhiteSpace(postalCode))
                return Result.Failure<RestaurantLocation>("Postal code cannot be empty.");

            if (string.IsNullOrWhiteSpace(country))
                return Result.Failure<RestaurantLocation>("Country cannot be empty.");

            if (string.IsNullOrWhiteSpace(region))
                return Result.Failure<RestaurantLocation>("Region cannot be empty.");

            if (string.IsNullOrWhiteSpace(geographyCountryCode) ||
                string.IsNullOrWhiteSpace(geographyRegionCode) ||
                string.IsNullOrWhiteSpace(geographyCityCode))
            {
                return Result.Failure<RestaurantLocation>(
                    "Country, region, and city codes are required.");
            }

            return Result.Success(new RestaurantLocation(
                id, employerId, name, phoneNumber, pib.Trim(), mb.Trim(), streetName, streetNumber,
                city, postalCode, country, region, geographyCountryCode.Trim().ToUpperInvariant(),
                geographyRegionCode.Trim(), geographyCityCode.Trim()));
        }

        public Result Update(
            string name,
            string phoneNumber,
            string pib,
            string mb,
            string streetName,
            string streetNumber,
            string city,
            string postalCode,
            string country,
            string region,
            string geographyCountryCode,
            string geographyRegionCode,
            string geographyCityCode)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure("Location name cannot be empty.");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                return Result.Failure("Phone number cannot be empty.");

            if (global::PIB.Create(pib).IsFailure)
                return Result.Failure("PIB is invalid.");

            if (global::MB.Create(mb).IsFailure)
                return Result.Failure("MB is invalid.");

            if (string.IsNullOrWhiteSpace(streetName))
                return Result.Failure("Street name cannot be empty.");

            if (string.IsNullOrWhiteSpace(streetNumber))
                return Result.Failure("Street number cannot be empty.");

            if (string.IsNullOrWhiteSpace(city))
                return Result.Failure("City cannot be empty.");

            if (string.IsNullOrWhiteSpace(postalCode))
                return Result.Failure("Postal code cannot be empty.");

            if (string.IsNullOrWhiteSpace(country))
                return Result.Failure("Country cannot be empty.");

            if (string.IsNullOrWhiteSpace(region))
                return Result.Failure("Region cannot be empty.");

            if (string.IsNullOrWhiteSpace(geographyCountryCode) ||
                string.IsNullOrWhiteSpace(geographyRegionCode) ||
                string.IsNullOrWhiteSpace(geographyCityCode))
            {
                return Result.Failure("Country, region, and city codes are required.");
            }

            Name = name.Trim();
            PhoneNumber = phoneNumber.Trim();
            PIB = pib.Trim();
            MB = mb.Trim();
            StreetName = streetName.Trim();
            StreetNumber = streetNumber.Trim();
            City = city.Trim();
            PostalCode = postalCode.Trim();
            Country = country.Trim();
            Region = region.Trim();
            GeographyCountryCode = geographyCountryCode.Trim().ToUpperInvariant();
            GeographyRegionCode = geographyRegionCode.Trim();
            GeographyCityCode = geographyCityCode.Trim();

            return Result.Success();
        }

        public void SetGeographyCodes(
            string geographyCountryCode,
            string geographyRegionCode,
            string geographyCityCode)
        {
            GeographyCountryCode = geographyCountryCode.Trim().ToUpperInvariant();
            GeographyRegionCode = geographyRegionCode.Trim();
            GeographyCityCode = geographyCityCode.Trim();
        }
    }
}

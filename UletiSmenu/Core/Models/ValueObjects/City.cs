using CSharpFunctionalExtensions;

namespace Core.Models.ValueObjects
{
    public class City : ValueObject
    {
        public string Name { get; }
        public PostalCode PostalCode { get; private set; }
        public Country Country { get; private set; }
        public Region Region { get; private set; }

        private City(string name, PostalCode postalCode, Country country, Region region)
        {
            Name = name;
            PostalCode = postalCode;
            Country = country;
            Region = region;
        }

        public City() { }

        public static Result<City> Create(string name, PostalCode postalCode, Country country, Region region)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result.Failure<City>("City name cannot be empty.");
            }

            return Result.Success(new City(name, postalCode, country, region));
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
            yield return PostalCode;
            yield return Country;
            yield return Region;
        }
    }
}

using CSharpFunctionalExtensions;

namespace Core.Models.ValueObjects
{
    public class City : ValueObject
    {
        public string Name { get; }
        public PostalCode PostalCode { get; }
        public Country Country { get; }
        public Region Region { get; }

        private City(string name, PostalCode postalCode, Country country, Region region)
        {
            Name = name;
            PostalCode = postalCode;
            Country = country;
            Region = region;
        }

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
            throw new NotImplementedException();
        }
    }
}

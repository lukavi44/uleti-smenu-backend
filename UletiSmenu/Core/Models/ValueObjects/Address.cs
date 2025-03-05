using CSharpFunctionalExtensions;

namespace Core.Models.ValueObjects
{
    public class Address : ValueObject
    {
        public Street Street { get; }
        public City City { get; }

        public Address() { }

        private Address(Street street, City city)
        {
            Street = street;
            City = city;
        }

        public static Result<Address> Create(Street street, City city)
        {
            return Result.Success(new Address(street, city));
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
        }
    }

}

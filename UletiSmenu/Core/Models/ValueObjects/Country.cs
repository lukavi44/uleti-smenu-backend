using CSharpFunctionalExtensions;

namespace Core.Models.ValueObjects
{
    public class Country : ValueObject
    {
        public string Name { get; }

        private Country(string name)
        {
            Name = name;
        }

        public static Result<Country> Create(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result.Failure<Country>("Country name cannot be empty.");
            }

            return Result.Success(new Country(name));
        }

        protected override IEnumerable<IComparable> GetEqualityComponents()
        {
            yield return Name;
        }
    }
}

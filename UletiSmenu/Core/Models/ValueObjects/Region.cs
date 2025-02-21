using CSharpFunctionalExtensions;

namespace Core.Models.ValueObjects
{
    public class Region : ValueObject
    {
        public string Name { get; }

        private Region(string name)
        {
            Name = name;
        }

        public static Result<Region> Create(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result.Failure<Region>("Region name cannot be empty.");
            }

            return Result.Success(new Region(name));
        }

        protected override IEnumerable<IComparable> GetEqualityComponents()
        {
            yield return Name;
        }
    }
}

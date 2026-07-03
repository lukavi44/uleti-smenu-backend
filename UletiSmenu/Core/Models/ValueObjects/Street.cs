using CSharpFunctionalExtensions;

namespace Core.Models.ValueObjects
{
    public class Street : ValueObject
    {
        public string Name { get; }
        public string Number { get; }

        private Street(string name, string number)
        {
            Name = name;
            Number = number;    
        }

        public Street() { }

        public static Result<Street> Create(string name, string number)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result.Failure<Street>("Name cannot be empty.");
            }

            return Result.Success(new Street(name, number));
        }

        public static Street Empty() => new(string.Empty, string.Empty);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
            yield return Number;
        }
    }
}

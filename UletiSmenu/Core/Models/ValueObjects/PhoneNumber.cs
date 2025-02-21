using CSharpFunctionalExtensions;

namespace Core.Models.ValueObjects
{
    public class PhoneNumber : ValueObject
    {
        public string Value { get; }

        private PhoneNumber(string phoneNumber)
        {
            Value = phoneNumber;
        }

        public static Result<PhoneNumber> Create(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return Result.Failure<PhoneNumber>("Phone number cannot be empty");
            }

            return Result.Success(new PhoneNumber(phoneNumber));
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}

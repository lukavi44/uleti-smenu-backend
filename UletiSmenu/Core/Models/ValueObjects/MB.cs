using CSharpFunctionalExtensions;

public class MB : ValueObject
{
    public string Value { get; }

    private MB(string value)
    {
        Value = value;
    }

    public static Result<MB> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<MB>("MB cannot be empty.");

        if (!value.All(char.IsDigit))
            return Result.Failure<MB>("MB must contain only numbers.");

        if (value.Length != 8)
            return Result.Failure<MB>("MB must be exactly 9 digits long.");

        return Result.Success(new MB(value));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

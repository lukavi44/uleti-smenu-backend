using CSharpFunctionalExtensions;

public class PIB : ValueObject
{
    public string Value { get; }

    private PIB(string value)
    {
        Value = value;
    }

    public static Result<PIB> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<PIB>("PIB cannot be empty.");

        if (!value.All(char.IsDigit))
            return Result.Failure<PIB>("PIB must contain only numbers.");

        if (value.Length != 9)
            return Result.Failure<PIB>("PIB must be exactly 9 digits long.");

        return Result.Success(new PIB(value));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

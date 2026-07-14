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

    /// <summary>
    /// Materializes PIB from storage. Incomplete employer profiles may store an empty value.
    /// </summary>
    public static PIB FromPersistence(string? value) =>
        string.IsNullOrWhiteSpace(value) ? Empty() : Create(value).Value;

    public static PIB Empty() => new(string.Empty);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

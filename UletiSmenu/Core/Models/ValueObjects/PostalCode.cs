using CSharpFunctionalExtensions;

namespace Core.Models.ValueObjects;

public class PostalCode : ValueObject
{
    public string Value { get; }

    private PostalCode(string value)
    {
        Value = value;
    }

    public static Result<PostalCode> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<PostalCode>("Postal code cannot be empty.");
        }

        return Result.Success(new PostalCode(value));
    }

    protected override IEnumerable<IComparable> GetEqualityComponents()
    {
        yield return Value;
    }
}

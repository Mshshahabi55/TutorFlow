using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Identity.ValueObjects;

public sealed class AuthTokenId : ValueObject
{
    private AuthTokenId(Guid value) => Value = value;

    public Guid Value { get; }

    public static AuthTokenId New() => new(Guid.NewGuid());

    public static AuthTokenId From(Guid value) => new(Guard.Against.Default(value, nameof(value)));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}

using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Scheduling.ValueObjects;

public sealed class SessionId : ValueObject
{
    private SessionId(Guid value) => Value = value;

    public Guid Value { get; }

    public static SessionId New() => new(Guid.NewGuid());

    public static SessionId From(Guid value) => new(Guard.Against.Default(value, nameof(value)));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}

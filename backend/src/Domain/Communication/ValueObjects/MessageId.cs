using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Communication.ValueObjects;

public sealed class MessageId : ValueObject
{
    private MessageId(Guid value) => Value = value;

    public Guid Value { get; }

    public static MessageId New() => new(Guid.NewGuid());

    public static MessageId From(Guid value) => new(Guard.Against.Default(value, nameof(value)));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}

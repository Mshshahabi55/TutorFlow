using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Communication.ValueObjects;

public sealed class ConversationId : ValueObject
{
    private ConversationId(Guid value) => Value = value;

    public Guid Value { get; }

    public static ConversationId New() => new(Guid.NewGuid());

    public static ConversationId From(Guid value) => new(Guard.Against.Default(value, nameof(value)));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}

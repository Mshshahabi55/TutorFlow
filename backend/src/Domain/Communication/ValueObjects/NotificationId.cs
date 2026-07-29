using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Communication.ValueObjects;

public sealed class NotificationId : ValueObject
{
    private NotificationId(Guid value) => Value = value;

    public Guid Value { get; }

    public static NotificationId New() => new(Guid.NewGuid());

    public static NotificationId From(Guid value) => new(Guard.Against.Default(value, nameof(value)));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}

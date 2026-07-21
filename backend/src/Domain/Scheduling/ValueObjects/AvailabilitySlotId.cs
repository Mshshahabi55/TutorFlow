using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Scheduling.ValueObjects;

public sealed class AvailabilitySlotId : ValueObject
{
    private AvailabilitySlotId(Guid value) => Value = value;

    public Guid Value { get; }

    public static AvailabilitySlotId New() => new(Guid.NewGuid());

    public static AvailabilitySlotId From(Guid value) => new(Guard.Against.Default(value, nameof(value)));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}

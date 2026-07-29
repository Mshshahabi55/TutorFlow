using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Meetings.ValueObjects;

public sealed class MeetingId : ValueObject
{
    private MeetingId(Guid value) => Value = value;

    public Guid Value { get; }

    public static MeetingId New() => new(Guid.NewGuid());

    public static MeetingId From(Guid value) => new(Guard.Against.Default(value, nameof(value)));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}

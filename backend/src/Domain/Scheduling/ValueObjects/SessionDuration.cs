using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Scheduling.ValueObjects;

// A length of time, defined per Tutor (PRODUCT_REQUIREMENTS.md SCH-3;
// DOMAIN_MODEL.md: Value Objects). Owned by Scheduling & Booking
// (docs/adr/ADR-002-domain-boundaries.md: Context Ownership).
public sealed class SessionDuration : ValueObject
{
    private SessionDuration(TimeSpan value) => Value = value;

    public TimeSpan Value { get; }

    public static SessionDuration Of(TimeSpan value) =>
        new(Guard.Against.NegativeOrZero(value, nameof(value)));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}

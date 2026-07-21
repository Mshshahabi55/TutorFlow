using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Scheduling.ValueObjects;

// A reference, by identity only, to a Tutor owned by the Identity &
// Relationship bounded context (docs/adr/ADR-002-domain-boundaries.md:
// Context Ownership, Integration Rules). Scheduling & Booking never mints a
// new Tutor identity — only Identity & Relationship's Tutor aggregate does —
// so no "New()" factory exists here, only reconstruction from a known value.
public sealed class TutorId : ValueObject
{
    private TutorId(Guid value) => Value = value;

    public Guid Value { get; }

    public static TutorId From(Guid value) => new(Guard.Against.Default(value, nameof(value)));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}

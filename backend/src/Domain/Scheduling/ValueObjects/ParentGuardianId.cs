using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Scheduling.ValueObjects;

// A reference, by identity only, to a Parent/Guardian owned by the Identity &
// Relationship bounded context (docs/adr/ADR-002-domain-boundaries.md:
// Context Ownership, Integration Rules). Mirrors TutorId/StudentId: Scheduling
// & Booking never mints a new Parent/Guardian identity, only references one.
public sealed class ParentGuardianId : ValueObject
{
    private ParentGuardianId(Guid value) => Value = value;

    public Guid Value { get; }

    public static ParentGuardianId From(Guid value) => new(Guard.Against.Default(value, nameof(value)));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}

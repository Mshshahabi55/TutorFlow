using TutorFlow.Domain.Common;

namespace TutorFlow.Domain.Scheduling.ValueObjects;

// A reference, by identity only, to a Student owned by the Identity &
// Relationship bounded context (docs/adr/ADR-002-domain-boundaries.md:
// Context Ownership, Integration Rules). Mirrors TutorId: Scheduling &
// Booking never mints a new Student identity, only references one.
public sealed class StudentId : ValueObject
{
    private StudentId(Guid value) => Value = value;

    public Guid Value { get; }

    public static StudentId From(Guid value) => new(Guard.Against.Default(value, nameof(value)));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}

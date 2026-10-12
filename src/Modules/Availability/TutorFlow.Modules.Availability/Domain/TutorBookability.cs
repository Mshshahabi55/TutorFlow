namespace TutorFlow.Modules.Availability.Domain;

// Availability's own local read model of "is this tutor currently bookable," built from a
// Tutors-published integration event per ADR-0011. No event consumer exists yet (Wolverine is
// intentionally not added until a real cross-module event is implemented) — this entity and
// its update method are the boundary that a future event handler will call.
public sealed class TutorBookability
{
    private TutorBookability()
    {
    }

    public Guid TutorUserId { get; private init; }

    public bool IsBookable { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static TutorBookability Create(Guid tutorUserId, bool isBookable, TimeProvider clock) => new()
    {
        TutorUserId = tutorUserId,
        IsBookable = isBookable,
        UpdatedAtUtc = clock.GetUtcNow()
    };

    public void Apply(bool isBookable, TimeProvider clock)
    {
        IsBookable = isBookable;
        UpdatedAtUtc = clock.GetUtcNow();
    }
}

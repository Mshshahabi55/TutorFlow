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

    // A44 replay-safety fix: the OccurredAtUtc of whichever TutorPublished/TutorUnpublished
    // event most recently determined IsBookable — lets TryApply detect and ignore a stale or
    // out-of-order redelivery (e.g. a delayed TutorPublished retry arriving after a later
    // TutorUnpublished already superseded it) instead of blindly overwriting a legitimately
    // newer state. Defaults to UpdatedAtUtc for callers that never pass an event timestamp
    // (existing test seed helpers outside Availability's own replay path, which don't need
    // this guard and use the original Create/Apply below).
    public DateTimeOffset LastEventAtUtc { get; private set; }

    public static TutorBookability Create(Guid tutorUserId, bool isBookable, TimeProvider clock, DateTimeOffset? eventOccurredAtUtc = null)
    {
        var now = clock.GetUtcNow();
        return new TutorBookability
        {
            TutorUserId = tutorUserId,
            IsBookable = isBookable,
            UpdatedAtUtc = now,
            LastEventAtUtc = eventOccurredAtUtc ?? now
        };
    }

    public void Apply(bool isBookable, TimeProvider clock)
    {
        IsBookable = isBookable;
        UpdatedAtUtc = clock.GetUtcNow();
        LastEventAtUtc = UpdatedAtUtc;
    }

    // A44 replay-safety fix: returns false (state left untouched) when occurredAtUtc is not
    // strictly newer than the event that last determined IsBookable, so a stale/out-of-order
    // redelivery cannot incorrectly mark an ineligible tutor bookable again (or vice versa).
    public bool TryApply(bool isBookable, DateTimeOffset occurredAtUtc, TimeProvider clock)
    {
        if (occurredAtUtc <= LastEventAtUtc)
        {
            return false;
        }

        IsBookable = isBookable;
        UpdatedAtUtc = clock.GetUtcNow();
        LastEventAtUtc = occurredAtUtc;
        return true;
    }
}

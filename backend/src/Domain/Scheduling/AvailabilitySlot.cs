using TutorFlow.Domain.Common;
using TutorFlow.Domain.Scheduling.Events;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Domain.Scheduling;

// A specific bookable time period owned by a Tutor, with a delivery mode
// (DOMAIN_MODEL.md: Aggregates). AvailabilitySlot owns booking eligibility:
// it is modeled separately from Session specifically so it can enforce
// "never bookable twice" independently of the Session it produces (CONST-1;
// DOMAIN_MODEL.md: Aggregates — "It is modeled separately here because it
// must enforce 'never bookable twice' independently of the Session it
// produces"). Session.Book(...) is internal for exactly this reason: the
// only way to produce a Session is through this aggregate's own Book(...)
// method, which checks and marks consumption first, so Session itself never
// checks — and can never bypass — this rule.
public sealed class AvailabilitySlot : AggregateRoot<AvailabilitySlotId>
{
    private AvailabilitySlot(
        AvailabilitySlotId id,
        TutorId tutorId,
        DateTime startTimeUtc,
        SessionDuration duration,
        DeliveryMode deliveryMode) : base(id)
    {
        TutorId = tutorId;
        StartTimeUtc = startTimeUtc;
        Duration = duration;
        DeliveryMode = deliveryMode;
    }

    // An Availability Slot belongs to exactly one Tutor, referenced by
    // identity only — no navigation property (DOMAIN_MODEL.md: Relationships;
    // docs/adr/ADR-002-domain-boundaries.md: Integration Rules).
    public TutorId TutorId { get; }

    public DateTime StartTimeUtc { get; }

    // Exactly one Session Duration (DOMAIN_MODEL.md: Aggregates). EndTimeUtc
    // is derived, not stored separately, so the slot can never exist with an
    // invalid time range: Duration is always positive (SessionDuration's own
    // invariant), which guarantees EndTimeUtc is always after StartTimeUtc.
    public SessionDuration Duration { get; }

    public DateTime EndTimeUtc => StartTimeUtc + Duration.Value;

    // Exactly one Delivery Mode (DOMAIN_MODEL.md: Aggregates).
    public DeliveryMode DeliveryMode { get; }

    // Whether this slot has already produced a Session. The minimum
    // additional state required to enforce CONST-1 ("An Availability Slot is
    // never associated with more than one active Session at the same time").
    public bool IsConsumed { get; private set; }

    public static AvailabilitySlot Declare(
        TutorId tutorId,
        DateTime startTimeUtc,
        SessionDuration duration,
        DeliveryMode deliveryMode)
    {
        Guard.Against.Null(tutorId, nameof(tutorId));
        Guard.Against.Default(startTimeUtc, nameof(startTimeUtc));
        Guard.Against.Null(duration, nameof(duration));

        var slot = new AvailabilitySlot(
            AvailabilitySlotId.New(),
            tutorId,
            startTimeUtc,
            duration,
            deliveryMode);

        slot.RaiseDomainEvent(new AvailabilityDeclared(slot.Id, tutorId));

        return slot;
    }

    // The only Domain method that produces a Session from this slot
    // (PRODUCT_REQUIREMENTS.md SCH-5). A consumed slot can never produce
    // another Session (CONST-1) — this is checked here, not by Session, and
    // not by any caller: Session.Book(...) is internal to this bounded
    // context, so this is the only path by which a Session can come to exist.
    public Session Book(StudentId studentId, ParentGuardianId? parentGuardianId)
    {
        if (IsConsumed)
        {
            throw new InvalidOperationException(
                "This Availability Slot has already been consumed and cannot produce another Session.");
        }

        var session = Session.Book(
            TutorId,
            studentId,
            parentGuardianId,
            Id,
            StartTimeUtc,
            Duration,
            DeliveryMode);

        IsConsumed = true;

        return session;
    }
}

using TutorFlow.Domain.Common;
using TutorFlow.Domain.Scheduling.Events;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Domain.Scheduling;

// The booked engagement that results once a Tutor's availability has been
// selected (DOMAIN_MODEL.md: Aggregates). This phase implements only the
// aggregate's own state and lifecycle — booking workflow, slot reservation,
// availability consumption, and double-booking prevention are cross-aggregate
// concerns for a later Application-layer phase, not this one.
public sealed class Session : AggregateRoot<SessionId>
{
    private Session(
        SessionId id,
        TutorId tutorId,
        StudentId studentId,
        ParentGuardianId? parentGuardianId,
        AvailabilitySlotId availabilitySlotId,
        DateTime scheduledTimeUtc,
        SessionDuration duration,
        DeliveryMode deliveryMode,
        SessionStatus status,
        SessionPrice? price) : base(id)
    {
        TutorId = tutorId;
        StudentId = studentId;
        ParentGuardianId = parentGuardianId;
        AvailabilitySlotId = availabilitySlotId;
        ScheduledTimeUtc = scheduledTimeUtc;
        Duration = duration;
        DeliveryMode = deliveryMode;
        Status = status;
        Price = price;
    }

    // Every Session identifies exactly one Tutor, referenced by identity only
    // — no navigation property (DOMAIN_MODEL.md: Relationships).
    public TutorId TutorId { get; }

    // Every Session identifies exactly one Student, referenced by identity
    // only (DOMAIN_MODEL.md: Relationships).
    public StudentId StudentId { get; }

    // A Session optionally identifies the Parent/Guardian who acted on the
    // Student's behalf, for attribution (DOMAIN_MODEL.md: Relationships;
    // PRODUCT_REQUIREMENTS.md IDR-5, IDR-6).
    public ParentGuardianId? ParentGuardianId { get; }

    // The Availability Slot this Session currently occupies, referenced by
    // identity only (DOMAIN_MODEL.md: Relationships). Mutable as of Phase
    // 4.7: Reschedule() moves a Session onto a different, already-open slot
    // rather than merely changing a timestamp — see Reschedule() below.
    public AvailabilitySlotId AvailabilitySlotId { get; private set; }

    public DateTime ScheduledTimeUtc { get; private set; }

    public SessionDuration Duration { get; private set; }

    public DateTime EndTimeUtc => ScheduledTimeUtc + Duration.Value;

    public DeliveryMode DeliveryMode { get; private set; }

    // Exactly one status at a time, drawn from the defined set
    // (PRODUCT_REQUIREMENTS.md SCH-6).
    public SessionStatus Status { get; private set; }

    // Phase 4.6: what this Session cost, captured from the Tutor's
    // HourlyRate at booking time — fixed forever after, immune to the
    // Tutor later changing their rate. Nullable, mirroring Tutor.HourlyRate's
    // own nullability exactly: a Tutor with no rate configured is still
    // bookable today (no rule requires one), so a Session booked against
    // such a Tutor simply has no price to capture, not an invented one.
    // Not yet exposed via SessionDto or any endpoint (Phase 4.6's own scope).
    public SessionPrice? Price { get; }

    // Internal: the only caller is AvailabilitySlot.Book(...). A consumed
    // slot can never produce more than one Session (CONST-1) — AvailabilitySlot
    // owns that check, so Session must never be created independently of it
    // and never checks this rule itself (see AvailabilitySlot.Book).
    internal static Session Book(
        TutorId tutorId,
        StudentId studentId,
        ParentGuardianId? parentGuardianId,
        AvailabilitySlotId availabilitySlotId,
        DateTime scheduledTimeUtc,
        SessionDuration duration,
        DeliveryMode deliveryMode,
        SessionPrice? price)
    {
        Guard.Against.Null(tutorId, nameof(tutorId));
        Guard.Against.Null(studentId, nameof(studentId));
        Guard.Against.Null(availabilitySlotId, nameof(availabilitySlotId));
        Guard.Against.Default(scheduledTimeUtc, nameof(scheduledTimeUtc));
        Guard.Against.Null(duration, nameof(duration));

        var session = new Session(
            SessionId.New(),
            tutorId,
            studentId,
            parentGuardianId,
            availabilitySlotId,
            scheduledTimeUtc,
            duration,
            deliveryMode,
            SessionStatus.Scheduled,
            price);

        session.RaiseDomainEvent(new SessionBooked(session.Id, tutorId, studentId, availabilitySlotId));

        return session;
    }

    // Phase 4.7: rescheduling targets an existing, open Availability Slot —
    // exactly like booking, not an arbitrary timestamp. This closes a real
    // double-booking defect (PHASE-046-REPORT.md, PHASE-047-REPORT.md
    // Task 1): the previous timestamp-only Reschedule left the new time
    // unprotected by any slot-occupancy invariant, and left the Session's
    // original slot permanently IsConsumed with a StartTimeUtc/Duration
    // describing a time the Session no longer occupies. The five inputs
    // below are the target slot's own fields, supplied by the caller
    // exactly like Session.Book(...) already takes AvailabilitySlot's
    // fields rather than a reference to the slot itself — Session never
    // holds a reference to AvailabilitySlot, only an id (DOMAIN_MODEL.md:
    // Relationships). Status is unchanged: "Rescheduled" is an event, not
    // one of the four defined statuses (SCH-6). Session.Price is
    // deliberately untouched — see PHASE-047-REPORT.md §2 for why
    // re-deriving it here would undo Phase 4.6's "price fixed at original
    // booking time" invariant.
    //
    // Guards, each stated explicitly:
    // - Only a Scheduled Session can be rescheduled (mirrors Cancel/
    //   Complete/MarkNoShow's own single-transition guard).
    // - The new slot must belong to the same Tutor as this Session — a
    //   Session belongs to one Tutor for its whole lifetime
    //   (DOMAIN_MODEL.md: Relationships); rescheduling must never silently
    //   reassign a Session to a different Tutor's slot.
    // - The new slot must not be the slot this Session already occupies —
    //   that would be a no-op dressed up as a state change, and would
    //   otherwise require reopening and re-consuming the same
    //   AvailabilitySlot instance within one transaction for no reason.
    // - The new slot must be un-consumed: enforced by
    //   AvailabilitySlot.Consume() at the call site (this method has no
    //   visibility into the slot's IsConsumed flag — only its id/Tutor/
    //   time/duration/delivery mode are passed in), the same separation of
    //   concerns Book() already establishes (AvailabilitySlot alone owns
    //   consumption; Session never checks it itself).
    // Deliberately does NOT guard against newScheduledTimeUtc already being
    // in the past — Book()/Reopen() never have either (no minimum/maximum
    // lead time exists; DOMAIN_MODEL.md Open Question 8, still open).
    public void Reschedule(
        AvailabilitySlotId newAvailabilitySlotId,
        TutorId newAvailabilitySlotTutorId,
        DateTime newScheduledTimeUtc,
        SessionDuration newDuration,
        DeliveryMode newDeliveryMode)
    {
        if (Status != SessionStatus.Scheduled)
        {
            throw new InvalidOperationException("Only a Scheduled Session can be rescheduled.");
        }

        Guard.Against.Null(newAvailabilitySlotId, nameof(newAvailabilitySlotId));
        Guard.Against.Null(newAvailabilitySlotTutorId, nameof(newAvailabilitySlotTutorId));
        Guard.Against.Default(newScheduledTimeUtc, nameof(newScheduledTimeUtc));
        Guard.Against.Null(newDuration, nameof(newDuration));

        if (newAvailabilitySlotTutorId != TutorId)
        {
            throw new InvalidOperationException(
                "A Session can only be rescheduled onto an Availability Slot belonging to the same Tutor.");
        }

        if (newAvailabilitySlotId == AvailabilitySlotId)
        {
            throw new InvalidOperationException(
                "A Session cannot be rescheduled onto the Availability Slot it already occupies.");
        }

        var oldAvailabilitySlotId = AvailabilitySlotId;

        AvailabilitySlotId = newAvailabilitySlotId;
        ScheduledTimeUtc = newScheduledTimeUtc;
        Duration = newDuration;
        DeliveryMode = newDeliveryMode;

        RaiseDomainEvent(new SessionRescheduled(Id, oldAvailabilitySlotId, newAvailabilitySlotId, newScheduledTimeUtc));
    }

    public void Cancel()
    {
        if (Status != SessionStatus.Scheduled)
        {
            throw new InvalidOperationException("Only a Scheduled Session can be cancelled.");
        }

        Status = SessionStatus.Cancelled;
        RaiseDomainEvent(new SessionCancelled(Id));
    }

    public void Complete()
    {
        if (Status != SessionStatus.Scheduled)
        {
            throw new InvalidOperationException("Only a Scheduled Session can be completed.");
        }

        Status = SessionStatus.Completed;
        RaiseDomainEvent(new SessionCompleted(Id));
    }

    public void MarkNoShow()
    {
        if (Status != SessionStatus.Scheduled)
        {
            throw new InvalidOperationException("Only a Scheduled Session can be marked No-Show.");
        }

        Status = SessionStatus.NoShow;
        RaiseDomainEvent(new SessionMarkedNoShow(Id));
    }
}

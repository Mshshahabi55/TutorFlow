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
        SessionStatus status) : base(id)
    {
        TutorId = tutorId;
        StudentId = studentId;
        ParentGuardianId = parentGuardianId;
        AvailabilitySlotId = availabilitySlotId;
        ScheduledTimeUtc = scheduledTimeUtc;
        Duration = duration;
        DeliveryMode = deliveryMode;
        Status = status;
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

    // The Availability Slot this Session originated from, referenced by
    // identity only (DOMAIN_MODEL.md: Relationships).
    public AvailabilitySlotId AvailabilitySlotId { get; }

    public DateTime ScheduledTimeUtc { get; private set; }

    public SessionDuration Duration { get; }

    public DateTime EndTimeUtc => ScheduledTimeUtc + Duration.Value;

    public DeliveryMode DeliveryMode { get; }

    // Exactly one status at a time, drawn from the defined set
    // (PRODUCT_REQUIREMENTS.md SCH-6).
    public SessionStatus Status { get; private set; }

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
        DeliveryMode deliveryMode)
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
            SessionStatus.Scheduled);

        session.RaiseDomainEvent(new SessionBooked(session.Id, tutorId, studentId, availabilitySlotId));

        return session;
    }

    // A Session's time is changed by a permitted actor (PRODUCT_REQUIREMENTS.md
    // SCH-7). Status is unchanged: "Rescheduled" is an event, not one of the
    // four defined statuses (SCH-6).
    public void Reschedule(DateTime newScheduledTimeUtc)
    {
        if (Status != SessionStatus.Scheduled)
        {
            throw new InvalidOperationException("Only a Scheduled Session can be rescheduled.");
        }

        Guard.Against.Default(newScheduledTimeUtc, nameof(newScheduledTimeUtc));

        ScheduledTimeUtc = newScheduledTimeUtc;
        RaiseDomainEvent(new SessionRescheduled(Id, newScheduledTimeUtc));
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

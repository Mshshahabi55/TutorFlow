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

    // existingSlotsForTutor: every AvailabilitySlot already declared by this
    // same Tutor (any consumption state — a booked slot still occupies that
    // time, same as an open one), supplied by the caller (Application layer,
    // via IAvailabilitySlotRepository.GetByTutorIdAsync) since this aggregate
    // has no repository access of its own. Phase 8a closes the one gap
    // BookSessionCommandHandler's own comment flagged as "explicitly
    // deferred to a later phase": a Tutor could previously declare two
    // slots that overlap in time. Two slots that only touch (one ends
    // exactly when the other starts) are not an overlap — strict inequality
    // on both sides — since a Tutor scheduling back-to-back teaching time is
    // the normal case, not a conflict.
    //
    // Optional (defaults to "no existing slots"), not required: the one real
    // production caller (DeclareAvailabilityCommandHandler) always supplies
    // the real list and gets the invariant enforced. Making it required
    // would force every one of the ~25 unrelated test fixtures elsewhere in
    // this codebase (Meetings, Discovery, Oversight, Infrastructure tests
    // that just need *a* slot to exist) to thread a same-Tutor overlap
    // concept they have no interest in — an omitted argument here means
    // "this call site isn't exercising the overlap invariant," not that the
    // invariant is optional in Domain; it's still enforced in full whenever
    // real data is passed, which is the only path real API traffic takes.
    public static AvailabilitySlot Declare(
        TutorId tutorId,
        DateTime startTimeUtc,
        SessionDuration duration,
        DeliveryMode deliveryMode,
        IEnumerable<AvailabilitySlot>? existingSlotsForTutor = null)
    {
        Guard.Against.Null(tutorId, nameof(tutorId));
        Guard.Against.Default(startTimeUtc, nameof(startTimeUtc));
        Guard.Against.Null(duration, nameof(duration));

        var endTimeUtc = startTimeUtc + duration.Value;

        foreach (var existing in existingSlotsForTutor ?? [])
        {
            var overlaps = startTimeUtc < existing.EndTimeUtc && endTimeUtc > existing.StartTimeUtc;
            if (overlaps)
            {
                throw new InvalidOperationException(
                    "This time range overlaps an Availability Slot you have already declared.");
            }
        }

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
    // price: Phase 4.6, the Tutor's HourlyRate as read by the Application
    // layer at booking time (or null if the Tutor has none configured) —
    // AvailabilitySlot only knows TutorId, never the Tutor's rate itself
    // (a different aggregate, owned by Identity & Relationship), so this
    // must be supplied by the caller, exactly like studentId/parentGuardianId.
    public Session Book(StudentId studentId, ParentGuardianId? parentGuardianId, SessionPrice? price = null)
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
            DeliveryMode,
            price);

        IsConsumed = true;

        return session;
    }

    // Phase 4.6: formally resolves DOMAIN_MODEL.md Open Question 7 —
    // cancelling a Session does reopen its Availability Slot. Originally
    // the only legitimate caller was CancelSessionCommandHandler,
    // immediately after Session.Cancel() succeeds (both persisted in the
    // same SaveChangesAsync call — ADR-004's Transaction Boundaries already
    // anticipated this exact atomicity once Question 7 resolved: "what
    // happens to the associated Availability Slot on cancellation... the
    // full transactional shape of cancellation is not yet complete").
    // Phase 4.7 adds a second legitimate caller, RescheduleSessionCommandHandler
    // — rescheduling is cancel-and-rebook of the same Session, so the old
    // slot is released via this exact same method, immediately after
    // Session.Reschedule() succeeds, with the new slot's own Consume()
    // and the Session's own save in the same transaction.
    //
    // AvailabilitySlot has no reference to the Session that consumed it —
    // by design, aggregates reference each other by identity only, and
    // only Session holds AvailabilitySlotId, never the reverse — so this
    // method cannot itself verify "the session that consumed this slot is
    // really the one being cancelled," or that it's genuinely still active.
    // That ordering (cancel the Session first, check it actually succeeded,
    // only then reopen the slot it named) is guaranteed by the calling
    // handler, not by this aggregate; cancelledSessionId is accepted only
    // for audit attribution (AvailabilitySlotReopened), not validated here.
    //
    // Deliberately does NOT guard against StartTimeUtc already being in the
    // past: Book() itself has never guarded against booking a past slot
    // either (no minimum/maximum lead time is established —
    // DOMAIN_MODEL.md Open Question 8, still open). Adding an asymmetric
    // "cannot reopen a past slot" rule here, where none exists on the
    // original booking path, would be inventing a new business rule this
    // phase is not authorized to decide, not fixing a bug.
    public void Reopen(SessionId cancelledSessionId)
    {
        if (!IsConsumed)
        {
            throw new InvalidOperationException(
                "Only a consumed Availability Slot can be reopened.");
        }

        IsConsumed = false;
        RaiseDomainEvent(new AvailabilitySlotReopened(Id, cancelledSessionId));
    }

    // Phase 4.7: the "book the new slot" half of rescheduling an existing
    // Session onto this slot. Deliberately separate from Book(): Book()
    // both validates IsConsumed and constructs a brand-new Session in one
    // step, which is wrong here — the Session already exists and is only
    // being moved onto this slot by RescheduleSessionCommandHandler
    // (Session.Reschedule(...) mutates the Session side; this mutates the
    // slot side; both in the same SaveChangesAsync call). Raises no event
    // of its own, mirroring Book() itself (see SessionBooked, which already
    // carries the AvailabilitySlotId, and SessionRescheduled, which
    // similarly already carries both slot ids) — a separate
    // AvailabilitySlotConsumed event would duplicate that same audit fact.
    public void Consume()
    {
        if (IsConsumed)
        {
            throw new InvalidOperationException(
                "This Availability Slot has already been consumed and cannot be assigned another Session.");
        }

        IsConsumed = true;
    }
}

namespace TutorFlow.Modules.Bookings.Domain;

/// <summary>
/// A44 #103 bounded fallback for <c>BookingHoldExpirySweepService</c> (see
/// <c>docs/OPEN_QUESTIONS.md</c>'s A44 entry): a durable record of "this booking's hold expired
/// and a <c>BookingHoldExpired</c> event is owed" — persisted in Bookings' own database, in the
/// SAME transaction as the booking's status change, so a process crash between the two can never
/// happen. Not a Wolverine outbox envelope — this table exists specifically because Wolverine's
/// own cross-store outbox mechanism was investigated and found unsafe for this path (see
/// Program.cs's A44 #103 follow-up comment on the Ancillary store registrations); this is a
/// plain, Bookings-owned durability record, dispatched by <see cref="Infrastructure.BookingHoldExpirySweepService"/>'s
/// own dispatch phase via an ordinary <c>bus.PublishAsync</c> — no envelope-store ambiguity is
/// possible because nothing here asks Wolverine to persist anything transactionally cross-store.
/// </summary>
public sealed class BookingHoldExpiryIntent
{
    private BookingHoldExpiryIntent()
    {
    }

    public Guid Id { get; private init; }

    public Guid BookingId { get; private init; }

    public Guid StudentUserId { get; private init; }

    public Guid TutorUserId { get; private init; }

    public DateTimeOffset StartUtc { get; private init; }

    public DateTimeOffset EndUtc { get; private init; }

    public int DurationMinutes { get; private init; }

    public bool IsTrial { get; private init; }

    public DateTimeOffset ExpiredAtUtc { get; private init; }

    public DateTimeOffset CreatedAtUtc { get; private init; }

    /// <summary>Null means "still owed" — the dispatch phase's own query target. Set only after
    /// <c>bus.PublishAsync</c> for this intent's <c>BookingHoldExpired</c> event has actually
    /// returned, never before (see the dispatcher's own remarks for why that order matters).</summary>
    public DateTimeOffset? DispatchedAtUtc { get; private set; }

    public static BookingHoldExpiryIntent Create(
        Guid bookingId,
        Guid studentUserId,
        Guid tutorUserId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        int durationMinutes,
        bool isTrial,
        DateTimeOffset expiredAtUtc) => new()
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            StudentUserId = studentUserId,
            TutorUserId = tutorUserId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            DurationMinutes = durationMinutes,
            IsTrial = isTrial,
            ExpiredAtUtc = expiredAtUtc,
            CreatedAtUtc = expiredAtUtc,
        };

    public void MarkDispatched(DateTimeOffset dispatchedAtUtc) => DispatchedAtUtc = dispatchedAtUtc;
}

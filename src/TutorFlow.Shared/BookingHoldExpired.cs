namespace TutorFlow.Shared;

/// <summary>
/// Published by <c>Bookings</c> immediately after a <c>Held</c>/<c>PendingPayment</c> booking's
/// hold TTL has genuinely lapsed without the student completing checkout — never on an
/// already-terminal booking (Confirmed/Cancelled/Completed/NoShow/Refunded/Rescheduled are all
/// excluded by construction, see <c>BookingHoldExpirySweepService</c>). The conditional-UPDATE
/// guard that *claims* a stale hold only ever succeeds once per row, but this event is
/// at-least-once, not exactly-once: <c>BookingHoldExpirySweepService</c>'s own dispatch phase has
/// no claim/lock on "not yet dispatched," so concurrent sweep executions can both publish the
/// same already-claimed intent (confirmed directly — see
/// <c>ConcurrentSweeps_RacingTheSamePendingIntent_...</c> in Host.Tests). Every consumer of this
/// event must be idempotent against a duplicate delivery with the same <c>BookingId</c> — see
/// <c>Notifications.Features.SendAbandonedCheckoutRecoveryEmail.Handler</c>'s own claim/resolve
/// protocol for the established pattern. Carries only what <c>Bookings</c>' own domain already
/// knows — no presentation-specific content (subject lines, HTML, CTA copy) belongs here; that is
/// <c>Notifications</c>' own concern.
/// </summary>
public sealed record BookingHoldExpired(
    Guid BookingId,
    Guid StudentUserId,
    Guid TutorUserId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    int DurationMinutes,
    bool IsTrial,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
}

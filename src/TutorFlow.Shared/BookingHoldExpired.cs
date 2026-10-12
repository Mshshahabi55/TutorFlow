namespace TutorFlow.Shared;

/// <summary>
/// Published by <c>Bookings</c> immediately after a <c>Held</c>/<c>PendingPayment</c> booking's
/// hold TTL has genuinely lapsed without the student completing checkout — never on an
/// already-terminal booking (Confirmed/Cancelled/Completed/NoShow/Refunded/Rescheduled are all
/// excluded by construction, see <c>BookingHoldExpirySweepService</c>) and never more than once
/// per booking (the conditional-UPDATE guard that triggers this publish only ever succeeds once
/// per row). Carries only what <c>Bookings</c>' own domain already knows — no presentation-
/// specific content (subject lines, HTML, CTA copy) belongs here; that is
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

namespace TutorFlow.Shared;

/// <summary>
/// Published by <c>Bookings</c> immediately after the <c>PendingPayment</c> -&gt; <c>Confirmed</c>
/// transition succeeds (ADR-0016 §1) — never on an idempotent duplicate-delivery no-op, and
/// never if the conditional update did not actually confirm the booking. Carries everything
/// <c>Lessons</c> needs to create a <c>Scheduled</c> <c>Lesson</c> (<c>docs/domain/STATE_MACHINES.md</c>)
/// without querying <c>Bookings</c>' schema (ADR-0016 §2). <c>SubscriptionReferenceId</c> is an
/// opaque external reference, passed through only so a future <c>Subscriptions</c>-driven
/// occurrence can be correlated later — it is not populated by anything in the current
/// implementation and is not stored by <c>Lessons</c>' first consumer slice.
///
/// Business fields match ADR-0016 §2 exactly, field-for-field. <c>EventId</c>/<c>OccurredAtUtc</c>
/// follow the mechanical convention every other real integration event in this codebase already
/// uses (self-generated <c>EventId</c>, <c>OccurredAtUtc</c> trailing) rather than the ADR's own
/// pre-implementation placeholder text, which predates every event that has actually been built —
/// see <c>PaymentSucceeded</c>/<c>TutorPublished</c>/<c>TutorUnpublished</c> for the established shape.
/// </summary>
public sealed record BookingConfirmed(
    Guid BookingId,
    Guid StudentUserId,
    Guid TutorUserId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    int DurationMinutes,
    Guid? SubscriptionReferenceId,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
}

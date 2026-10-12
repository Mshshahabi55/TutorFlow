namespace TutorFlow.Shared;

/// <summary>
/// Published by <c>Payments</c>, consumed by <c>Bookings</c> (ADR-0027, PAYMENTS_SPEC.md §16) —
/// the first real publisher+consumer pair for <c>Payments</c>, and the reason Wolverine is
/// introduced for it. <c>PaymentSucceeded</c> drives <c>Bookings</c>' existing, already-idempotent
/// <c>ConfirmBooking.Handler</c>; <c>PaymentFailed</c> drives a new, system-triggered
/// <c>PendingPayment -&gt; Cancelled</c> path (webhook-triggered, no user caller to attribute it to).
/// </summary>
public sealed record PaymentSucceeded(Guid PaymentReferenceId, Guid BookingId, DateTimeOffset OccurredAtUtc) : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
}

public sealed record PaymentFailed(Guid PaymentReferenceId, Guid BookingId, string? Reason, DateTimeOffset OccurredAtUtc) : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
}

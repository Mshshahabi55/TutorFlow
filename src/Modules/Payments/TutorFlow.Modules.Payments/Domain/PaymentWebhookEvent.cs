namespace TutorFlow.Modules.Payments.Domain;

// Append-only audit/idempotency log — one row per received Stripe webhook event, regardless of
// whether it changed a Payment row (PAYMENTS_SPEC.md §1.3). StripeEventId is DB-unique — the
// webhook-level dedup guard, in addition to (not instead of) Payment.Status's own conditional
// UPDATE guard.
public sealed class PaymentWebhookEvent
{
    private PaymentWebhookEvent()
    {
    }

    public Guid Id { get; private init; }

    public string StripeEventId { get; private init; } = string.Empty;

    public string EventType { get; private init; } = string.Empty;

    public Guid? PaymentId { get; private init; }

    public DateTimeOffset ReceivedAtUtc { get; private init; }

    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public string RawPayloadJson { get; private init; } = string.Empty;

    public static PaymentWebhookEvent Receive(string stripeEventId, string eventType, Guid? paymentId, string rawPayloadJson, TimeProvider clock) => new()
    {
        Id = Guid.NewGuid(),
        StripeEventId = stripeEventId,
        EventType = eventType,
        PaymentId = paymentId,
        ReceivedAtUtc = clock.GetUtcNow(),
        RawPayloadJson = rawPayloadJson
    };

    public void MarkProcessed(TimeProvider clock) => ProcessedAtUtc = clock.GetUtcNow();
}

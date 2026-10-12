namespace TutorFlow.Modules.Payments.Infrastructure;

// Isolates every Stripe SDK call behind one narrow, Payments-internal interface (ADR-0018 §4) —
// nothing about this crosses the module boundary; Bookings never sees it. Two implementations
// share it: a real Stripe-backed one and a deterministic test one (ADR-0027 §3) — selected by
// PaymentsModule based on whether a Stripe secret key is configured, never by product logic.
public interface IPaymentProviderClient
{
    Task<ProviderPaymentIntent> CreateOrRetrievePaymentIntentAsync(
        string idempotencyKey,
        int amountMinorUnits,
        string currency,
        CancellationToken ct);
}

public sealed record ProviderPaymentIntent(string ProviderPaymentId, string ClientSecret);

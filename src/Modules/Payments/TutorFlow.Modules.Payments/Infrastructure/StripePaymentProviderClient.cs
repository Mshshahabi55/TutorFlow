using Stripe;

namespace TutorFlow.Modules.Payments.Infrastructure;

// Real Stripe integration (ADR-0004, ADR-0018). A plain PaymentIntent on the platform's own
// account — no transfer_data/application_fee_amount (destination-charge routing to a tutor's
// Connect account is deliberately deferred, ADR-0027 §2: no connected-account onboarding exists
// yet, and the commission rate is still undecided, OPEN_QUESTIONS.md #34/A32). Stripe's own
// idempotency-key handling on Create means calling this again with the same key returns the
// same PaymentIntent (and a valid client_secret) rather than creating a duplicate — no manual
// get-or-create logic is needed.
internal sealed class StripePaymentProviderClient(string secretKey) : IPaymentProviderClient
{
    public async Task<ProviderPaymentIntent> CreateOrRetrievePaymentIntentAsync(
        string idempotencyKey,
        int amountMinorUnits,
        string currency,
        CancellationToken ct)
    {
        var client = new StripeClient(secretKey);
        var service = new PaymentIntentService(client);

        var intent = await service.CreateAsync(
            new PaymentIntentCreateOptions
            {
                Amount = amountMinorUnits,
                Currency = currency.ToLowerInvariant(),
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true }
            },
            new RequestOptions { IdempotencyKey = idempotencyKey },
            ct);

        return new ProviderPaymentIntent(intent.Id, intent.ClientSecret);
    }
}

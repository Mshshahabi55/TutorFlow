using System.Security.Cryptography;
using System.Text;

namespace TutorFlow.Modules.Payments.Infrastructure;

// Used only when no Stripe secret key is configured — local dev and the automated test suite
// (ADR-0027 §3). Deterministic (same idempotency key always yields the same fake IDs, mirroring
// Stripe's own real idempotency-key behavior) so the rest of the pipeline — the Payment row, its
// state machine, the webhook flow a test simulates directly — is exercised exactly as it would be
// against the real provider. This is swappable infrastructure behind IPaymentProviderClient, not
// a product-logic shortcut; nothing about Payment/the API contract differs by which is active.
internal sealed class TestPaymentProviderClient : IPaymentProviderClient
{
    public Task<ProviderPaymentIntent> CreateOrRetrievePaymentIntentAsync(
        string idempotencyKey,
        int amountMinorUnits,
        string currency,
        CancellationToken ct)
    {
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(idempotencyKey)))[..24];
        return Task.FromResult(new ProviderPaymentIntent($"pi_test_{hash}", $"pi_test_{hash}_secret_{hash}"));
    }
}

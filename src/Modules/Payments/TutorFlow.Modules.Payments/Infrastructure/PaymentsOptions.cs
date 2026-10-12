namespace TutorFlow.Modules.Payments.Infrastructure;

// Options-bound from configuration ("Payments:Stripe") — sourced from .NET user-secrets /
// environment variables in real environments, never appsettings.json (ADR-0027, matching the
// existing Jwt:Key convention). Left empty in local dev/test, which is the deliberate signal
// PaymentsModule uses to select the deterministic TestPaymentProviderClient instead of the real
// Stripe-backed one — never a product-logic branch, purely an infrastructure selection.
public sealed class PaymentsOptions
{
    public string? StripeSecretKey { get; set; }

    public string? StripeWebhookSigningSecret { get; set; }
}

namespace TutorFlow.Modules.Notifications.Infrastructure;

// Options-bound from configuration ("Notifications:Postmark...") — PostmarkServerToken is
// sourced from .NET user-secrets / environment variables in real environments, never
// appsettings.json (ADR-0029, matching the existing Jwt:Key/Payments:StripeSecretKey
// convention). Left empty in local dev/test, which is the deliberate signal
// NotificationsModule uses to select the deterministic TestEmailSender instead of the real
// Postmark-backed one — never a product-logic branch, purely an infrastructure selection.
public sealed class NotificationsOptions
{
    public string? PostmarkServerToken { get; set; }

    public string FromEmail { get; set; } = "notifications@tutorflow.app";

    public string FromName { get; set; } = "TutorFlow";

    /// <summary>Frontend origin used to build the email's CTA link — matches
    /// <c>apps/web</c>'s own <c>NEXT_PUBLIC_API_BASE_URL</c> localhost-fallback convention.
    /// Set via configuration in real environments, never hardcoded at the call site.</summary>
    public string AppBaseUrl { get; set; } = "http://localhost:3000";
}

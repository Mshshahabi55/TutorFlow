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

    /// <summary>How long a claim's ownership lease lasts before
    /// <c>AbandonedCheckoutClaimReconciliationService</c> considers it abandoned and eligible for
    /// takeover (ADR-0029 Addendum A). Set generously longer than realistic end-to-end processing
    /// time — this project's own reliability investigation directly measured a first-dispatch
    /// latency up to ~10s in a freshly-started host, so 10 minutes leaves a wide margin without
    /// meaningfully delaying real recovery.</summary>
    public int ClaimLeaseMinutes { get; set; } = 10;

    /// <summary>How often <see cref="AbandonedCheckoutClaimReconciliationService"/>
    /// scans for stale unresolved claims — same "intentional engineering decision, not a
    /// Preply-sourced timing" rationale as <c>BookingsOptions.HoldExpirySweepIntervalSeconds</c>.</summary>
    public int ClaimReconciliationIntervalSeconds { get; set; } = 60;

    /// <summary>Upper bound on how many stale claims a single reconciliation tick takes over —
    /// bounded work per tick rather than loading every unresolved claim into memory at once.</summary>
    public int ClaimReconciliationBatchSize { get; set; } = 25;
}

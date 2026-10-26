using TutorFlow.Shared;
using Wolverine.Attributes;
using SendAbandonedCheckoutRecoveryEmailFeature = TutorFlow.Modules.Notifications.Features.SendAbandonedCheckoutRecoveryEmail.SendAbandonedCheckoutRecoveryEmail;

namespace TutorFlow.Modules.Notifications.Consumers;

/// <summary>
/// Explicitly marked for Wolverine discovery (F6 — see Program.cs's
/// DisableConventionalDiscovery comment) — ADR-0029, the same shape as
/// <c>Search.TutorPublishedConsumer</c>. Thin delegation to the real feature handler, never
/// business logic of its own.
/// </summary>
/// <remarks>
/// A44: the real handler (<c>SendAbandonedCheckoutRecoveryEmail.Handler</c>) reaches three
/// DbContexts (<c>NotificationsDbContext</c> directly, plus <c>IdentityDbContext</c>/
/// <c>TutorsDbContext</c> transitively via read-only query interfaces), which is ambiguous
/// under <c>AutoApplyTransactions()</c> (Program.cs). <c>[NonTransactional]</c> is the correct
/// resolution, not a blanket workaround: the handler's own claim/resolve protocol (ADR-0029
/// Addendum A) deliberately commits its claim-insert and its later resolve-write as two
/// independent SaveChangesAsync calls — not one ambient transaction — specifically so a crash
/// between them leaves a durably-visible, reconciliation-recoverable claim row for another
/// worker's stale-takeover to find. Wrapping the whole handler (including the external
/// Postmark send in between) in one ambient transaction would hide the claim from every other
/// connection until the entire handler committed, breaking that cross-worker visibility
/// assumption outright. This handler was never one of A44's four target publisher modules —
/// opting it out of the new global policy preserves its current, already-idempotent behavior
/// unchanged.
/// </remarks>
[WolverineHandler]
[NonTransactional]
public sealed class BookingHoldExpiredConsumer(SendAbandonedCheckoutRecoveryEmailFeature.Handler handler)
{
    public Task HandleAsync(BookingHoldExpired message, CancellationToken ct) => handler.HandleAsync(message, ct);
}

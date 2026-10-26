using TutorFlow.Shared;
using Wolverine.Attributes;
using Wolverine.Persistence;
using SendCertificateRejectedEmailFeature = TutorFlow.Modules.Notifications.Features.SendCertificateRejectedEmail.SendCertificateRejectedEmail;

namespace TutorFlow.Modules.Notifications.Consumers;

/// <summary>Explicitly marked for Wolverine discovery (F6 — see Program.cs's
/// DisableConventionalDiscovery comment) — same shape as <see cref="BookingHoldExpiredConsumer"/>.
/// Thin delegation to the real feature handler, never business logic of its own.</summary>
/// <remarks>
/// A44: its only DbContext dependency, <c>IdentityDbContext</c> (via <c>IUserEmailQuery</c>), is
/// registered with <c>EnableRetryOnFailure()</c> (IdentityModule.cs) — incompatible with
/// <c>AutoApplyTransactions()</c>'s default <c>TransactionMiddlewareMode.Eager</c> (Program.cs),
/// which emits an explicit <c>BeginTransactionAsync()</c> that EF Core's retrying execution
/// strategy forbids (confirmed by Wolverine's own startup assertion). Overriding just this
/// handler to <c>Lightweight</c> mode — rather than changing the global default, which would
/// also affect the four A44 outbox modules' already-verified Eager-mode atomicity — fixes this
/// without touching anything outbox-related; <c>IdentityDbContext</c> has no registered message
/// store and was never part of A44's outbox scope. Tutors' own DbContext registration also uses
/// <c>EnableRetryOnFailure()</c> (TutorsModule.cs) and will need the identical per-handler
/// override once its own Wolverine handlers are wired in a future task.
/// </remarks>
[WolverineHandler]
[Transactional(Mode = TransactionMiddlewareMode.Lightweight)]
public sealed class TutorCertificateRejectedConsumer(SendCertificateRejectedEmailFeature.Handler handler)
{
    public Task HandleAsync(TutorCertificateRejected message, CancellationToken ct) => handler.HandleAsync(message, ct);
}

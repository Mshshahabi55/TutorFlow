using TutorFlow.Modules.Bookings.Features.CancelBookingForPaymentFailure;
using TutorFlow.Shared;
using Wolverine.Attributes;
using Wolverine.Persistence;

namespace TutorFlow.Modules.Bookings.Consumers;

/// <summary>
/// Explicitly marked for Wolverine discovery (F6 — see Program.cs's
/// DisableConventionalDiscovery comment) — drives the new, webhook-triggered cancellation path
/// (ADR-0027 §5, OPEN_QUESTIONS.md A33). Treated as terminal: V1 does not support in-place retry
/// with a different payment method on the same booking (ADR-0015 §2).
/// </summary>
/// <remarks>
/// A44 topology fix: Lightweight mode — not the global Eager default — because
/// <see cref="CancelBookingForPaymentFailure"/>'s <c>BookingsDbContext</c> commit (via
/// <c>ExecuteUpdateAsync</c>) only needs to be atomic with the Main store's envelope bookkeeping
/// if both are reachable on the same physical connection, which production's one-database-per-
/// module topology does not guarantee. Safe to weaken here specifically because this handler is
/// terminal — no outgoing message depends on this commit, and the conditional
/// <c>ExecuteUpdateAsync</c> + <c>AlreadyTerminal</c> fallback is already idempotent against
/// at-least-once redelivery on its own, independent of transaction mode.
/// </remarks>
[WolverineHandler]
[Transactional(Mode = TransactionMiddlewareMode.Lightweight)]
public sealed class PaymentFailedConsumer(CancelBookingForPaymentFailure.Handler cancelForFailure)
{
    public async Task HandleAsync(PaymentFailed message, CancellationToken ct)
    {
        await cancelForFailure.HandleAsync(message.BookingId, message.Reason, ct);
    }
}

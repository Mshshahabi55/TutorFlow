using TutorFlow.Modules.Bookings.Features.ConfirmBooking;
using TutorFlow.Shared;
using Wolverine.Attributes;
using Wolverine.Persistence;

namespace TutorFlow.Modules.Bookings.Consumers;

/// <summary>
/// Explicitly marked for Wolverine discovery (F6 — see Program.cs's
/// DisableConventionalDiscovery comment) — <c>Payments</c>' first real publisher, consumed here
/// for the first time (ADR-0027 §5). Drives <see cref="ConfirmBooking.Handler"/> — no change to
/// its existing shape, just a new caller.
/// </summary>
/// <remarks>
/// A44 topology fix: Lightweight mode — not the global Eager default — because
/// ConfirmBooking's BookingsDbContext commit only needs to be atomic with the Main store's
/// envelope bookkeeping if both are reachable on the same physical connection, which
/// production's one-database-per-module topology does not guarantee. Safe specifically because
/// ConfirmBooking.Handler's AlreadyConfirmed path now republishes BookingConfirmed using
/// authoritative persisted state (Booking.PaymentReferenceId) whenever a genuine redelivery of
/// the same payment event arrives after a crash between the business commit and the original
/// publish — see ConfirmBooking.cs's own remarks for the full analysis.
/// </remarks>
[WolverineHandler]
[Transactional(Mode = TransactionMiddlewareMode.Lightweight)]
public sealed class PaymentSucceededConsumer(ConfirmBooking.Handler confirmBooking)
{
    public async Task HandleAsync(PaymentSucceeded message, CancellationToken ct)
    {
        await confirmBooking.HandleAsync(message.BookingId, message.PaymentReferenceId.ToString(), ct);
    }
}

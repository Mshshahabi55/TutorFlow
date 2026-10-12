using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Persistence;

namespace TutorFlow.Modules.Bookings.Features.CancelBookingForPaymentFailure;

// ADR-0015 §2 / PAYMENTS_SPEC.md §16 / OPEN_QUESTIONS.md A33: a webhook-triggered
// PendingPayment -> Cancelled transition has no user caller to attribute it to — CancelBooking
// .Handler assumes one (ownership check, CancelledByUserId audit field), so this is a small,
// additive new handler, not a change to CancelBooking's existing shape. Treated as terminal —
// V1 does not support in-place retry with a different payment method on the same booking.
public static class CancelBookingForPaymentFailure
{
    public enum Outcome
    {
        Cancelled,
        AlreadyTerminal,
        NotFound
    }

    // Public (not the usual internal convention) — reached by PaymentFailedConsumer, a
    // Wolverine-discovered consumer whose dependencies' types must be constructible from
    // Wolverine's generated code (same reasoning as ConfirmBooking.Handler).
    public sealed class Handler(BookingsDbContext db, TimeProvider clock)
    {
        public async Task<Outcome> HandleAsync(Guid bookingId, string? reason, CancellationToken ct)
        {
            var now = clock.GetUtcNow();

            var affected = await db.Bookings
                .Where(b => b.Id == bookingId && b.Status == BookingStatus.PendingPayment)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.Status, BookingStatus.Cancelled)
                    .SetProperty(b => b.CancelledAtUtc, now)
                    .SetProperty(b => b.CancellationReason, reason ?? "Payment failed")
                    .SetProperty(b => b.HoldExpiresAtUtc, (DateTimeOffset?)null)
                    .SetProperty(b => b.UpdatedAtUtc, now), ct);

            if (affected == 1)
            {
                return Outcome.Cancelled;
            }

            var current = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookingId, ct);
            return current is null ? Outcome.NotFound : Outcome.AlreadyTerminal;
        }
    }
}

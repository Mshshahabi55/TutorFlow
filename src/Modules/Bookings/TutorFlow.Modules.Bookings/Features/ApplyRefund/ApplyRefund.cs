using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Persistence;

namespace TutorFlow.Modules.Bookings.Features.ApplyRefund;

// ADR-0015 §5: Cancelled -> Refunded is driven exclusively by a Payments webhook confirming a
// refund it processed, only after Bookings itself requested one — never a public endpoint, and
// never reachable directly from Confirmed. Not reachable end-to-end until Payments exists;
// exercised today only by tests simulating the webhook directly.
public static class ApplyRefund
{
    public enum Outcome
    {
        RefundedNow,
        AlreadyRefunded,
        NotFound,
        Conflict
    }

    internal sealed class Handler(BookingsDbContext db, TimeProvider clock)
    {
        public async Task<Outcome> HandleAsync(Guid bookingId, CancellationToken ct)
        {
            var now = clock.GetUtcNow();

            var affected = await db.Bookings
                .Where(b => b.Id == bookingId && b.Status == BookingStatus.Cancelled)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.Status, BookingStatus.Refunded)
                    .SetProperty(b => b.UpdatedAtUtc, now), ct);

            if (affected == 1)
            {
                return Outcome.RefundedNow;
            }

            var current = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookingId, ct);
            if (current is null)
            {
                return Outcome.NotFound;
            }

            return current.Status == BookingStatus.Refunded ? Outcome.AlreadyRefunded : Outcome.Conflict;
        }
    }
}

using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Persistence;

namespace TutorFlow.Modules.Bookings.Features.CancelBooking;

public static class CancelBooking
{
    public sealed record Command(string? Reason);

    public sealed record Response(Guid Id, BookingStatus Status, DateTimeOffset CancelledAtUtc, Guid CancelledByUserId);

    public enum Outcome
    {
        Ok,
        NotFound,
        Forbidden,
        Conflict
    }

    internal sealed class Handler(BookingsDbContext db, TimeProvider clock)
    {
        public async Task<(Outcome Outcome, Response? Response)> HandleAsync(Guid callerUserId, Guid bookingId, Command command, CancellationToken ct)
        {
            var booking = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookingId, ct);
            if (booking is null)
            {
                return (Outcome.NotFound, null);
            }

            if (booking.StudentUserId != callerUserId && booking.TutorUserId != callerUserId)
            {
                return (Outcome.Forbidden, null);
            }

            var now = clock.GetUtcNow();

            // Conditional UPDATE (ADR-0013 §3): not idempotent by design (architecture review,
            // 2026-10-04) — a retry against an already-terminal booking affects zero rows and
            // is reported as 409, not silently treated as success, unlike checkout/confirm.
            var affected = await db.Bookings
                .Where(b => b.Id == bookingId && Booking.ActiveStatuses.Contains(b.Status))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.Status, BookingStatus.Cancelled)
                    .SetProperty(b => b.CancelledAtUtc, now)
                    .SetProperty(b => b.CancelledByUserId, callerUserId)
                    .SetProperty(b => b.CancellationReason, command.Reason)
                    .SetProperty(b => b.HoldExpiresAtUtc, (DateTimeOffset?)null)
                    .SetProperty(b => b.UpdatedAtUtc, now), ct);

            if (affected == 0)
            {
                return (Outcome.Conflict, null);
            }

            return (Outcome.Ok, new Response(bookingId, BookingStatus.Cancelled, now, callerUserId));
        }
    }
}

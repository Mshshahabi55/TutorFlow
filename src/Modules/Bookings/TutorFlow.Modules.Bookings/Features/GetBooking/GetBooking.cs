using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Persistence;

namespace TutorFlow.Modules.Bookings.Features.GetBooking;

public static class GetBooking
{
    public sealed record Response(
        Guid Id,
        Guid StudentUserId,
        Guid TutorUserId,
        BookingStatus Status,
        int DurationMinutes,
        DateTimeOffset StartUtc,
        DateTimeOffset EndUtc,
        DateTimeOffset HeldAtUtc,
        DateTimeOffset? HoldExpiresAtUtc,
        DateTimeOffset? ConfirmedAtUtc,
        DateTimeOffset? CancelledAtUtc,
        Guid? CancelledByUserId,
        string? CancellationReason,
        int PriceAmountMinorUnits,
        string PriceCurrency,
        string? PaymentReferenceId,
        Guid? SubscriptionReferenceId,
        Guid? RescheduledFromBookingId,
        Guid? RescheduledToBookingId,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);

    public enum Outcome
    {
        Found,
        NotFound,
        Forbidden
    }

    internal sealed class Handler(BookingsDbContext db)
    {
        public async Task<(Outcome Outcome, Response? Response)> HandleAsync(Guid callerUserId, Guid bookingId, CancellationToken ct)
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

            return (Outcome.Found, Map(booking));
        }

        private static Response Map(Booking b) => new(
            b.Id,
            b.StudentUserId,
            b.TutorUserId,
            b.Status,
            b.DurationMinutes,
            b.StartUtc,
            b.EndUtc,
            b.HeldAtUtc,
            b.HoldExpiresAtUtc,
            b.ConfirmedAtUtc,
            b.CancelledAtUtc,
            b.CancelledByUserId,
            b.CancellationReason,
            b.PriceAmountMinorUnits,
            b.PriceCurrency,
            b.PaymentReferenceId,
            b.SubscriptionReferenceId,
            b.RescheduledFromBookingId,
            b.RescheduledToBookingId,
            b.CreatedAtUtc,
            b.UpdatedAtUtc);
    }
}

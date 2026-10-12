using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Bookings.Infrastructure;

// ADR-0019/ADR-0027: the fourth instance of ADR-0014's read-only query pattern, consumed via DI
// by Payments, implemented here. IsEligible requires PendingPayment specifically — not Held —
// so a booking only ever becomes checkout-eligible by first passing through Bookings' own
// checkout handler (the ordering, not a new auth scheme, is the security boundary). Payments
// uses AmountMinorUnits/Currency/TutorUserId from here, and only from here, to create the
// Stripe PaymentIntent — never a caller-supplied value.
public sealed class BookingCheckoutEligibilityQuery(BookingsDbContext db) : IBookingCheckoutEligibilityQuery
{
    public async Task<BookingCheckoutEligibility> ValidateAsync(Guid bookingId, Guid callerStudentUserId, CancellationToken ct)
    {
        var booking = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookingId, ct);
        if (booking is null)
        {
            return new BookingCheckoutEligibility(false, "Booking not found.", null, null, null);
        }

        if (booking.StudentUserId != callerStudentUserId)
        {
            return new BookingCheckoutEligibility(false, "This booking does not belong to the caller.", null, null, null);
        }

        if (booking.Status != BookingStatus.PendingPayment)
        {
            return new BookingCheckoutEligibility(false, $"Booking is not in a payable state (status: {booking.Status}).", null, null, null);
        }

        return new BookingCheckoutEligibility(true, null, booking.PriceAmountMinorUnits, booking.PriceCurrency, booking.TutorUserId);
    }
}

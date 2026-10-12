using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Infrastructure;
using TutorFlow.Modules.Bookings.Persistence;

namespace TutorFlow.Modules.Bookings.Features.CheckoutBooking;

// ADR-0019/ADR-0027: the real checkout mechanism, no longer a 501 stub. Ordering is the
// security boundary (ADR-0019) — the booking transitions Held -> PendingPayment *before* the
// call to Payments, never after, so Payments' own eligibility check (requiring PendingPayment
// specifically) can never be satisfied by anything other than this handler having run first.
public static class CheckoutBooking
{
    public sealed record Response(Guid PaymentReferenceId, string ClientSecret);

    public enum Outcome
    {
        Ok,
        PaymentsUnavailable,
        NotFound,
        Forbidden,
        Conflict
    }

    internal sealed class Handler(BookingsDbContext db, TimeProvider clock, PaymentsApiClient paymentsApiClient)
    {
        public async Task<(Outcome Outcome, Response? Response)> HandleAsync(Guid callerUserId, Guid bookingId, CancellationToken ct)
        {
            var booking = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookingId, ct);
            if (booking is null)
            {
                return (Outcome.NotFound, null);
            }

            if (booking.StudentUserId != callerUserId)
            {
                return (Outcome.Forbidden, null);
            }

            if (booking.Status != BookingStatus.PendingPayment && booking.Status != BookingStatus.Held)
            {
                return (Outcome.Conflict, null);
            }

            if (booking.Status == BookingStatus.Held)
            {
                var now = clock.GetUtcNow();

                // Re-checks HoldExpiresAtUtc > now in the same conditional update (ADR-0013 §3,
                // BOOKINGS_SPEC.md §4 endpoint 5) — the lazy sweep only runs when a *new* hold is
                // requested for this tutor, so a stale Held row can still be sitting in the table,
                // unswept, past its TTL; without this clause checkout would happily flip it to
                // PendingPayment and create a real PaymentIntent for an expired hold.
                var affected = await db.Bookings
                    .Where(b => b.Id == bookingId && b.Status == BookingStatus.Held && b.HoldExpiresAtUtc > now)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(b => b.Status, BookingStatus.PendingPayment)
                        .SetProperty(b => b.UpdatedAtUtc, now), ct);

                if (affected == 0)
                {
                    // Lost a race (hold expired, or another request already moved it) between
                    // the read above and this conditional update — re-read for an accurate
                    // outcome rather than assuming success.
                    var current = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookingId, ct);
                    if (current?.Status != BookingStatus.PendingPayment)
                    {
                        return (Outcome.Conflict, null);
                    }
                }
            }

            // Called on every attempt, including retries — never short-circuited. Payments'
            // own client_secret is never persisted (ADR-0018 §2), so a retry has nothing cached
            // to fall back to; safety against a duplicate Stripe PaymentIntent comes from the
            // idempotency key on Payments' side, not from skipping this call.
            var result = await paymentsApiClient.CreatePaymentIntentAsync(bookingId, ct);
            if (!result.IsSuccess)
            {
                return (Outcome.PaymentsUnavailable, null);
            }

            await db.Bookings
                .Where(b => b.Id == bookingId)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.PaymentReferenceId, result.PaymentReferenceId!.Value.ToString()), ct);

            return (Outcome.Ok, new Response(result.PaymentReferenceId!.Value, result.ClientSecret!));
        }
    }
}

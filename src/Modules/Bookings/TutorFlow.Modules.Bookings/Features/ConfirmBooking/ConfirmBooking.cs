using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Persistence;
using TutorFlow.Shared;
using Wolverine;

namespace TutorFlow.Modules.Bookings.Features.ConfirmBooking;

// ADR-0015 §4: the PendingPayment -> Confirmed transition is webhook-driven by Payments, never
// a public HTTP endpoint (BOOKINGS_SPEC.md §4: "No payment-confirmation HTTP endpoint"). Called
// by PaymentSucceededConsumer (ADR-0027 §5) — public, not the usual internal convention, because
// Wolverine's codegen generates direct construction code for a consumer's dependencies, and an
// internal concrete type can't be referenced from that generated code (same reason
// ActiveBookingIntervalsQuery/GetTutorAvailabilityPreview.Handler were made public earlier).
// Any transient exception from ExecuteUpdateAsync propagates uncaught — the webhook endpoint
// wrapping this (via the consumer) surfaces that as a retryable HTTP error (ADR-0015 §4), not
// swallowing it.
public static class ConfirmBooking
{
    public enum Outcome
    {
        ConfirmedNow,
        AlreadyConfirmed,
        NotFound,
        Conflict
    }

    public sealed class Handler(BookingsDbContext db, TimeProvider clock, IMessageBus bus)
    {
        public async Task<Outcome> HandleAsync(Guid bookingId, string paymentReferenceId, CancellationToken ct)
        {
            var now = clock.GetUtcNow();

            // Read before the conditional update purely to source BookingConfirmed's payload —
            // every field read here (StudentUserId/TutorUserId/StartUtc/EndUtc/DurationMinutes/
            // SubscriptionReferenceId) is write-once at hold creation and never changes on this
            // transition, so reading it a moment before the atomic update is race-free. The
            // update's own affected-row-count, not this read, remains the sole source of truth
            // for whether a confirmation actually happened.
            var booking = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookingId, ct);
            if (booking is null)
            {
                return Outcome.NotFound;
            }

            var affected = await db.Bookings
                .Where(b => b.Id == bookingId && b.Status == BookingStatus.PendingPayment)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.Status, BookingStatus.Confirmed)
                    .SetProperty(b => b.ConfirmedAtUtc, now)
                    .SetProperty(b => b.PaymentReferenceId, paymentReferenceId)
                    .SetProperty(b => b.HoldExpiresAtUtc, (DateTimeOffset?)null)
                    .SetProperty(b => b.UpdatedAtUtc, now), ct);

            if (affected == 1)
            {
                // ADR-0016 §1: published only after the transition genuinely happened, never on
                // the idempotent AlreadyConfirmed/Conflict paths below.
                await bus.PublishAsync(new BookingConfirmed(
                    booking.Id,
                    booking.StudentUserId,
                    booking.TutorUserId,
                    booking.StartUtc,
                    booking.EndUtc,
                    booking.DurationMinutes,
                    booking.SubscriptionReferenceId,
                    now));

                return Outcome.ConfirmedNow;
            }

            var current = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookingId, ct);
            if (current is null)
            {
                return Outcome.NotFound;
            }

            // Duplicate webhook delivery for an already-confirmed booking is idempotent
            // success, not re-processed (ADR-0015 §4) — and, per ADR-0016 §1, not re-published.
            return current.Status == BookingStatus.Confirmed ? Outcome.AlreadyConfirmed : Outcome.Conflict;
        }
    }
}

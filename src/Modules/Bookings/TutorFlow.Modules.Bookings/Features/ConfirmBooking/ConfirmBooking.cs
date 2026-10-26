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
        Conflict,

        // A44 replay-safety fix: distinct from Conflict (which covers every other non-Confirmed,
        // non-PendingPayment state) so a genuinely different/conflicting payment reference
        // confirming an already-Confirmed booking is never silently conflated with the expected,
        // benign "duplicate delivery of the SAME payment" case (AlreadyConfirmed).
        ConfirmedByDifferentPayment
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
                // ADR-0016 §1: published after the transition genuinely happens here. The
                // AlreadyConfirmed path below also republishes, but only as replay-recovery for
                // the SAME payment reference (see its own comment) — Conflict/NotFound never
                // publish.
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

            if (current.Status != BookingStatus.Confirmed)
            {
                return Outcome.Conflict;
            }

            // A44 replay-safety fix: Lightweight transaction mode (unlike Eager) does not
            // atomically couple this row's commit with BookingConfirmed's durable persistence —
            // there is a narrow window where the booking transitions to Confirmed but the
            // process crashes before the outgoing envelope is durably recorded, in which case
            // the message is never sent and this same PaymentSucceeded envelope is redelivered
            // (it was never marked handled, since the handler never returned). Republishing here
            // recovers that case. Guarded by PaymentReferenceId so only a genuine redelivery of
            // the SAME payment event triggers recovery — using Booking's own already-persisted,
            // authoritative PaymentReferenceId (set at the original confirmation), not an
            // invented identity or an inference from status alone. A different payment reference
            // confirming an already-Confirmed booking is a conflicting event, not a replay, and
            // must never be treated as idempotent success.
            if (current.PaymentReferenceId != paymentReferenceId)
            {
                return Outcome.ConfirmedByDifferentPayment;
            }

            await bus.PublishAsync(new BookingConfirmed(
                current.Id,
                current.StudentUserId,
                current.TutorUserId,
                current.StartUtc,
                current.EndUtc,
                current.DurationMinutes,
                current.SubscriptionReferenceId,
                current.ConfirmedAtUtc ?? now));

            return Outcome.AlreadyConfirmed;
        }
    }
}

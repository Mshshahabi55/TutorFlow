using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Infrastructure;
using TutorFlow.Modules.Bookings.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Bookings.Features.RescheduleBooking;

// ADR-0017 "Mechanics": one transaction, flip-first ordering. The original's conditional
// transition to Rescheduled is attempted before anything else; if it loses a concurrent race
// (affects zero rows), the handler aborts without ever creating the replacement booking — no
// orphan can result either from a crash between the two writes or from two concurrent
// reschedule requests against the same original.
public static class RescheduleBooking
{
    public sealed record Command(DateTimeOffset NewStartUtc);

    public sealed record Response(Guid Id, BookingStatus Status, DateTimeOffset StartUtc, DateTimeOffset EndUtc, Guid RescheduledFromBookingId);

    public enum Outcome
    {
        Ok,
        NotFound,
        Forbidden,
        Conflict,
        InvalidSlot,
        TooLate
    }

    internal sealed class Handler(
        BookingsDbContext db,
        TimeProvider clock,
        IOptions<BookingsOptions> options,
        ITutorAvailabilityQuery availabilityQuery,
        ITutorPricingQuery pricingQuery)
    {
        public async Task<(Outcome Outcome, Response? Response, string? Reason)> HandleAsync(Guid callerUserId, Guid originalId, Command command, CancellationToken ct)
        {
            var original = await db.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == originalId, ct);
            if (original is null)
            {
                return (Outcome.NotFound, null, null);
            }

            if (original.StudentUserId != callerUserId)
            {
                return (Outcome.Forbidden, null, null);
            }

            // Allowed only from Confirmed — rescheduling a mere Held/PendingPayment hold is just
            // "cancel and rebook," not a distinct operation (BOOKINGS_SPEC.md §4 endpoint 7).
            // This is only an advisory pre-check; the authoritative guard is the conditional
            // UPDATE below, which re-checks the same condition atomically.
            if (original.Status != BookingStatus.Confirmed)
            {
                return (Outcome.Conflict, null, null);
            }

            // Resolved — Product Owner decision, 2026-10-04 (ADR-0017): at most one reschedule
            // per booking — rejected if the original is itself already the product of an earlier
            // reschedule.
            if (original.RescheduledFromBookingId is not null)
            {
                return (Outcome.Conflict, null, null);
            }

            // Resolved — Product Owner decision, 2026-10-04 (ADR-0017): the request must be made
            // >=12 hours before the original's StartUtc; inside that window the cancellation
            // policy applies instead (which at <12h notice is also <24h, so always "no refund").
            // The client surfaces this as "cannot reschedule; cancelling now is not refundable,"
            // never silently converted into a cancellation here.
            var now = clock.GetUtcNow();
            if (original.StartUtc - now < TimeSpan.FromHours(12))
            {
                return (Outcome.TooLate, null, "Too close to the lesson start time to reschedule. You can still cancel, but it won't be refunded this close to the start time.");
            }

            var newEndUtc = command.NewStartUtc.AddMinutes(original.DurationMinutes);

            // The new slot goes through the exact same validation as a fresh hold — no
            // shortcut. Advisory-but-required: can be stale by the time the transaction below
            // runs; the exclusion constraint is still the final word.
            var validation = await availabilityQuery.ValidateSlotAsync(original.TutorUserId, command.NewStartUtc, newEndUtc, ct);
            if (!validation.IsValid)
            {
                return (Outcome.InvalidSlot, null, validation.Reason ?? "Requested slot is not available.");
            }

            var price = await pricingQuery.GetCurrentPriceAsync(original.TutorUserId, original.DurationMinutes, ct);
            if (price is null)
            {
                return (Outcome.InvalidSlot, null, "Tutor pricing is not configured.");
            }

            var holdExpiresAtUtc = now.AddMinutes(options.Value.HoldDurationMinutes);

            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            // Steps 1-2: conditionally transition the original, verify exactly one row changed.
            var flipped = await db.Bookings
                .Where(b => b.Id == originalId && b.Status == BookingStatus.Confirmed)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.Status, BookingStatus.Rescheduled)
                    .SetProperty(b => b.UpdatedAtUtc, now), ct);

            if (flipped == 0)
            {
                // Lost the race (already rescheduled/cancelled by a concurrent request) —
                // abort before creating anything. No orphan is possible.
                await transaction.RollbackAsync(ct);
                return (Outcome.Conflict, null, null);
            }

            // Step 3: create the replacement, through the identical sweep-then-insert path as
            // any fresh hold (ADR-0013 §2-3), still inside this same transaction.
            await BookingWrites.ExpireStaleHoldsAsync(db, original.TutorUserId, now, ct);

            var createResult = Booking.CreateHeld(
                original.StudentUserId,
                original.TutorUserId,
                original.DurationMinutes,
                command.NewStartUtc,
                holdExpiresAtUtc,
                new Money(price.AmountMinorUnits, price.Currency),
                clock,
                isTrial: original.IsTrial,
                rescheduledFromBookingId: originalId);

            if (createResult.IsFailure)
            {
                await transaction.RollbackAsync(ct);
                return (Outcome.InvalidSlot, null, createResult.Errors.FirstOrDefault());
            }

            var replacement = createResult.Value;
            db.Bookings.Add(replacement);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (BookingWrites.IsExclusionViolation(ex))
            {
                // The transaction is now aborted by Postgres; disposing it via `await using`
                // rolls it back — an explicit RollbackAsync here is unnecessary and, per the
                // identical precedent in Availability's ReplaceWeeklyRules, can throw on an
                // already-aborted transaction.
                throw new SlotNotAvailableException("The requested new slot is no longer available.");
            }

            await db.Bookings
                .Where(b => b.Id == originalId)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.RescheduledToBookingId, replacement.Id), ct);

            // Step 4: commit both operations atomically.
            await transaction.CommitAsync(ct);

            return (Outcome.Ok, new Response(replacement.Id, replacement.Status, replacement.StartUtc, replacement.EndUtc, originalId), null);
        }
    }
}

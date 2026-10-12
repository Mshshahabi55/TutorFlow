using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Infrastructure;
using TutorFlow.Modules.Bookings.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Bookings.Features.CreateTrialHold;

public static class CreateTrialHold
{
    public sealed record Command(Guid TutorUserId, int DurationMinutes, DateTimeOffset StartUtc);

    public sealed record Response(
        Guid Id,
        BookingStatus Status,
        DateTimeOffset StartUtc,
        DateTimeOffset EndUtc,
        DateTimeOffset? HoldExpiresAtUtc,
        int PriceAmountMinorUnits,
        string PriceCurrency);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator(TimeProvider clock)
        {
            RuleFor(x => x.TutorUserId).NotEmpty();
            RuleFor(x => x.DurationMinutes).GreaterThan(0);
            RuleFor(x => x.StartUtc).GreaterThan(_ => clock.GetUtcNow()).WithMessage("Start time must be in the future.");
        }
    }

    internal sealed class Handler(
        BookingsDbContext db,
        TimeProvider clock,
        IOptions<BookingsOptions> options,
        ITutorAvailabilityQuery availabilityQuery,
        ITutorPricingQuery pricingQuery)
    {
        public async Task<Result<Response>> HandleAsync(Guid studentUserId, Command command, CancellationToken ct)
        {
            if (command.TutorUserId == studentUserId)
            {
                return Result.Failure<Response>("A tutor cannot book themselves.");
            }

            if (!options.Value.AllowedTrialDurationsMinutes.Contains(command.DurationMinutes))
            {
                return Result.Failure<Response>("Requested duration is not one of the currently allowed trial durations.");
            }

            // One trial per tutor (BOOKINGS_SPEC.md, ADR-0027 Preply research) — GetTrialEligibility
            // only ever advises the frontend CTA; this is the actual server-side enforcement gate,
            // since the create endpoint must never trust the client to have checked first.
            var hasUsedTrial = await db.Bookings
                .AsNoTracking()
                .AnyAsync(b =>
                    b.StudentUserId == studentUserId &&
                    b.TutorUserId == command.TutorUserId &&
                    b.IsTrial &&
                    Booking.UsedTrialStatuses.Contains(b.Status),
                    ct);
            if (hasUsedTrial)
            {
                return Result.Failure<Response>("You've already used your trial lesson with this tutor.");
            }

            var now = clock.GetUtcNow();

            // Resolved — Product Owner decision, 2026-10-04 (BOOKINGS_SPEC.md §4 endpoint 1): caps
            // a student's global concurrent Held/PendingPayment count at 3 to close the inventory-
            // squatting abuse vector (docs/OPEN_QUESTIONS.md #32). Global, not per-tutor; an older
            // hold is never auto-expired to make room. Filtered by HoldExpiresAtUtc, not Status
            // alone — the lazy sweep only runs scoped to the tutor being requested, so a stale
            // hold with a *different* tutor may not have flipped to Expired in the database yet;
            // counting it anyway would wrongly reject a student whose hold has already lapsed.
            var activeHoldCount = await db.Bookings
                .AsNoTracking()
                .CountAsync(b =>
                    b.StudentUserId == studentUserId &&
                    (b.Status == BookingStatus.Held || b.Status == BookingStatus.PendingPayment) &&
                    b.HoldExpiresAtUtc > now,
                    ct);
            if (activeHoldCount >= 3)
            {
                return Result.Failure<Response>("You already have the maximum number of active holds. Complete or cancel one before creating another.");
            }

            var endUtc = command.StartUtc.AddMinutes(command.DurationMinutes);

            var validation = await availabilityQuery.ValidateSlotAsync(command.TutorUserId, command.StartUtc, endUtc, ct);
            if (!validation.IsValid)
            {
                return Result.Failure<Response>(validation.Reason ?? "Requested slot is not available.");
            }

            var price = await pricingQuery.GetCurrentPriceAsync(command.TutorUserId, command.DurationMinutes, ct);
            if (price is null)
            {
                return Result.Failure<Response>("Tutor pricing is not configured.");
            }

            var holdExpiresAtUtc = now.AddMinutes(options.Value.HoldDurationMinutes);

            var createResult = Booking.CreateHeld(
                studentUserId,
                command.TutorUserId,
                command.DurationMinutes,
                command.StartUtc,
                holdExpiresAtUtc,
                new Money(price.AmountMinorUnits, price.Currency),
                clock,
                isTrial: true);

            if (createResult.IsFailure)
            {
                return Result.Failure<Response>([.. createResult.Errors]);
            }

            var booking = createResult.Value;

            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            await BookingWrites.ExpireStaleHoldsAsync(db, command.TutorUserId, now, ct);

            db.Bookings.Add(booking);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (BookingWrites.IsExclusionViolation(ex))
            {
                throw new SlotNotAvailableException("The requested slot is no longer available.");
            }

            await transaction.CommitAsync(ct);

            return Result.Success(Map(booking));
        }

        private static Response Map(Booking booking) => new(
            booking.Id,
            booking.Status,
            booking.StartUtc,
            booking.EndUtc,
            booking.HoldExpiresAtUtc,
            booking.PriceAmountMinorUnits,
            booking.PriceCurrency);
    }
}

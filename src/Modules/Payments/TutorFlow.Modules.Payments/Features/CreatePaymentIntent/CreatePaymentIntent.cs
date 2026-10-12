using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TutorFlow.Modules.Payments.Domain;
using TutorFlow.Modules.Payments.Infrastructure;
using TutorFlow.Modules.Payments.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Payments.Features.CreatePaymentIntent;

// ADR-0019/ADR-0027: the only caller is Bookings' own checkout handler (server-to-server), on
// every checkout attempt including retries — never short-circuited, because the client_secret
// is never persisted (ADR-0018 §2) and a retry needs a fresh one. Safety against a duplicate
// Stripe PaymentIntent comes from the idempotency key, not from skipping this call.
public static class CreatePaymentIntent
{
    public sealed record Command(Guid BookingId);

    public sealed record Response(Guid PaymentReferenceId, string ClientSecret);

    public enum Outcome
    {
        Created,
        Ineligible
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.BookingId).NotEmpty();
        }
    }

    internal sealed class Handler(
        PaymentsDbContext db,
        TimeProvider clock,
        IBookingCheckoutEligibilityQuery eligibilityQuery,
        IPaymentProviderClient providerClient)
    {
        public async Task<(Outcome Outcome, Response? Response, string? Reason)> HandleAsync(Guid studentUserId, Command command, CancellationToken ct)
        {
            // Payments never trusts a caller-supplied amount/currency — only this read-only
            // query, owned by Bookings, is authoritative (ADR-0019).
            var eligibility = await eligibilityQuery.ValidateAsync(command.BookingId, studentUserId, ct);
            if (!eligibility.IsEligible)
            {
                return (Outcome.Ineligible, null, eligibility.Reason ?? "This booking is not eligible for checkout.");
            }

            var idempotencyKey = $"create-intent:{command.BookingId}";
            var existing = await db.Payments.FirstOrDefaultAsync(p => p.BookingId == command.BookingId, ct);

            if (existing is not null)
            {
                // A retry (frontend reloaded before completing payment) — ask the provider again
                // with the same stored key; Stripe (and the deterministic test adapter) return
                // the same PaymentIntent and a fresh, usable client_secret every time.
                var retryIntent = await providerClient.CreateOrRetrievePaymentIntentAsync(
                    existing.IdempotencyKey, existing.AmountMinorUnits, existing.Currency, ct);
                return (Outcome.Created, new Response(existing.Id, retryIntent.ClientSecret), null);
            }

            var providerIntent = await providerClient.CreateOrRetrievePaymentIntentAsync(
                idempotencyKey, eligibility.AmountMinorUnits!.Value, eligibility.Currency!, ct);

            var payment = Payment.Create(
                command.BookingId,
                studentUserId,
                eligibility.TutorUserId!.Value,
                new Money(eligibility.AmountMinorUnits.Value, eligibility.Currency!),
                idempotencyKey,
                providerIntent.ProviderPaymentId,
                clock);

            db.Payments.Add(payment);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Two concurrent checkout attempts for the same booking can both pass the
                // existing-row check above; the DB's UNIQUE(BookingId) index is the real guard.
                // The loser re-reads the winner's row and asks the provider again with its key.
                var winner = await db.Payments.AsNoTracking().FirstAsync(p => p.BookingId == command.BookingId, ct);
                var sharedIntent = await providerClient.CreateOrRetrievePaymentIntentAsync(
                    winner.IdempotencyKey, winner.AmountMinorUnits, winner.Currency, ct);
                return (Outcome.Created, new Response(winner.Id, sharedIntent.ClientSecret), null);
            }

            return (Outcome.Created, new Response(payment.Id, providerIntent.ClientSecret), null);
        }
    }
}

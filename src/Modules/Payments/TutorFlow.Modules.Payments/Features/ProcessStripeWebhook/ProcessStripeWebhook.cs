using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Payments.Domain;
using TutorFlow.Modules.Payments.Persistence;
using TutorFlow.Shared;
using Wolverine;

namespace TutorFlow.Modules.Payments.Features.ProcessStripeWebhook;

// ADR-0018 §3 / ADR-0027 §4: signature verification happens in the HTTP endpoint, before this
// handler is ever reached — this handler is the part that is directly, deterministically
// testable (mirroring Bookings.ConfirmBooking's own "only reachable by tests simulating the
// notification directly" precedent), never exposed as a public "mark as paid" endpoint itself.
public static class ProcessStripeWebhook
{
    public enum Outcome
    {
        Succeeded,
        Failed,
        DuplicateEvent,
        PaymentNotFound,
        Conflict,
        Unrecognized
    }

    internal sealed class Handler(PaymentsDbContext db, TimeProvider clock, IMessageBus bus)
    {
        public async Task<Outcome> ProcessEventAsync(
            string stripeEventId,
            string eventType,
            string? providerPaymentId,
            string? failureReason,
            string rawPayloadJson,
            CancellationToken ct)
        {
            // Event-level idempotency (ADR-0018 §3) — in addition to, not instead of, the
            // Payment-row conditional UPDATE below. Checked, then the receipt is recorded,
            // inside the same unit of work as whatever the event actually does.
            if (await db.PaymentWebhookEvents.AnyAsync(e => e.StripeEventId == stripeEventId, ct))
            {
                return Outcome.DuplicateEvent;
            }

            var payment = providerPaymentId is null
                ? null
                : await db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.StripePaymentIntentId == providerPaymentId, ct);

            db.PaymentWebhookEvents.Add(PaymentWebhookEvent.Receive(stripeEventId, eventType, payment?.Id, rawPayloadJson, clock));

            switch (eventType)
            {
                case "payment_intent.succeeded":
                    return await HandleSucceededAsync(payment, ct);
                case "payment_intent.payment_failed":
                    return await HandleFailedAsync(payment, failureReason, ct);
                default:
                    // Unrecognized event types are logged (the row added above) and
                    // acknowledged, not errored (ADR-0018 §3).
                    await db.SaveChangesAsync(ct);
                    return Outcome.Unrecognized;
            }
        }

        private async Task<Outcome> HandleSucceededAsync(Payment? payment, CancellationToken ct)
        {
            if (payment is null)
            {
                await db.SaveChangesAsync(ct);
                return Outcome.PaymentNotFound;
            }

            var now = clock.GetUtcNow();
            var affected = await db.Payments
                .Where(p => p.Id == payment.Id && (p.Status == PaymentStatus.Created || p.Status == PaymentStatus.RequiresAction || p.Status == PaymentStatus.Processing))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Status, PaymentStatus.Succeeded)
                    .SetProperty(p => p.ConfirmedAtUtc, now), ct);

            await db.SaveChangesAsync(ct);

            if (affected == 0)
            {
                // Out-of-order/duplicate delivery against an already-terminal Payment — the
                // webhook event itself is still recorded above; not re-processed, not erroring.
                return Outcome.Conflict;
            }

            await bus.PublishAsync(new PaymentSucceeded(payment.Id, payment.BookingId, now));
            return Outcome.Succeeded;
        }

        private async Task<Outcome> HandleFailedAsync(Payment? payment, string? failureReason, CancellationToken ct)
        {
            if (payment is null)
            {
                await db.SaveChangesAsync(ct);
                return Outcome.PaymentNotFound;
            }

            var now = clock.GetUtcNow();
            var affected = await db.Payments
                .Where(p => p.Id == payment.Id && (p.Status == PaymentStatus.Created || p.Status == PaymentStatus.RequiresAction || p.Status == PaymentStatus.Processing))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Status, PaymentStatus.Failed)
                    .SetProperty(p => p.FailedAtUtc, now)
                    .SetProperty(p => p.FailureReason, failureReason), ct);

            await db.SaveChangesAsync(ct);

            if (affected == 0)
            {
                return Outcome.Conflict;
            }

            await bus.PublishAsync(new PaymentFailed(payment.Id, payment.BookingId, failureReason, now));
            return Outcome.Failed;
        }
    }
}

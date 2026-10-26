using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Payments.Domain;
using TutorFlow.Modules.Payments.Persistence;
using TutorFlow.Shared;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.Persistence;

namespace TutorFlow.Modules.Payments.Features.ProcessStripeWebhook;

// ADR-0018 §3 / ADR-0027 §4: signature verification happens in the HTTP endpoint, before this
// handler is ever reached — this handler is the part that is directly, deterministically
// testable (mirroring Bookings.ConfirmBooking's own "only reachable by tests simulating the
// notification directly" precedent), never exposed as a public "mark as paid" endpoint itself.
//
// A44 #103: the HTTP endpoint dispatches Command via IMessageBus.InvokeAsync
// (ProcessStripeWebhookConsumer below) instead of calling Handler directly. Unlike
// PublishApplication/SuspendApplication/RejectCertificate, Handler's own SaveChangesAsync/
// ExecuteUpdateAsync calls did not need to change at all: PaymentsDbContext has no
// EnableRetryOnFailure(), so Eager mode's explicit transaction wraps every one of them exactly
// as written, genuinely atomically, with the outgoing envelope — no deferred-save restructuring,
// no idempotent-republish branch needed (the existing DuplicateEvent check already handles a
// Stripe-redelivered event correctly once true atomicity means nothing can ever half-commit).
// Called directly (bypassing Wolverine) still works for isolated unit tests of Handler's own
// business logic — it just does not get the atomic wrapping outside that chain.
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

    public sealed record Command(string StripeEventId, string EventType, string? ProviderPaymentId, string? FailureReason, string RawPayloadJson);

    public sealed class Handler(PaymentsDbContext db, TimeProvider clock, IMessageBus bus)
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

/// <summary>
/// A44 #103: local-command dispatch target — see <c>ProcessStripeWebhook</c>'s own remarks.
/// Eager (not Lightweight) mode is both viable and correct here: <c>PaymentsDbContext</c> has no
/// <c>EnableRetryOnFailure()</c> registration (unlike <c>TutorsDbContext</c>), so Eager's explicit
/// transaction can wrap the handler's <c>ExecuteUpdateAsync</c> calls without conflict, giving
/// genuine same-transaction atomicity for the business write and the outgoing envelope.
/// </summary>
[WolverineHandler]
[Transactional(typeof(PaymentsDbContext), Mode = TransactionMiddlewareMode.Eager)]
public sealed class ProcessStripeWebhookConsumer(ProcessStripeWebhook.Handler handler)
{
    public Task<ProcessStripeWebhook.Outcome> HandleAsync(ProcessStripeWebhook.Command command, CancellationToken ct) =>
        handler.ProcessEventAsync(command.StripeEventId, command.EventType, command.ProviderPaymentId, command.FailureReason, command.RawPayloadJson, ct);
}

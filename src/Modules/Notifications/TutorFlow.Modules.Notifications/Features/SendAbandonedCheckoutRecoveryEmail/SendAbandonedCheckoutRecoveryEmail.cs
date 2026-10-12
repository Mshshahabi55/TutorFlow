using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TutorFlow.Modules.Notifications.Domain;
using TutorFlow.Modules.Notifications.Infrastructure;
using TutorFlow.Modules.Notifications.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Notifications.Features.SendAbandonedCheckoutRecoveryEmail;

/// <summary>
/// Reacts to <see cref="BookingHoldExpired"/> (ADR-0029) — the TutorFlow equivalent of the
/// OBSERVED Preply "abandoned slot" recovery email (`18-screenshot-evidence.md` SS51–SS52).
/// Replicates the observed information architecture (tutor identity, slot date/time, a CTA back
/// to the tutor) with original TutorFlow copy — never Preply's proprietary wording. Idempotent
/// via <see cref="SentAbandonedCheckoutEmail"/>'s DB-unique <c>BookingId</c> index, not an
/// in-memory guard: a redelivered event, a retried handler, or a process restart all land on the
/// same "already claimed" check below.
/// </summary>
public static class SendAbandonedCheckoutRecoveryEmail
{
    // Public (not the usual internal convention) — consumed by the public
    // BookingHoldExpiredConsumer, whose own constructor parameter must be at least as
    // accessible as the consumer itself (matching Bookings.ConfirmBooking.Handler's identical
    // precedent).
    public sealed class Handler(
        NotificationsDbContext db,
        TimeProvider clock,
        IEmailSender emailSender,
        IUserEmailQuery userEmailQuery,
        ITutorProfileSummaryQuery tutorProfileQuery,
        IOptions<NotificationsOptions> options,
        ILogger<Handler> logger)
    {
        public async Task HandleAsync(BookingHoldExpired message, CancellationToken ct)
        {
            // Fast-path idempotency check — the unique index below is the real guarantee; this
            // avoids redundant provider/cross-module lookups on the common redelivery case.
            if (await db.SentAbandonedCheckoutEmails.AsNoTracking().AnyAsync(e => e.BookingId == message.BookingId, ct))
            {
                return;
            }

            var claim = SentAbandonedCheckoutEmail.Claim(message.BookingId, message.StudentUserId, message.TutorUserId, clock);
            db.Add(claim);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (IsUniqueViolation(ex))
            {
                // Lost a genuine race against another delivery of the same event — the other
                // delivery owns sending it; detach so this unit of work doesn't try to re-save
                // the now-conflicting entity on a later call.
                db.Entry(claim).State = EntityState.Detached;
                return;
            }

            var recipientEmail = await userEmailQuery.GetEmailAsync(message.StudentUserId, ct);
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                claim.MarkSkipped("No email on file for this student.");
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Abandoned-checkout email skipped for booking {BookingId}: no email on file", message.BookingId);
                return;
            }

            var tutorProfile = await tutorProfileQuery.GetAsync(message.TutorUserId, ct);
            if (tutorProfile is null)
            {
                claim.MarkSkipped("Tutor profile unavailable.");
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Abandoned-checkout email skipped for booking {BookingId}: tutor profile unavailable", message.BookingId);
                return;
            }

            var email = AbandonedCheckoutEmailTemplate.Build(recipientEmail, tutorProfile, message, options.Value.AppBaseUrl);
            var result = await emailSender.SendAsync(email, ct);

            if (result.IsSuccess)
            {
                claim.MarkSent(clock);
                logger.LogInformation("Abandoned-checkout email sent for booking {BookingId}", message.BookingId);
            }
            else
            {
                // Deliberately never persists or logs result.ErrorMessage verbatim — a provider
                // error can echo back request fields (e.g. the recipient address); only a fixed,
                // non-PII-bearing classification is kept.
                claim.MarkSkipped("Provider send failed.");
                logger.LogWarning("Abandoned-checkout email failed to send for booking {BookingId}", message.BookingId);
            }

            await db.SaveChangesAsync(ct);
        }

        private static bool IsUniqueViolation(Exception ex) =>
            ex is DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation } };
    }
}

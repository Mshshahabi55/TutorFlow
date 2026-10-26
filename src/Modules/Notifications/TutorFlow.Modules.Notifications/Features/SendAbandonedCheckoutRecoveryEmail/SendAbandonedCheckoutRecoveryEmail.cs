using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TutorFlow.Modules.Notifications.Domain;
using TutorFlow.Modules.Notifications.Infrastructure;
using TutorFlow.Modules.Notifications.Persistence;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Notifications.Features.SendAbandonedCheckoutRecoveryEmail;

/// <summary>
/// Reacts to <see cref="BookingHoldExpired"/> (ADR-0029, Addendum A) — the TutorFlow equivalent of
/// the OBSERVED Preply "abandoned slot" recovery email (`18-screenshot-evidence.md` SS51–SS52).
/// Replicates the observed information architecture (tutor identity, slot date/time, a CTA back
/// to the tutor) with original TutorFlow copy — never Preply's proprietary wording. Idempotent via
/// <see cref="SentAbandonedCheckoutEmail"/>'s DB-unique <c>BookingId</c> index against duplicate
/// *rows*, and via its owner-token fencing lease for safely resuming an *unresolved* row without
/// ever reopening a resolved one — see that entity's own doc comment and
/// <c>AbandonedCheckoutClaimReconciliationService</c> for the full state machine.
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
            bool alreadyResolved;
            try
            {
                // Fast-path idempotency check — a RESOLVED row (WasSent or a terminal
                // SkippedReason) is the only "nothing left to do" signal. An unresolved row
                // (neither) is NOT treated as done (ADR-0029 Addendum A) — that state is handled
                // by the owner-token fencing protocol below / by reconciliation.
                alreadyResolved = await db.SentAbandonedCheckoutEmails.AsNoTracking()
                    .AnyAsync(e => e.BookingId == message.BookingId && (e.WasSent || e.SkippedReason != null), ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Distinct from the "Technical failure during processing" skip below — no claim
                // row exists yet at this point, so there is nothing to mark skipped. Rethrowing
                // (not swallowing) keeps Wolverine's existing unhandled-failure/error-queue
                // behavior exactly as it is today; this only makes the failure attributable in
                // logs instead of surfacing as an undifferentiated pipeline exception.
                logger.LogError(ex, "Abandoned-checkout pre-claim idempotency check failed for booking {BookingId} before any claim could be recorded — this delivery is unresolved and not retried automatically", message.BookingId);
                throw;
            }

            if (alreadyResolved)
            {
                return;
            }

            var ownerToken = Guid.NewGuid();
            var leaseDuration = TimeSpan.FromMinutes(options.Value.ClaimLeaseMinutes);
            var claim = SentAbandonedCheckoutEmail.Claim(
                message.BookingId, message.StudentUserId, message.TutorUserId, message.StartUtc, ownerToken, leaseDuration, clock);
            db.Add(claim);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (IsUniqueViolation(ex))
            {
                // Lost the INSERT race — a claim already exists for this booking. It may be
                // resolved (nothing to do), genuinely still owned by an active worker within its
                // lease (must not be touched), or stale and abandoned (ADR-0029 Addendum A:
                // Batch 51 — safely recoverable here through the exact same atomic,
                // staleness-gated takeover protocol reconciliation uses, not a separate one).
                db.Entry(claim).State = EntityState.Detached;

                var takeoverToken = Guid.NewGuid();
                var takenOver = await AbandonedCheckoutClaimWrites.TryTakeOverStaleAsync(
                    db, message.BookingId, takeoverToken, clock.GetUtcNow(), leaseDuration, ct) == 1;
                if (!takenOver)
                {
                    // Resolved, or still genuinely owned within its lease — either way this
                    // delivery has nothing further to do. Reconciliation remains the backstop for
                    // a still-unresolved row that goes stale later.
                    return;
                }

                logger.LogInformation("Abandoned-checkout claim for booking {BookingId} was stale and abandoned — this delivery took over ownership directly instead of waiting for reconciliation", message.BookingId);
                await ProcessOwnedClaimAsync(message.BookingId, message.StudentUserId, message.TutorUserId, message.StartUtc, takeoverToken, ct);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The insert's outcome is now ambiguous — it may or may not have actually
                // committed before this exception surfaced (observed directly in this project: a
                // transient Npgsql connection failure can occur after the server has already
                // applied the write). Rethrowing (not swallowing) preserves today's
                // unhandled-failure visibility; if the row did commit, the reconciliation service
                // will find it once its lease goes stale and safely take over.
                logger.LogError(ex, "Abandoned-checkout claim persistence failed for booking {BookingId} — the insert's outcome is ambiguous; this delivery is unresolved here, but eligible for reconciliation once its lease expires", message.BookingId);
                throw;
            }

            await ProcessOwnedClaimAsync(message.BookingId, message.StudentUserId, message.TutorUserId, message.StartUtc, ownerToken, ct);
        }

        /// <summary>
        /// The actual lookup/send/resolve sequence, shared by the event-driven path above
        /// (immediately after a fresh claim) and <c>AbandonedCheckoutClaimReconciliationService</c>
        /// (after taking over a stale, abandoned claim) — both ways of legitimately coming to own
        /// a claim converge on identical processing and identical ownership-fencing discipline.
        /// Callers must already hold ownership (a successful insert or a successful takeover)
        /// before calling this.
        /// </summary>
        internal async Task ProcessOwnedClaimAsync(Guid bookingId, Guid studentUserId, Guid tutorUserId, DateTimeOffset startUtc, Guid ownerToken, CancellationToken ct)
        {
            string? recipientEmail;
            TutorProfileSummary tutorProfile;

            // Phase 1 — strictly before any external send attempt. Every failure caught here is
            // PROVEN to have no possible external side effect (the two cross-module reads and the
            // ownership check below never touch the email provider), so confidently recording a
            // terminal technical-failure skip is safe and accurate (ADR-0029 Addendum A).
            try
            {
                recipientEmail = await userEmailQuery.GetEmailAsync(studentUserId, ct);
                if (string.IsNullOrWhiteSpace(recipientEmail))
                {
                    var resolved = await AbandonedCheckoutClaimWrites.TryResolveAsOwnerAsync(
                        db, bookingId, ownerToken, wasSent: false, sentAtUtc: null, "No email on file for this student.", ct) == 1;
                    LogResolution(resolved, bookingId, LogLevel.Information, "Abandoned-checkout email skipped for booking {BookingId}: no email on file");
                    return;
                }

                var profile = await tutorProfileQuery.GetAsync(tutorUserId, ct);
                if (profile is null)
                {
                    var resolved = await AbandonedCheckoutClaimWrites.TryResolveAsOwnerAsync(
                        db, bookingId, ownerToken, wasSent: false, sentAtUtc: null, "Tutor profile unavailable.", ct) == 1;
                    LogResolution(resolved, bookingId, LogLevel.Information, "Abandoned-checkout email skipped for booking {BookingId}: tutor profile unavailable");
                    return;
                }

                tutorProfile = profile;

                // The critical fencing check (ADR-0029 Addendum A): a reconciliation sweep may
                // already have decided this worker is dead and taken over between claim and
                // here. If so, the new owner is responsible now — this worker must not send. This
                // narrows, but does not fully close, the race: a takeover landing in the gap
                // between this check and the resolve write below is only caught by that write's
                // own guarded affected-row check, not by this check.
                if (!await AbandonedCheckoutClaimWrites.StillOwnsAsync(db, bookingId, ownerToken, ct))
                {
                    logger.LogWarning("Abandoned-checkout processing for booking {BookingId} lost claim ownership before the send attempt — another worker now owns this claim; aborting without sending", bookingId);
                    return;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException && IsTransientDataAccessFailure(ex))
            {
                await RecordTechnicalFailureSkipAsync(ex, bookingId, ownerToken, ct);
                return;
            }

            // Phase 2 — from here on, IEmailSender.SendAsync is about to be invoked (or has been).
            // A failure anywhere below can no longer be confidently classified as "nothing
            // happened" (ADR-0029 Addendum A, Batch 53 update): the provider may have accepted the
            // email before an exception surfaced. Each step below decides, on its own evidence,
            // whether a side effect could have occurred.
            var eventForTemplate = new BookingHoldExpired(bookingId, studentUserId, tutorUserId, startUtc, startUtc, 0, false, startUtc);
            var email = AbandonedCheckoutEmailTemplate.Build(recipientEmail, tutorProfile, eventForTemplate, options.Value.AppBaseUrl);

            EmailSendResult result;
            try
            {
                result = await emailSender.SendAsync(email, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException && IsTransientDataAccessFailure(ex))
            {
                // The send attempt itself failed — whether the provider received and accepted the
                // request before this exception surfaced is unknown. Never record this as a
                // confident "not sent" skip: that could permanently hide a delivery that actually
                // happened. Leave the claim unresolved and propagate instead; reconciliation (or a
                // later redelivery) remains the only way to revisit it, accepting the same residual
                // duplicate-send risk already documented for the post-send race below.
                logger.LogError(ex, "Abandoned-checkout email send attempt for booking {BookingId} failed with an uncertain outcome — the provider may or may not have accepted it; leaving this claim unresolved rather than asserting a skip", bookingId);
                throw;
            }

            switch (result.Outcome)
            {
                case EmailDeliveryOutcome.Sent:
                    try
                    {
                        var resolved = await AbandonedCheckoutClaimWrites.TryResolveAsOwnerAsync(
                            db, bookingId, ownerToken, wasSent: true, clock.GetUtcNow(), skippedReason: null, ct) == 1;
                        if (resolved)
                        {
                            logger.LogInformation("Abandoned-checkout email sent for booking {BookingId}", bookingId);
                        }
                        else
                        {
                            // The provider has already been called — this cannot be undone. Ownership
                            // was lost in the narrow window between the check above and this write, so
                            // another worker may independently resend. This residual duplicate-send
                            // risk is accepted and documented (ADR-0029 Addendum A); Postmark offers no
                            // idempotency key this integration can use to close it.
                            logger.LogError("Abandoned-checkout email for booking {BookingId} was sent to the provider, but ownership was lost before the result could be recorded — a duplicate send by another worker is possible", bookingId);
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException && IsTransientDataAccessFailure(ex))
                    {
                        // The provider has ALREADY accepted this email — that side effect cannot be
                        // undone. Recording a "Technical failure during processing." skip here would be
                        // actively false: it would permanently claim the student was never emailed when
                        // they were (the Batch 52 finding this phase separation exists to close). Leave
                        // the claim unresolved instead and propagate — reconciliation remains the only
                        // way to revisit it, accepting a residual duplicate-send risk rather than a
                        // false delivery record. Exactly-once delivery is still not claimed.
                        logger.LogError(ex, "Abandoned-checkout email for booking {BookingId} was accepted by the provider, but recording that outcome failed — leaving the claim unresolved rather than a false technical-failure skip; a duplicate send by a later owner remains possible", bookingId);
                        throw;
                    }

                    break;

                case EmailDeliveryOutcome.ConfirmedNotSent:
                    // The sender's own contract proved this email was never sent (ADR-0029
                    // Addendum A, Batch 55 — see PostmarkEmailSender for exactly which outcomes
                    // qualify) — no side effect occurred either way, so a persistence failure
                    // recording that outcome is handled identically to Phase 1: safe to record a
                    // confident terminal technical-failure skip.
                    try
                    {
                        // Deliberately never persists or logs result.ErrorMessage verbatim — a provider
                        // error can echo back request fields (e.g. the recipient address); only a
                        // fixed, non-PII-bearing classification is kept.
                        var resolved = await AbandonedCheckoutClaimWrites.TryResolveAsOwnerAsync(
                            db, bookingId, ownerToken, wasSent: false, sentAtUtc: null, "Provider send failed.", ct) == 1;
                        LogResolution(resolved, bookingId, LogLevel.Warning, "Abandoned-checkout email failed to send for booking {BookingId}");
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException && IsTransientDataAccessFailure(ex))
                    {
                        await RecordTechnicalFailureSkipAsync(ex, bookingId, ownerToken, ct);
                    }

                    break;

                case EmailDeliveryOutcome.Ambiguous:
                default:
                    // Whether the provider accepted the email could not be determined (ADR-0029
                    // Addendum A, Batch 55) — never record this as a terminal skip; doing so would
                    // be a confident claim of non-delivery this worker cannot actually prove. Leave
                    // the claim unresolved instead: reconciliation (or a later redelivery) remains
                    // the only way to revisit it once the lease expires, accepting the same residual
                    // duplicate-send risk already documented for the post-send race above.
                    // result.ErrorMessage here is always either a transport-exception message or
                    // Postmark's own error response body, never the recipient's address or content.
                    logger.LogError("Abandoned-checkout email send outcome for booking {BookingId} could not be determined ({ErrorMessage}) — leaving this claim unresolved rather than asserting a skip", bookingId, result.ErrorMessage);
                    break;
            }
        }

        private async Task RecordTechnicalFailureSkipAsync(Exception ex, Guid bookingId, Guid ownerToken, CancellationToken ct)
        {
            logger.LogError(ex, "Abandoned-checkout email processing hit an unexpected technical failure for booking {BookingId} before any send attempt — recording a technical-failure skip", bookingId);

            try
            {
                var resolved = await AbandonedCheckoutClaimWrites.TryResolveAsOwnerAsync(
                    db, bookingId, ownerToken, wasSent: false, sentAtUtc: null, "Technical failure during processing.", ct) == 1;
                if (!resolved)
                {
                    logger.LogWarning("Abandoned-checkout technical-failure skip for booking {BookingId} could not be recorded under this worker's ownership — another worker has since taken over", bookingId);
                }
            }
            catch (Exception persistEx)
            {
                // The claim genuinely cannot be finalized by this worker — never claim otherwise.
                // Rethrowing (rather than swallowing) keeps this visible as an unhandled failure;
                // the row remains eligible for reconciliation once stale.
                logger.LogError(persistEx, "Failed to persist the technical-failure skip for booking {BookingId} after the original failure logged above — the claim remains unresolved", bookingId);
                throw;
            }
        }

        private void LogResolution(bool resolved, Guid bookingId, LogLevel successLevel, string successMessageTemplate)
        {
            if (resolved)
            {
                logger.Log(successLevel, successMessageTemplate, bookingId);
            }
            else
            {
                logger.LogWarning("Abandoned-checkout resolution for booking {BookingId} could not be recorded under this worker's ownership — another worker has since taken over", bookingId);
            }
        }

        private static bool IsUniqueViolation(Exception ex) =>
            ex is DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation } };

        // The exact shape directly observed twice this session at this call site: Npgsql's own
        // connection-level exception, sometimes surfaced directly and sometimes wrapped by EF
        // Core's execution strategy in an InvalidOperationException. Intentionally narrower than
        // bare Exception/DbException — an unrelated programming error (e.g. a LINQ translation
        // bug) must still propagate rather than be silently recorded as a technical skip.
        private static bool IsTransientDataAccessFailure(Exception ex) =>
            ex is Npgsql.NpgsqlException || ex.InnerException is Npgsql.NpgsqlException;
    }
}

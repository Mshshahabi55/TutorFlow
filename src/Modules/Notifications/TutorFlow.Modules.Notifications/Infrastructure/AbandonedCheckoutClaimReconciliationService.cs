using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TutorFlow.Modules.Notifications.Persistence;
using SendAbandonedCheckoutRecoveryEmailFeature = TutorFlow.Modules.Notifications.Features.SendAbandonedCheckoutRecoveryEmail.SendAbandonedCheckoutRecoveryEmail;

namespace TutorFlow.Modules.Notifications.Infrastructure;

/// <summary>
/// ADR-0029 Addendum A: recovers <c>SentAbandonedCheckoutEmail</c> rows left unresolved by a
/// crash, an ambiguous database commit, or cancellation — the gap the original claim-then-act
/// design did not close (reinstating a *resolved* skip remains explicitly out of scope,
/// unchanged). Same <see cref="BackgroundService"/> + <see cref="PeriodicTimer"/> shape as
/// <c>BookingHoldExpirySweepService</c>, for the same reasons (no job-scheduling library in this
/// codebase; Wolverine's own scheduled delivery has no durable backing store here). Bounded per
/// tick (<see cref="NotificationsOptions.ClaimReconciliationBatchSize"/>) — never loads every
/// unresolved claim into memory at once.
/// </summary>
public sealed class AbandonedCheckoutClaimReconciliationService(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    IOptions<NotificationsOptions> options,
    ILogger<AbandonedCheckoutClaimReconciliationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.ClaimReconciliationIntervalSeconds), clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ReconcileOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A failed tick must never crash the host or stop future ticks — the next tick
                // will simply re-evaluate the same (still-stale) candidates.
                logger.LogError(ex, "Abandoned-checkout claim reconciliation tick failed");
            }
        }
    }

    /// <summary>Public so tests can drive one reconciliation pass deterministically rather than
    /// waiting on the real timer (matching <c>BookingHoldExpirySweepService.SweepOnceAsync</c>'s
    /// established "public for testability" convention).</summary>
    public async Task ReconcileOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var handler = scope.ServiceProvider.GetRequiredService<SendAbandonedCheckoutRecoveryEmailFeature.Handler>();
        var now = clock.GetUtcNow();
        var leaseDuration = TimeSpan.FromMinutes(options.Value.ClaimLeaseMinutes);
        var batchSize = options.Value.ClaimReconciliationBatchSize;

        // Read-only candidate list first — never the source of truth for "did this worker
        // actually acquire it" — the guarded per-row takeover below is, exactly matching
        // BookingHoldExpirySweepService's own candidates-then-guarded-update shape.
        var candidates = await db.SentAbandonedCheckoutEmails
            .AsNoTracking()
            .Where(e => !e.WasSent && e.SkippedReason == null && e.OwnedUntilUtc < now)
            .OrderBy(e => e.OwnedUntilUtc)
            .Take(batchSize)
            .Select(e => new { e.BookingId, e.StudentUserId, e.TutorUserId, e.StartUtc })
            .ToListAsync(ct);

        logger.LogInformation("Abandoned-checkout claim reconciliation scan found {CandidateCount} stale unresolved claim(s)", candidates.Count);

        foreach (var candidate in candidates)
        {
            var newOwnerToken = Guid.NewGuid();
            var affected = await AbandonedCheckoutClaimWrites.TryTakeOverStaleAsync(db, candidate.BookingId, newOwnerToken, now, leaseDuration, ct);
            if (affected != 1)
            {
                // Another reconciliation tick (or, implausibly, a still-live original worker)
                // already acted on this row since the read above — not an error, just lost this
                // particular race; it is re-evaluated next tick if still unresolved.
                continue;
            }

            logger.LogInformation("Abandoned-checkout claim reconciliation acquired ownership of booking {BookingId}", candidate.BookingId);

            try
            {
                await handler.ProcessOwnedClaimAsync(candidate.BookingId, candidate.StudentUserId, candidate.TutorUserId, candidate.StartUtc, newOwnerToken, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // ProcessOwnedClaimAsync already logs and classifies everything it can recover
                // from; anything that still escapes here is the rethrow path for a claim that
                // truly cannot be finalized right now (e.g. the resolve write itself failed) —
                // log it and move on to the next candidate rather than aborting the whole batch.
                logger.LogError(ex, "Abandoned-checkout claim reconciliation failed to resolve booking {BookingId}", candidate.BookingId);
            }
        }
    }
}

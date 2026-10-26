using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TutorFlow.Modules.Bookings.Domain;
using TutorFlow.Modules.Bookings.Persistence;
using TutorFlow.Shared;
using Wolverine;

namespace TutorFlow.Modules.Bookings.Infrastructure;

/// <summary>
/// The real, time-driven trigger for <see cref="BookingHoldExpired"/> — ADR-0029. This is a
/// genuinely new piece of infrastructure, not a duplicate of Booking's existing state machine:
/// <see cref="BookingWrites.ExpireStaleHoldsAsync"/> already existed, but only runs lazily,
/// scoped to one tutor, at the moment a *different* student happens to request a new hold with
/// that same tutor (ADR-0013 §2) — it cannot serve as a reliable per-booking notification
/// trigger, since a tutor with no other booking traffic would never have their stale holds swept
/// at all. This service reuses the exact same conditional-UPDATE guard
/// (<see cref="BookingWrites.ExpireSingleHoldIfStaleAsync"/>) on a standard .NET
/// <see cref="PeriodicTimer"/>-driven <see cref="BackgroundService"/> — the framework's own
/// recommended building block for recurring background work, not a hand-rolled polling loop —
/// because no job-scheduling library (Hangfire/Quartz) exists anywhere in this codebase yet and
/// Wolverine's own scheduled-message delivery has no durable backing store in this project
/// (`OPEN_QUESTIONS.md` A44) to survive a restart. The sweep interval
/// (<see cref="BookingsOptions.HoldExpirySweepIntervalSeconds"/>) is TutorFlow's own engineering
/// choice, explicitly not a reverse-engineered Preply timing (which is NOT VERIFIED — see
/// ADR-0029).
///
/// A44 #103: deliberately NOT routed through a Wolverine chain (unlike PublishApplication/
/// SuspendApplication/RejectCertificate/ProcessStripeWebhook) — see
/// docs/HANDOFF-2026-10-26-delivery-sprint.md's A44 #103 addendum for why: Eager mode was found,
/// empirically, to mis-route the outgoing envelope to the wrong Ancillary store (observed:
/// "payments" schema, via BookingsDbContext's own connection) whenever more than one Ancillary
/// store is registered — reproducible against genuinely separate per-module databases, which is
/// this project's actual target topology, not just a test artifact. Lightweight mode avoids that
/// specific bug but reintroduces the original gap with no safe compensating mechanism available
/// (no persisted marker on Booking analogous to TutorBookability.LastEventAtUtc or
/// Payment.PaymentReferenceId to safely gate an idempotent republish). Left unfixed and reported,
/// per this task's explicit instruction not to hide a gap that cannot be safely closed.
/// </summary>
public sealed class BookingHoldExpirySweepService(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    IOptions<BookingsOptions> options,
    ILogger<BookingHoldExpirySweepService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.HoldExpirySweepIntervalSeconds), clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await SweepOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A failed tick must never crash the host or stop future ticks — the next tick
                // will simply re-evaluate the same (still-stale) candidates.
                logger.LogError(ex, "Booking hold-expiry sweep tick failed");
            }
        }
    }

    /// <summary>Public so tests can drive one sweep deterministically rather than waiting on the
    /// real timer (matching this codebase's established "public for testability" convention,
    /// e.g. <c>ActiveBookingIntervalsQuery</c>).</summary>
    public async Task SweepOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var now = clock.GetUtcNow();

        // Read-only candidate list first (never the source of truth for "did this actually
        // transition" — the guarded per-row update below is) — Confirmed/Cancelled/Completed/
        // NoShow/Refunded/Rescheduled bookings are excluded by the Status filter alone, with no
        // special-casing needed for any of them.
        var candidates = await db.Bookings
            .AsNoTracking()
            .Where(b => (b.Status == BookingStatus.Held || b.Status == BookingStatus.PendingPayment) && b.HoldExpiresAtUtc < now)
            .Select(b => new { b.Id, b.StudentUserId, b.TutorUserId, b.StartUtc, b.EndUtc, b.DurationMinutes, b.IsTrial })
            .ToListAsync(ct);

        foreach (var candidate in candidates)
        {
            var affected = await BookingWrites.ExpireSingleHoldIfStaleAsync(db, candidate.Id, now, ct);
            if (affected == 1)
            {
                await bus.PublishAsync(new BookingHoldExpired(
                    candidate.Id,
                    candidate.StudentUserId,
                    candidate.TutorUserId,
                    candidate.StartUtc,
                    candidate.EndUtc,
                    candidate.DurationMinutes,
                    candidate.IsTrial,
                    now));
            }
        }
    }
}

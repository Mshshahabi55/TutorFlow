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
/// A44 #103 bounded fallback (see <c>docs/OPEN_QUESTIONS.md</c>'s A44 entry for the full
/// investigation): routing this through a Wolverine chain was attempted twice and reproducibly
/// found unsafe both times — first under Eager mode (the outgoing envelope mis-routed to
/// whichever Ancillary store happened to be registered last), then again after fixing that exact
/// root cause with Wolverine.Postgresql's <c>Enroll&lt;T&gt;()</c> (which fixed the originally-
/// targeted misrouting, but surfaced a second, confirmed Wolverine 6.45.0 codegen-ordering
/// defect: generated chains run <c>EnlistInOutboxAsync</c>/<c>EnrollDbContextInTransaction</c>
/// before the frame that pins <c>MessageContext.Storage</c> by type, so the very first delivery
/// through that newly-fixed path durably persisted its incoming envelope against the wrong
/// schema regardless). Both are genuine Wolverine-library-level limitations, not something this
/// module's own code can safely patch. Fallback instead: this service owns its own durability
/// directly, via <see cref="BookingHoldExpiryIntent"/> — a plain row in Bookings' own database,
/// written in the same explicit transaction as the booking's status change (so a crash between
/// the two can never happen), later dispatched by this same service's own second phase. No
/// Wolverine envelope/outbox machinery is involved in persisting the durability guarantee at
/// all — only the final, ordinary <c>bus.PublishAsync</c> once dispatch has already decided to
/// deliver, which carries no store-ambiguity risk because nothing asks Wolverine to persist it
/// transactionally cross-store.
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
                // will simply re-evaluate the same (still-stale) candidates, and any already-
                // claimed-but-undispatched intent from a prior tick is retried by the dispatch
                // phase below regardless of what failed here.
                logger.LogError(ex, "Booking hold-expiry sweep tick failed");
            }
        }
    }

    /// <summary>Public so tests can drive one sweep deterministically rather than waiting on the
    /// real timer (matching this codebase's established "public for testability" convention,
    /// e.g. <c>ActiveBookingIntervalsQuery</c>). Two independent phases, both idempotent and
    /// safe to run concurrently with themselves or each other: claim (business-state change +
    /// durable intent, atomically) and dispatch (publish owed events, oldest-first, marking each
    /// delivered only once <c>bus.PublishAsync</c> has actually returned).</summary>
    public async Task SweepOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var now = clock.GetUtcNow();

        var candidates = await db.Bookings
            .AsNoTracking()
            .Where(b => (b.Status == BookingStatus.Held || b.Status == BookingStatus.PendingPayment) && b.HoldExpiresAtUtc < now)
            .Select(b => new { b.Id, b.StudentUserId, b.TutorUserId, b.StartUtc, b.EndUtc, b.DurationMinutes, b.IsTrial })
            .ToListAsync(ct);

        foreach (var candidate in candidates)
        {
            try
            {
                await using var transaction = await db.Database.BeginTransactionAsync(ct);

                // Never the source of truth for "did this actually transition" — the guarded
                // per-row update is. Confirmed/Cancelled/Completed/NoShow/Refunded/Rescheduled
                // bookings are excluded by the Status filter alone, with no special-casing
                // needed for any of them.
                var affected = await BookingWrites.ExpireSingleHoldIfStaleAsync(db, candidate.Id, now, ct);
                if (affected == 1)
                {
                    // Same transaction as the status change above — a crash here rolls back
                    // both together, leaving neither a partial expiry nor an orphaned intent.
                    db.BookingHoldExpiryIntents.Add(BookingHoldExpiryIntent.Create(
                        candidate.Id, candidate.StudentUserId, candidate.TutorUserId,
                        candidate.StartUtc, candidate.EndUtc, candidate.DurationMinutes, candidate.IsTrial, now));
                    await db.SaveChangesAsync(ct);
                }

                await transaction.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                // One candidate's genuine failure (e.g. a transient DB error) must never block
                // the rest of this tick's otherwise-healthy candidates — the next tick
                // re-evaluates this one from scratch, since nothing partial was ever committed.
                logger.LogError(ex, "Failed to claim hold expiry for booking {BookingId}", candidate.Id);
            }
        }

        await DispatchPendingIntentsAsync(db, bus, ct);
    }

    /// <summary>
    /// Publishes every not-yet-dispatched intent and marks it dispatched — deliberately in that
    /// order, never reversed: a crash between <c>PublishAsync</c> returning and the follow-up
    /// <c>SaveChangesAsync</c> leaves the intent looking "still owed," so the next tick (or the
    /// next restart) re-publishes it. That is an accepted, at-least-once duplicate, not a lost
    /// event — the same guarantee every other event in this codebase provides, absorbed the same
    /// way: <see cref="Notifications.Consumers.BookingHoldExpiredConsumer"/>'s own existing
    /// claim/resolve protocol (ADR-0029 Addendum A) already treats a duplicate
    /// <c>BookingHoldExpired</c> delivery as a harmless re-send, by design, independent of this
    /// change. Concurrent dispatchers (if this service were ever scaled to multiple nodes) are
    /// safe for the identical reason — a duplicate publish is tolerated, not prevented — rather
    /// than via a dedicated claim lock, which would reintroduce exactly the "mark before the work
    /// is confirmed done" ordering risk this method's own comment above explains avoiding.
    /// </summary>
    private async Task DispatchPendingIntentsAsync(BookingsDbContext db, IMessageBus bus, CancellationToken ct)
    {
        var pending = await db.BookingHoldExpiryIntents
            .Where(i => i.DispatchedAtUtc == null)
            .OrderBy(i => i.CreatedAtUtc)
            .ToListAsync(ct);

        foreach (var intent in pending)
        {
            try
            {
                await bus.PublishAsync(new BookingHoldExpired(
                    intent.BookingId, intent.StudentUserId, intent.TutorUserId,
                    intent.StartUtc, intent.EndUtc, intent.DurationMinutes, intent.IsTrial, intent.ExpiredAtUtc));

                intent.MarkDispatched(clock.GetUtcNow());
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to dispatch hold-expiry intent for booking {BookingId}", intent.BookingId);
            }
        }
    }
}

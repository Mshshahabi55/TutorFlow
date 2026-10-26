using TutorFlow.Modules.Availability.Infrastructure;
using TutorFlow.Modules.Availability.Persistence;
using TutorFlow.Shared;
using Wolverine.Attributes;
using Wolverine.Persistence;

namespace TutorFlow.Modules.Availability.Consumers;

/// <summary>
/// Explicitly marked for Wolverine discovery (F6 production reliability fix — see Program.cs's
/// DisableConventionalDiscovery comment: ordinary feature "Handler" classes across every module
/// matched Wolverine's default "type name ends with Handler" naming convention too, which
/// `codegen write`'s ahead-of-time, every-discovered-type pass turned into real build failures
/// that Dynamic mode's lazy, never-actually-dispatched compilation had never surfaced). Same as
/// Search's own <c>TutorPublishedConsumer</c> — the second subscriber to this event (ADR-0011's
/// original design, wired for the first time here, ADR-0024). Gives
/// <see cref="TutorBookabilityProjector"/> its first real caller: a newly-published tutor becomes
/// bookable, then the availability signal is computed and published for Search to pick up.
/// </summary>
/// <remarks>
/// A44: <see cref="AvailabilitySignalPublisher"/>'s own dependency chain
/// (<c>GetTutorAvailabilityPreview.Handler</c>) also reaches <c>BookingsDbContext</c>, but only
/// ever to read it (<c>IActiveBookingIntervalsQuery</c>) — the only DbContext this handler ever
/// writes to is <see cref="AvailabilityDbContext"/>, via
/// <see cref="TutorBookabilityProjector.ApplyBookabilityChangedAsync"/>. Explicitly designating
/// it resolves Wolverine's "multiple DbContext types detected" ambiguity under
/// <c>AutoApplyTransactions()</c> (Program.cs) without guessing.
/// <para>
/// A44 topology fix: Lightweight mode (not Eager) — AvailabilityDbContext's connection only
/// needs to be atomic with the Main store's envelope bookkeeping if both are reachable on the
/// same physical connection, which production's one-database-per-module topology does not
/// guarantee. Safe specifically because <see cref="HandleAsync"/> now republishes the signal
/// unconditionally on every call (never gated on whether the projector actually changed a row)
/// and <see cref="Domain.TutorBookability.TryApply(bool,DateTimeOffset,TimeProvider)"/> guards
/// against a stale/out-of-order redelivery incorrectly reinstating bookability — see both of
/// their own remarks for the full analysis.
/// </para>
/// </remarks>
[WolverineHandler]
[Transactional(typeof(AvailabilityDbContext), Mode = TransactionMiddlewareMode.Lightweight)]
public sealed class TutorPublishedConsumer(TutorBookabilityProjector projector, AvailabilitySignalPublisher signalPublisher)
{
    public async Task HandleAsync(TutorPublished message, CancellationToken ct)
    {
        await projector.ApplyBookabilityChangedAsync(message.TutorUserId, isBookable: true, message.OccurredAtUtc, ct);

        // A44 replay-safety fix: unconditional, regardless of whether the projector call above
        // actually changed anything (including when TryApply ignored a stale redelivery) —
        // recomputed live from current authoritative state (GetTutorAvailabilityPreview), never
        // replayed from this message's own payload, so a redelivery after a crash between the
        // projector's commit and this publish still recovers the signal correctly.
        await signalPublisher.PublishCurrentSignalAsync(message.TutorUserId, ct);
    }
}

using TutorFlow.Modules.Availability.Persistence;
using TutorFlow.Shared;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.Persistence;
using GetTutorAvailabilityPreviewFeature = TutorFlow.Modules.Availability.Features.GetTutorAvailabilityPreview.GetTutorAvailabilityPreview;

namespace TutorFlow.Modules.Availability.Infrastructure;

/// <summary>
/// Recomputes and publishes <see cref="TutorAvailabilitySignalChanged"/> (ADR-0024) by reusing
/// the exact computation the Tutor Profile page already shows to guests
/// (<see cref="GetTutorAvailabilityPreviewFeature.Handler"/>) — not a second, separate
/// implementation. Called from every self-service mutation that could change whether a tutor has
/// an upcoming open slot, plus once on first becoming bookable (<c>TutorPublished</c>).
/// Public (not internal, despite the usual convention) because Wolverine's convention-discovered
/// consumers that depend on it (<c>Consumers/TutorPublishedConsumer</c>) must themselves be
/// public, and a public type cannot take a less-accessible type as a constructor parameter.
///
/// A44 #103: this class and its direct callers from <c>Consumers/TutorPublishedConsumer</c>/
/// <c>TutorUnpublishedConsumer</c> are deliberately left unchanged — those are already-fixed,
/// already-tested Lightweight chains (see their own remarks) and must not be disturbed. The
/// self-service callers (AddTimeOff/RemoveTimeOff/ReplaceWeeklyRules/UpdateBookingSettings) are
/// fixed separately, below, by dispatching <see cref="RecomputeAvailabilitySignalCommand"/>
/// through Wolverine instead of calling <see cref="PublishCurrentSignalAsync"/> directly — see
/// that command's own remarks for why, and for what tier of safety this actually achieves.
/// </summary>
public sealed class AvailabilitySignalPublisher(
    GetTutorAvailabilityPreviewFeature.Handler preview,
    IMessageBus bus,
    TimeProvider clock)
{
    public async Task PublishCurrentSignalAsync(Guid tutorUserId, CancellationToken ct)
    {
        var response = await preview.HandleAsync(tutorUserId, ct);
        await bus.PublishAsync(new TutorAvailabilitySignalChanged(tutorUserId, response.HasUpcomingAvailability, clock.GetUtcNow()));
    }
}

/// <summary>
/// A44 #103: dispatched by the four self-service handlers (AddTimeOff/RemoveTimeOff/
/// ReplaceWeeklyRules/UpdateBookingSettings) instead of their calling
/// <see cref="AvailabilitySignalPublisher.PublishCurrentSignalAsync"/> directly. This is a
/// deliberately weaker guarantee than the other A44 #103 fixes, documented honestly rather than
/// overclaimed: it does NOT make the business write and this publish share one transaction.
/// <c>ReplaceWeeklyRules.Handler</c> already manages its own explicit Serializable transaction
/// for a real, independent reason (write-skew prevention) that a second, Wolverine-managed
/// transaction on the same DbContext would conflict with; the other three handlers' plain
/// <c>SaveChangesAsync</c> calls could in principle be deferred the way
/// <c>PublishApplication</c>/<c>SuspendApplication</c> were, but <see cref="GetTutorAvailabilityPreviewFeature.Handler"/>'s
/// own query would trigger EF Core's implicit pre-query <c>SaveChanges</c> on any pending tracked
/// change anyway, silently reintroducing a second commit. Routing just the publish through its
/// own Wolverine chain (Lightweight; AvailabilityDbContext transitively reaches BookingsDbContext
/// via GetTutorAvailabilityPreview, so the DbContext must be named explicitly — same reason
/// <c>Consumers/TutorPublishedConsumer</c> does) still closes the gap where today's bare
/// <c>IMessageBus.PublishAsync</c> call happens with no enlisting chain at all. The remaining,
/// accepted gap is structurally identical to <c>ConfirmBooking.Handler</c>'s already-accepted
/// Lightweight tier — and lower severity here, since <see cref="TutorAvailabilitySignalChanged"/>
/// is a recomputed projection, not a one-time irreversible transition: a lost publish goes stale
/// only until the next trigger (another self-service edit, or a future TutorPublished/
/// TutorUnpublished) recomputes it again.
/// </summary>
public sealed record RecomputeAvailabilitySignalCommand(Guid TutorUserId);

[WolverineHandler]
[Transactional(typeof(AvailabilityDbContext), Mode = TransactionMiddlewareMode.Lightweight)]
public sealed class RecomputeAvailabilitySignalConsumer(AvailabilitySignalPublisher signalPublisher)
{
    public Task HandleAsync(RecomputeAvailabilitySignalCommand command, CancellationToken ct) =>
        signalPublisher.PublishCurrentSignalAsync(command.TutorUserId, ct);
}

using TutorFlow.Modules.Availability.Infrastructure;
using TutorFlow.Modules.Availability.Persistence;
using TutorFlow.Shared;
using Wolverine.Attributes;
using Wolverine.Persistence;

namespace TutorFlow.Modules.Availability.Consumers;

/// <summary>
/// Flips a suspended tutor's bookability back to <c>false</c> (ADR-0024) — without this,
/// <c>ITutorAvailabilityQuery</c>/<c>GetTutorSlots</c> would keep treating a suspended tutor as
/// bookable, since nothing else in this module reacts to suspension. Explicitly marked for
/// Wolverine discovery (F6 — see Program.cs's DisableConventionalDiscovery comment).
/// </summary>
/// <remarks>
/// A44: Wolverine's <c>AutoApplyTransactions()</c> (Program.cs) groups every handler of the same
/// message type into one chain when resolving the transactional DbContext — <c>TutorUnpublished</c>
/// also has a <c>Search.TutorUnpublishedConsumer</c> subscriber backed by <c>SearchDbContext</c>,
/// so even though this handler alone only ever reaches <see cref="AvailabilityDbContext"/> (via
/// <see cref="TutorBookabilityProjector"/>), the pair is ambiguous without an explicit
/// designation.
/// <para>
/// A44 topology fix: Lightweight mode (not the sibling <c>TutorPublishedConsumer</c>'s Eager) —
/// unlike that sibling, this handler never publishes an outgoing message, so there is no
/// downstream event that a non-atomic commit could lose. Flipping <c>isBookable</c> to
/// <c>false</c> is naturally idempotent under at-least-once redelivery on its own, independent
/// of transaction mode, and this handler's connection does not need to reach the Main store's
/// physical database for that write to be safe.
/// </para>
/// </remarks>
[WolverineHandler]
[Transactional(typeof(AvailabilityDbContext), Mode = TransactionMiddlewareMode.Lightweight)]
public sealed class TutorUnpublishedConsumer(TutorBookabilityProjector projector)
{
    public Task HandleAsync(TutorUnpublished message, CancellationToken ct) =>
        projector.ApplyBookabilityChangedAsync(message.TutorUserId, isBookable: false, message.OccurredAtUtc, ct);
}

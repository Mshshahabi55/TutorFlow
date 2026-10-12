using TutorFlow.Modules.Availability.Infrastructure;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Availability.Consumers;

/// <summary>
/// Flips a suspended tutor's bookability back to <c>false</c> (ADR-0024) — without this,
/// <c>ITutorAvailabilityQuery</c>/<c>GetTutorSlots</c> would keep treating a suspended tutor as
/// bookable, since nothing else in this module reacts to suspension.
/// </summary>
public sealed class TutorUnpublishedConsumer(TutorBookabilityProjector projector)
{
    public Task HandleAsync(TutorUnpublished message, CancellationToken ct) =>
        projector.ApplyBookabilityChangedAsync(message.TutorUserId, isBookable: false, ct);
}

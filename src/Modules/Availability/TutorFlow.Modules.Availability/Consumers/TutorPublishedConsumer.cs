using TutorFlow.Modules.Availability.Infrastructure;
using TutorFlow.Shared;

namespace TutorFlow.Modules.Availability.Consumers;

/// <summary>
/// Wolverine discovers this by convention (a public <c>HandleAsync</c> instance method), same as
/// Search's own <c>TutorPublishedConsumer</c> — the second subscriber to this event (ADR-0011's
/// original design, wired for the first time here, ADR-0024). Gives
/// <see cref="TutorBookabilityProjector"/> its first real caller: a newly-published tutor becomes
/// bookable, then the availability signal is computed and published for Search to pick up.
/// </summary>
public sealed class TutorPublishedConsumer(TutorBookabilityProjector projector, AvailabilitySignalPublisher signalPublisher)
{
    public async Task HandleAsync(TutorPublished message, CancellationToken ct)
    {
        await projector.ApplyBookabilityChangedAsync(message.TutorUserId, isBookable: true, ct);
        await signalPublisher.PublishCurrentSignalAsync(message.TutorUserId, ct);
    }
}

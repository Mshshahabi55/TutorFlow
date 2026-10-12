using TutorFlow.Shared;
using Wolverine;
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

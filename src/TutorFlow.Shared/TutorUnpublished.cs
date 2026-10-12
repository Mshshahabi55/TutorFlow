namespace TutorFlow.Shared;

/// <summary>
/// Published by <c>Tutors</c> when a published <c>TutorApplication</c> is suspended (ADR-0024) —
/// the de-listing counterpart to <c>TutorPublished</c>. <c>Search</c> consumes it to set
/// <c>TutorSearchDocument.IsPublished = false</c> so the tutor stops appearing in results;
/// <c>Availability</c> consumes it to flip its own bookability read model back to not-bookable,
/// so a suspended tutor cannot still be booked via <c>GetTutorSlots</c>/<c>ITutorAvailabilityQuery</c>.
/// </summary>
public sealed record TutorUnpublished(Guid TutorUserId, DateTimeOffset OccurredAtUtc) : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
}

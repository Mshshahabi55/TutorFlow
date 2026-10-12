namespace TutorFlow.Shared;

/// <summary>
/// Published by <c>Availability</c> whenever its coarse "has an upcoming open slot" signal for a
/// tutor changes — initially on consuming <c>TutorPublished</c>, and again after any self-service
/// change to the tutor's own weekly rules, time off, or booking settings (ADR-0024). Reuses the
/// exact same computation the Tutor Profile page already shows to guests
/// (<c>GetTutorAvailabilityPreview</c>) — this is a relay of that real signal, not a separate one.
/// <c>Search</c> consumes it to keep <c>TutorSearchDocument.HasUpcomingAvailability</c> current.
/// </summary>
public sealed record TutorAvailabilitySignalChanged(
    Guid TutorUserId,
    bool HasUpcomingAvailability,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
}

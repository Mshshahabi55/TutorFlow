using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Meetings.Interfaces;

// docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md: the only
// shapes IMeetingProvider ever exchanges with Application — no provider SDK
// type (Google/Microsoft/Zoom) crosses this boundary.
public sealed record CreateProviderMeetingRequest(SessionId SessionId, DateTime StartsAtUtc, DateTime EndsAtUtc);

public sealed record UpdateProviderMeetingRequest(DateTime StartsAtUtc, DateTime EndsAtUtc);

public sealed record ProviderMeetingResult(
    string ProviderMeetingId,
    string JoinUrl,
    string? HostUrl,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc);

using TutorFlow.Domain.Meetings.ValueObjects;

namespace TutorFlow.Application.Meetings.Interfaces;

// docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md: the only
// abstraction the Application layer depends on for online-lesson delivery.
// Every real adapter (GoogleMeetProvider, MicrosoftTeamsProvider,
// ZoomProvider) and the Development-only MockMeetingProvider live entirely
// in Infrastructure — no provider SDK type is ever referenced here or in
// any Application handler. Adding a future provider (Cisco Webex, Jitsi
// Meet, BigBlueButton, ...) means one new class implementing this
// interface, one DI registration, and one configuration section — never a
// change to this interface or to any Application handler.
public interface IMeetingProvider
{
    MeetingProviderOption Provider { get; }

    Task<ProviderMeetingResult> CreateMeetingAsync(
        CreateProviderMeetingRequest request, CancellationToken cancellationToken = default);

    Task<ProviderMeetingResult> UpdateMeetingAsync(
        string providerMeetingId, UpdateProviderMeetingRequest request, CancellationToken cancellationToken = default);

    Task CancelMeetingAsync(string providerMeetingId, CancellationToken cancellationToken = default);

    Task<ProviderMeetingResult?> GetMeetingAsync(string providerMeetingId, CancellationToken cancellationToken = default);

    // Separate from Create/Get because some real providers issue
    // per-participant signed join tokens that must be freshly generated
    // rather than reused verbatim — Google Meet/Microsoft Teams/Zoom's own
    // implementations here return storedJoinUrl unchanged; the seam exists
    // for a future provider that needs to do more.
    string GenerateJoinLink(string providerMeetingId, string storedJoinUrl);
}

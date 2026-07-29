namespace TutorFlow.Domain.Meetings.ValueObjects;

// docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md: the
// closed set of meeting providers TutorFlow orchestrates today. Adding a
// future provider (Cisco Webex, Jitsi Meet, BigBlueButton, ...) means one
// new value here, one new Infrastructure IMeetingProvider implementation,
// one DI registration, and one configuration section — never a Domain or
// Application change beyond this enum gaining a member.
public enum MeetingProviderOption
{
    GoogleMeet = 0,
    MicrosoftTeams = 1,
    Zoom = 2,

    // Development-only (Infrastructure/DependencyInjection.cs registers
    // MockMeetingProvider under this key only when
    // ASPNETCORE_ENVIRONMENT=Development) — never a legitimate
    // DefaultProvider value outside that environment.
    Mock = 3,
}

using TutorFlow.Domain.Meetings.ValueObjects;

namespace TutorFlow.Infrastructure.Meetings.Configuration;

// Bound from the "Meeting" configuration section (appsettings/user-secrets/
// environment variables — docs/adr/ADR-023-...). Every credential-shaped
// value below is empty by default; appsettings.json never carries a real
// one (CLAUDE.md: "never commit secrets").
public sealed class MeetingProviderSettings
{
    public const string SectionName = "Meeting";

    public MeetingProviderOption DefaultProvider { get; init; } = MeetingProviderOption.GoogleMeet;

    public GoogleMeetSettings Google { get; init; } = new();

    public MicrosoftTeamsSettings Microsoft { get; init; } = new();

    public ZoomSettings Zoom { get; init; } = new();
}

// Google Calendar API's events.insert with conferenceData.createRequest is
// how a Google Meet link is actually generated server-side — there is no
// standalone "create a Meet link" endpoint. RefreshToken is the realistic
// server-side pattern (one-time OAuth consent during setup, refreshed
// automatically thereafter) rather than a service-account JWT flow, which
// needs domain-wide delegation an Admin/Staff operator is far less likely
// to have already set up.
public sealed class GoogleMeetSettings
{
    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;

    public string CalendarId { get; init; } = "primary";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret) && !string.IsNullOrWhiteSpace(RefreshToken);
}

// Microsoft Graph's /users/{id}/onlineMeetings, application (client
// credentials) permissions — OrganizerUserId names which user's mailbox
// the meeting is created under, since app-only Graph calls have no signed-in
// user of their own.
public sealed class MicrosoftTeamsSettings
{
    public string TenantId { get; init; } = string.Empty;

    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    public string OrganizerUserId { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(TenantId) && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret) && !string.IsNullOrWhiteSpace(OrganizerUserId);
}

// Zoom's Server-to-Server OAuth app model (account_credentials grant) —
// the current, non-deprecated way to call Zoom's REST API without a
// per-user OAuth consent flow.
public sealed class ZoomSettings
{
    public string AccountId { get; init; } = string.Empty;

    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AccountId) && !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}

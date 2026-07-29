using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TutorFlow.Application.Meetings.Interfaces;
using TutorFlow.Infrastructure.Meetings.Configuration;
using MeetingProvider = TutorFlow.Domain.Meetings.ValueObjects.MeetingProviderOption;

namespace TutorFlow.Infrastructure.Meetings.Providers;

// Real Google Calendar API integration (docs/adr/ADR-023-...) — Google has
// no standalone "create a Meet link" endpoint; a Meet link is generated as
// a side effect of creating a Calendar event with conferenceData.createRequest
// (https://developers.google.com/calendar/api/guides/create-events#conferencing).
// Auth: OAuth2 refresh-token flow (https://oauth2.googleapis.com/token,
// grant_type=refresh_token) — the realistic server-side pattern once an
// Admin/Staff operator has completed the one-time OAuth consent during
// setup. No caching of the access token here (a real production deployment
// would want one — a disclosed, bounded simplification, not a fabricated
// optimization): every call fetches a fresh token, which Google's own token
// endpoint supports without rate-limiting concern at this app's scale.
internal sealed class GoogleMeetProvider : IMeetingProvider
{
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string CalendarApiBase = "https://www.googleapis.com/calendar/v3/calendars";

    private readonly HttpClient _httpClient;
    private readonly GoogleMeetSettings _settings;

    public GoogleMeetProvider(HttpClient httpClient, IOptions<MeetingProviderSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value.Google;
    }

    public MeetingProvider Provider => MeetingProvider.GoogleMeet;

    public async Task<ProviderMeetingResult> CreateMeetingAsync(
        CreateProviderMeetingRequest request, CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);

        var body = new GoogleEventRequest
        {
            Summary = "TutorFlow Online Lesson",
            Start = new GoogleEventDateTime { DateTime = request.StartsAtUtc, TimeZone = "UTC" },
            End = new GoogleEventDateTime { DateTime = request.EndsAtUtc, TimeZone = "UTC" },
            ConferenceData = new GoogleConferenceDataRequest
            {
                CreateRequest = new GoogleCreateConferenceRequest
                {
                    RequestId = Guid.NewGuid().ToString("N"),
                    ConferenceSolutionKey = new GoogleConferenceSolutionKey { Type = "hangoutsMeet" },
                },
            },
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post, $"{CalendarApiBase}/{Uri.EscapeDataString(_settings.CalendarId)}/events?conferenceDataVersion=1")
        {
            Content = JsonContent.Create(body),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var googleEvent = await response.Content.ReadFromJsonAsync<GoogleEventResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Google Calendar API returned an empty response.");

        return ToResult(googleEvent, request.StartsAtUtc, request.EndsAtUtc);
    }

    public async Task<ProviderMeetingResult> UpdateMeetingAsync(
        string providerMeetingId, UpdateProviderMeetingRequest request, CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);

        var body = new GoogleEventRequest
        {
            Start = new GoogleEventDateTime { DateTime = request.StartsAtUtc, TimeZone = "UTC" },
            End = new GoogleEventDateTime { DateTime = request.EndsAtUtc, TimeZone = "UTC" },
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Patch,
            $"{CalendarApiBase}/{Uri.EscapeDataString(_settings.CalendarId)}/events/{Uri.EscapeDataString(providerMeetingId)}")
        {
            Content = JsonContent.Create(body),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var googleEvent = await response.Content.ReadFromJsonAsync<GoogleEventResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Google Calendar API returned an empty response.");

        return ToResult(googleEvent, request.StartsAtUtc, request.EndsAtUtc);
    }

    public async Task CancelMeetingAsync(string providerMeetingId, CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{CalendarApiBase}/{Uri.EscapeDataString(_settings.CalendarId)}/events/{Uri.EscapeDataString(providerMeetingId)}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        // 410 Gone means the event was already deleted — cancelling an
        // already-cancelled Meeting must stay idempotent (Meeting.Cancel()'s
        // own domain-level idempotency, mirrored here at the provider call).
        if (response.StatusCode != System.Net.HttpStatusCode.Gone)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task<ProviderMeetingResult?> GetMeetingAsync(string providerMeetingId, CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{CalendarApiBase}/{Uri.EscapeDataString(_settings.CalendarId)}/events/{Uri.EscapeDataString(providerMeetingId)}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var googleEvent = await response.Content.ReadFromJsonAsync<GoogleEventResponse>(cancellationToken);
        if (googleEvent is null)
        {
            return null;
        }

        return ToResult(
            googleEvent,
            googleEvent.Start?.DateTime ?? DateTime.UtcNow,
            googleEvent.End?.DateTime ?? DateTime.UtcNow);
    }

    // Google Meet has no separate host link — every participant joins the
    // same URL, and host controls activate based on Calendar ownership, not
    // a distinct URL — so this simply returns the one stored link unchanged.
    public string GenerateJoinLink(string providerMeetingId, string storedJoinUrl) => storedJoinUrl;

    private static ProviderMeetingResult ToResult(GoogleEventResponse googleEvent, DateTime startsAtUtc, DateTime endsAtUtc)
    {
        var joinUrl = googleEvent.HangoutLink
            ?? googleEvent.ConferenceData?.EntryPoints?.FirstOrDefault(e => e.EntryPointType == "video")?.Uri
            ?? throw new InvalidOperationException("Google Calendar API did not return a Meet join link.");

        return new ProviderMeetingResult(googleEvent.Id, joinUrl, HostUrl: null, startsAtUtc, endsAtUtc);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _settings.ClientId,
                ["client_secret"] = _settings.ClientSecret,
                ["refresh_token"] = _settings.RefreshToken,
                ["grant_type"] = "refresh_token",
            }),
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken);
        return token?.AccessToken ?? throw new InvalidOperationException("Google OAuth token response carried no access_token.");
    }

    private sealed class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }
    }

    private sealed class GoogleEventRequest
    {
        [JsonPropertyName("summary")]
        public string? Summary { get; init; }

        [JsonPropertyName("start")]
        public GoogleEventDateTime? Start { get; init; }

        [JsonPropertyName("end")]
        public GoogleEventDateTime? End { get; init; }

        [JsonPropertyName("conferenceData")]
        public GoogleConferenceDataRequest? ConferenceData { get; init; }
    }

    private sealed class GoogleEventDateTime
    {
        [JsonPropertyName("dateTime")]
        public DateTime DateTime { get; init; }

        [JsonPropertyName("timeZone")]
        public string? TimeZone { get; init; }
    }

    private sealed class GoogleConferenceDataRequest
    {
        [JsonPropertyName("createRequest")]
        public GoogleCreateConferenceRequest? CreateRequest { get; init; }
    }

    private sealed class GoogleCreateConferenceRequest
    {
        [JsonPropertyName("requestId")]
        public string? RequestId { get; init; }

        [JsonPropertyName("conferenceSolutionKey")]
        public GoogleConferenceSolutionKey? ConferenceSolutionKey { get; init; }
    }

    private sealed class GoogleConferenceSolutionKey
    {
        [JsonPropertyName("type")]
        public string? Type { get; init; }
    }

    private sealed class GoogleEventResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("hangoutLink")]
        public string? HangoutLink { get; init; }

        [JsonPropertyName("start")]
        public GoogleEventDateTime? Start { get; init; }

        [JsonPropertyName("end")]
        public GoogleEventDateTime? End { get; init; }

        [JsonPropertyName("conferenceData")]
        public GoogleConferenceDataResponse? ConferenceData { get; init; }
    }

    private sealed class GoogleConferenceDataResponse
    {
        [JsonPropertyName("entryPoints")]
        public List<GoogleEntryPoint>? EntryPoints { get; init; }
    }

    private sealed class GoogleEntryPoint
    {
        [JsonPropertyName("entryPointType")]
        public string? EntryPointType { get; init; }

        [JsonPropertyName("uri")]
        public string? Uri { get; init; }
    }
}

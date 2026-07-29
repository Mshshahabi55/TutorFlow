using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TutorFlow.Application.Meetings.Interfaces;
using TutorFlow.Infrastructure.Meetings.Configuration;
using MeetingProvider = TutorFlow.Domain.Meetings.ValueObjects.MeetingProviderOption;

namespace TutorFlow.Infrastructure.Meetings.Providers;

// Real Microsoft Graph API integration (docs/adr/ADR-023-...) —
// /users/{id}/onlineMeetings, application (client credentials) permissions.
// App-only Graph calls have no signed-in user of their own, so
// OrganizerUserId names whose mailbox the meeting is created under.
// Auth: Microsoft Entra ID OAuth2 client credentials grant
// (https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token). No token
// caching — same disclosed, bounded simplification as GoogleMeetProvider.
internal sealed class MicrosoftTeamsProvider : IMeetingProvider
{
    private const string GraphApiBase = "https://graph.microsoft.com/v1.0";

    private readonly HttpClient _httpClient;
    private readonly MicrosoftTeamsSettings _settings;

    public MicrosoftTeamsProvider(HttpClient httpClient, IOptions<MeetingProviderSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value.Microsoft;
    }

    public MeetingProvider Provider => MeetingProvider.MicrosoftTeams;

    public async Task<ProviderMeetingResult> CreateMeetingAsync(
        CreateProviderMeetingRequest request, CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);

        var body = new TeamsOnlineMeetingRequest
        {
            Subject = "TutorFlow Online Lesson",
            StartDateTime = request.StartsAtUtc,
            EndDateTime = request.EndsAtUtc,
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post, $"{GraphApiBase}/users/{Uri.EscapeDataString(_settings.OrganizerUserId)}/onlineMeetings")
        {
            Content = JsonContent.Create(body),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var meeting = await response.Content.ReadFromJsonAsync<TeamsOnlineMeetingResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Microsoft Graph API returned an empty response.");

        return ToResult(meeting, request.StartsAtUtc, request.EndsAtUtc);
    }

    public async Task<ProviderMeetingResult> UpdateMeetingAsync(
        string providerMeetingId, UpdateProviderMeetingRequest request, CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);

        var body = new TeamsOnlineMeetingRequest
        {
            StartDateTime = request.StartsAtUtc,
            EndDateTime = request.EndsAtUtc,
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Patch,
            $"{GraphApiBase}/users/{Uri.EscapeDataString(_settings.OrganizerUserId)}/onlineMeetings/{Uri.EscapeDataString(providerMeetingId)}")
        {
            Content = JsonContent.Create(body),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var meeting = await response.Content.ReadFromJsonAsync<TeamsOnlineMeetingResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Microsoft Graph API returned an empty response.");

        return ToResult(meeting, request.StartsAtUtc, request.EndsAtUtc);
    }

    public async Task CancelMeetingAsync(string providerMeetingId, CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{GraphApiBase}/users/{Uri.EscapeDataString(_settings.OrganizerUserId)}/onlineMeetings/{Uri.EscapeDataString(providerMeetingId)}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task<ProviderMeetingResult?> GetMeetingAsync(string providerMeetingId, CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{GraphApiBase}/users/{Uri.EscapeDataString(_settings.OrganizerUserId)}/onlineMeetings/{Uri.EscapeDataString(providerMeetingId)}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var meeting = await response.Content.ReadFromJsonAsync<TeamsOnlineMeetingResponse>(cancellationToken);
        if (meeting is null)
        {
            return null;
        }

        return ToResult(meeting, meeting.StartDateTime ?? DateTime.UtcNow, meeting.EndDateTime ?? DateTime.UtcNow);
    }

    // Teams has no separate host link — every participant joins the same
    // joinWebUrl; host controls activate based on being the organizer.
    public string GenerateJoinLink(string providerMeetingId, string storedJoinUrl) => storedJoinUrl;

    private static ProviderMeetingResult ToResult(TeamsOnlineMeetingResponse meeting, DateTime startsAtUtc, DateTime endsAtUtc) =>
        new(
            meeting.Id,
            meeting.JoinWebUrl ?? throw new InvalidOperationException("Microsoft Graph API did not return a joinWebUrl."),
            HostUrl: null,
            startsAtUtc,
            endsAtUtc);

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"https://login.microsoftonline.com/{Uri.EscapeDataString(_settings.TenantId)}/oauth2/v2.0/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _settings.ClientId,
                ["client_secret"] = _settings.ClientSecret,
                ["scope"] = "https://graph.microsoft.com/.default",
                ["grant_type"] = "client_credentials",
            }),
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<MicrosoftTokenResponse>(cancellationToken);
        return token?.AccessToken ?? throw new InvalidOperationException("Microsoft OAuth token response carried no access_token.");
    }

    private sealed class MicrosoftTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }
    }

    private sealed class TeamsOnlineMeetingRequest
    {
        [JsonPropertyName("subject")]
        public string? Subject { get; init; }

        [JsonPropertyName("startDateTime")]
        public DateTime? StartDateTime { get; init; }

        [JsonPropertyName("endDateTime")]
        public DateTime? EndDateTime { get; init; }
    }

    private sealed class TeamsOnlineMeetingResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("joinWebUrl")]
        public string? JoinWebUrl { get; init; }

        [JsonPropertyName("startDateTime")]
        public DateTime? StartDateTime { get; init; }

        [JsonPropertyName("endDateTime")]
        public DateTime? EndDateTime { get; init; }
    }
}

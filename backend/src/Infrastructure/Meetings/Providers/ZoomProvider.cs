using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TutorFlow.Application.Meetings.Interfaces;
using TutorFlow.Infrastructure.Meetings.Configuration;
using MeetingProvider = TutorFlow.Domain.Meetings.ValueObjects.MeetingProviderOption;

namespace TutorFlow.Infrastructure.Meetings.Providers;

// Real Zoom REST API integration (docs/adr/ADR-023-...) — Server-to-Server
// OAuth (account_credentials grant), Zoom's current, non-deprecated app
// model for calling its API without a per-user consent flow. Zoom is the
// one provider of the three that genuinely distinguishes a host link
// (start_url) from the join link (join_url) — unlike Google Meet/Microsoft
// Teams, where every participant uses the same URL. No token caching —
// same disclosed, bounded simplification as the other two providers.
internal sealed class ZoomProvider : IMeetingProvider
{
    private const string TokenEndpoint = "https://zoom.us/oauth/token";
    private const string ApiBase = "https://api.zoom.us/v2";

    private readonly HttpClient _httpClient;
    private readonly ZoomSettings _settings;

    public ZoomProvider(HttpClient httpClient, IOptions<MeetingProviderSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value.Zoom;
    }

    public MeetingProvider Provider => MeetingProvider.Zoom;

    public async Task<ProviderMeetingResult> CreateMeetingAsync(
        CreateProviderMeetingRequest request, CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);

        var body = new ZoomMeetingRequest
        {
            Topic = "TutorFlow Online Lesson",
            Type = 2, // Scheduled meeting
            StartTime = request.StartsAtUtc,
            Duration = (int)Math.Ceiling((request.EndsAtUtc - request.StartsAtUtc).TotalMinutes),
            Timezone = "UTC",
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/users/me/meetings")
        {
            Content = JsonContent.Create(body),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var meeting = await response.Content.ReadFromJsonAsync<ZoomMeetingResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Zoom API returned an empty response.");

        return ToResult(meeting, request.StartsAtUtc, request.EndsAtUtc);
    }

    public async Task<ProviderMeetingResult> UpdateMeetingAsync(
        string providerMeetingId, UpdateProviderMeetingRequest request, CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);

        var body = new ZoomMeetingRequest
        {
            StartTime = request.StartsAtUtc,
            Duration = (int)Math.Ceiling((request.EndsAtUtc - request.StartsAtUtc).TotalMinutes),
            Timezone = "UTC",
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Patch, $"{ApiBase}/meetings/{Uri.EscapeDataString(providerMeetingId)}")
        {
            Content = JsonContent.Create(body),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Zoom's PATCH /meetings/{id} returns 204 No Content on success —
        // the caller already knows the new times it asked for, so those
        // (not a re-fetched response body) are what's reflected back.
        return new ProviderMeetingResult(providerMeetingId, string.Empty, null, request.StartsAtUtc, request.EndsAtUtc);
    }

    public async Task CancelMeetingAsync(string providerMeetingId, CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Delete, $"{ApiBase}/meetings/{Uri.EscapeDataString(providerMeetingId)}");
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
            HttpMethod.Get, $"{ApiBase}/meetings/{Uri.EscapeDataString(providerMeetingId)}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var meeting = await response.Content.ReadFromJsonAsync<ZoomMeetingResponse>(cancellationToken);
        if (meeting is null)
        {
            return null;
        }

        return ToResult(meeting, meeting.StartTime ?? DateTime.UtcNow, (meeting.StartTime ?? DateTime.UtcNow).AddMinutes(meeting.Duration ?? 60));
    }

    // Zoom does not issue per-participant signed join tokens for a basic
    // join_url — the stored link is reused unchanged.
    public string GenerateJoinLink(string providerMeetingId, string storedJoinUrl) => storedJoinUrl;

    private static ProviderMeetingResult ToResult(ZoomMeetingResponse meeting, DateTime startsAtUtc, DateTime endsAtUtc) =>
        new(
            meeting.Id.ToString(),
            meeting.JoinUrl ?? throw new InvalidOperationException("Zoom API did not return a join_url."),
            meeting.StartUrl,
            startsAtUtc,
            endsAtUtc);

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{TokenEndpoint}?grant_type=account_credentials&account_id={Uri.EscapeDataString(_settings.AccountId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}")));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<ZoomTokenResponse>(cancellationToken);
        return token?.AccessToken ?? throw new InvalidOperationException("Zoom OAuth token response carried no access_token.");
    }

    private sealed class ZoomTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }
    }

    private sealed class ZoomMeetingRequest
    {
        [JsonPropertyName("topic")]
        public string? Topic { get; init; }

        [JsonPropertyName("type")]
        public int? Type { get; init; }

        [JsonPropertyName("start_time")]
        public DateTime? StartTime { get; init; }

        [JsonPropertyName("duration")]
        public int? Duration { get; init; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; init; }
    }

    private sealed class ZoomMeetingResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }

        [JsonPropertyName("join_url")]
        public string? JoinUrl { get; init; }

        [JsonPropertyName("start_url")]
        public string? StartUrl { get; init; }

        [JsonPropertyName("start_time")]
        public DateTime? StartTime { get; init; }

        [JsonPropertyName("duration")]
        public int? Duration { get; init; }
    }
}

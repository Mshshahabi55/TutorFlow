using TutorFlow.Application.Meetings.Interfaces;
using TutorFlow.Domain.Meetings.ValueObjects;

namespace TutorFlow.Infrastructure.Meetings.Providers;

// Development-only (registered only when ASPNETCORE_ENVIRONMENT=Development,
// mirroring DevelopmentSeeder's own environment guard — see
// DependencyInjection.cs). Never claims to be a real provider: every link
// it returns is obviously and permanently fake
// ("mock-meeting.tutorflow.dev"), so it can never be mistaken for a real
// Google Meet/Microsoft Teams/Zoom join link in a screenshot or log.
internal sealed class MockMeetingProvider : IMeetingProvider
{
    private const string BaseUrl = "https://mock-meeting.tutorflow.dev";

    public MeetingProviderOption Provider => MeetingProviderOption.Mock;

    public Task<ProviderMeetingResult> CreateMeetingAsync(
        CreateProviderMeetingRequest request, CancellationToken cancellationToken = default)
    {
        var providerMeetingId = Guid.NewGuid().ToString("N");
        return Task.FromResult(new ProviderMeetingResult(
            providerMeetingId,
            $"{BaseUrl}/join/{providerMeetingId}",
            $"{BaseUrl}/host/{providerMeetingId}",
            request.StartsAtUtc,
            request.EndsAtUtc));
    }

    public Task<ProviderMeetingResult> UpdateMeetingAsync(
        string providerMeetingId, UpdateProviderMeetingRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ProviderMeetingResult(
            providerMeetingId,
            $"{BaseUrl}/join/{providerMeetingId}",
            $"{BaseUrl}/host/{providerMeetingId}",
            request.StartsAtUtc,
            request.EndsAtUtc));

    public Task CancelMeetingAsync(string providerMeetingId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<ProviderMeetingResult?> GetMeetingAsync(string providerMeetingId, CancellationToken cancellationToken = default) =>
        Task.FromResult<ProviderMeetingResult?>(new ProviderMeetingResult(
            providerMeetingId, $"{BaseUrl}/join/{providerMeetingId}", $"{BaseUrl}/host/{providerMeetingId}",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(1)));

    public string GenerateJoinLink(string providerMeetingId, string storedJoinUrl) => storedJoinUrl;
}

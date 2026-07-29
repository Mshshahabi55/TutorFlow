using TutorFlow.Application.Meetings.Interfaces;
using TutorFlow.Domain.Meetings.ValueObjects;

namespace TutorFlow.Application.Tests.TestDoubles;

// A hand-rolled IMeetingProvider double — proves CreateMeetingCommandHandler
// depends only on the interface (docs/adr/ADR-023-...), never a real
// provider SDK, matching this project's no-mocking-framework convention.
internal sealed class FakeMeetingProvider : IMeetingProvider
{
    public MeetingProviderOption Provider { get; init; } = MeetingProviderOption.GoogleMeet;

    public bool ThrowOnCreate { get; init; }

    public int CreateCallCount { get; private set; }

    public Task<ProviderMeetingResult> CreateMeetingAsync(
        CreateProviderMeetingRequest request, CancellationToken cancellationToken = default)
    {
        CreateCallCount++;
        if (ThrowOnCreate)
        {
            throw new InvalidOperationException("Simulated provider failure.");
        }

        return Task.FromResult(new ProviderMeetingResult(
            $"provider-meeting-{CreateCallCount}",
            "https://meet.example.com/join/fake",
            "https://meet.example.com/host/fake",
            request.StartsAtUtc,
            request.EndsAtUtc));
    }

    public Task<ProviderMeetingResult> UpdateMeetingAsync(
        string providerMeetingId, UpdateProviderMeetingRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ProviderMeetingResult(
            providerMeetingId, "https://meet.example.com/join/fake", "https://meet.example.com/host/fake",
            request.StartsAtUtc, request.EndsAtUtc));

    public Task CancelMeetingAsync(string providerMeetingId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<ProviderMeetingResult?> GetMeetingAsync(string providerMeetingId, CancellationToken cancellationToken = default) =>
        Task.FromResult<ProviderMeetingResult?>(null);

    public string GenerateJoinLink(string providerMeetingId, string storedJoinUrl) => storedJoinUrl;
}

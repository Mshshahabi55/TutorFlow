using TutorFlow.Application.Meetings.Interfaces;
using TutorFlow.Domain.Meetings.ValueObjects;

namespace TutorFlow.Application.Tests.TestDoubles;

internal sealed class FakeMeetingProviderSettingsCatalog : IMeetingProviderSettingsCatalog
{
    public MeetingProviderOption DefaultProvider { get; init; } = MeetingProviderOption.GoogleMeet;

    public bool Configured { get; init; } = true;

    public bool IsConfigured(MeetingProviderOption provider) => Configured;
}

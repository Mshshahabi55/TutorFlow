using TutorFlow.Application.Meetings.Interfaces;
using TutorFlow.Domain.Meetings.ValueObjects;

namespace TutorFlow.Application.Tests.TestDoubles;

internal sealed class FakeMeetingProviderResolver : IMeetingProviderResolver
{
    private readonly FakeMeetingProvider _provider;

    public FakeMeetingProviderResolver(FakeMeetingProvider provider) => _provider = provider;

    public IMeetingProvider Resolve(MeetingProviderOption provider) => _provider;
}

using TutorFlow.Domain.Meetings.ValueObjects;

namespace TutorFlow.Application.Meetings.Interfaces;

// Resolves which IMeetingProvider implementation to call for a given
// provider selection — Infrastructure implements this using keyed DI;
// Application never touches the DI container directly.
public interface IMeetingProviderResolver
{
    IMeetingProvider Resolve(MeetingProviderOption provider);
}

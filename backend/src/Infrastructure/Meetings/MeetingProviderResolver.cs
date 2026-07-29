using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Application.Meetings.Interfaces;
using TutorFlow.Domain.Meetings.ValueObjects;

namespace TutorFlow.Infrastructure.Meetings;

// The only place that ever calls GetRequiredKeyedService — Application
// never touches the DI container directly (docs/adr/ADR-023-...).
internal sealed class MeetingProviderResolver : IMeetingProviderResolver
{
    private readonly IServiceProvider _serviceProvider;

    public MeetingProviderResolver(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public IMeetingProvider Resolve(MeetingProviderOption provider) =>
        _serviceProvider.GetRequiredKeyedService<IMeetingProvider>(provider);
}

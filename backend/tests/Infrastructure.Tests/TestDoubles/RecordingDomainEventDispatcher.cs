using TutorFlow.Application.Common;
using TutorFlow.Domain.Common;

namespace TutorFlow.Infrastructure.Tests.TestDoubles;

// Test-only instrumentation — records what it was called with so tests can
// assert the integration point's wiring. It is not a production listener
// and embodies no delivery policy: it does not invoke anything else, retry,
// or reorder what it is given.
internal sealed class RecordingDomainEventDispatcher : IDomainEventDispatcher
{
    public int CallCount { get; private set; }

    public IReadOnlyCollection<IAggregateRoot>? LastTouchedAggregates { get; private set; }

    public Task DispatchAsync(IReadOnlyCollection<IAggregateRoot> touchedAggregates, CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastTouchedAggregates = touchedAggregates;
        return Task.CompletedTask;
    }
}

using TutorFlow.Application.Common;
using TutorFlow.Domain.Common;
using TutorFlow.Domain.Identity;
using TutorFlow.Infrastructure.Common;

namespace TutorFlow.Infrastructure.Tests.Common;

public class DomainEventDispatcherTests
{
    private sealed class RecordingHandler : IDomainEventHandler
    {
        public List<IDomainEvent> Received { get; } = new();

        public Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            Received.Add(domainEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : IDomainEventHandler
    {
        public int CallCount { get; private set; }

        public Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            CallCount++;
            throw new InvalidOperationException("Simulated listener failure.");
        }
    }

    [Fact]
    public async Task DispatchAsync_completes_with_zero_registered_handlers()
    {
        var dispatcher = new DomainEventDispatcher(Enumerable.Empty<IDomainEventHandler>());
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        await dispatcher.DispatchAsync(new IAggregateRoot[] { tutor });

        Assert.NotEmpty(tutor.DomainEvents);
    }

    [Fact]
    public async Task DispatchAsync_invokes_every_registered_handler_for_every_raised_event()
    {
        var handlerA = new RecordingHandler();
        var handlerB = new RecordingHandler();
        var dispatcher = new DomainEventDispatcher(new IDomainEventHandler[] { handlerA, handlerB });
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        await dispatcher.DispatchAsync(new IAggregateRoot[] { tutor });

        Assert.Single(handlerA.Received);
        Assert.Single(handlerB.Received);
    }

    [Fact]
    public async Task DispatchAsync_preserves_same_aggregate_event_order()
    {
        var handler = new RecordingHandler();
        var dispatcher = new DomainEventDispatcher(new IDomainEventHandler[] { handler });

        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        tutor.Approve();
        tutor.Suspend();

        await dispatcher.DispatchAsync(new IAggregateRoot[] { tutor });

        Assert.Equal(3, handler.Received.Count);
        Assert.IsType<TutorFlow.Domain.Identity.Events.TutorRegistered>(handler.Received[0]);
        Assert.IsType<TutorFlow.Domain.Identity.Events.TutorApproved>(handler.Received[1]);
        Assert.IsType<TutorFlow.Domain.Identity.Events.TutorSuspended>(handler.Received[2]);
    }

    [Fact]
    public async Task DispatchAsync_propagates_a_handler_failure_instead_of_swallowing_it()
    {
        var dispatcher = new DomainEventDispatcher(new IDomainEventHandler[] { new ThrowingHandler() });
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.DispatchAsync(new IAggregateRoot[] { tutor }));
    }

    [Fact]
    public async Task DispatchAsync_makes_exactly_one_attempt_per_handler_per_event_with_no_retry()
    {
        var throwing = new ThrowingHandler();
        var dispatcher = new DomainEventDispatcher(new IDomainEventHandler[] { throwing });
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.DispatchAsync(new IAggregateRoot[] { tutor }));

        Assert.Equal(1, throwing.CallCount);
    }
}

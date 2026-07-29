using TutorFlow.Application.Oversight.Handlers;
using TutorFlow.Application.Oversight.Queries;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Tests.Oversight;

public class GetSessionStatusCountsQueryHandlerTests
{
    private static Session BookSession() => AvailabilitySlot.Declare(
            TutorId.From(Guid.NewGuid()),
            DateTime.UtcNow.AddDays(1),
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.Online)
        .Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null);

    [Fact]
    public async Task Handle_counts_sessions_by_their_own_status()
    {
        var repository = new InMemorySessionRepository();

        var scheduled = BookSession();
        var completed = BookSession();
        completed.Complete();
        var cancelled = BookSession();
        cancelled.Cancel();
        var noShow = BookSession();
        noShow.MarkNoShow();

        await repository.AddAsync(scheduled);
        await repository.AddAsync(completed);
        await repository.AddAsync(cancelled);
        await repository.AddAsync(noShow);

        var handler = new GetSessionStatusCountsQueryHandler(repository);

        var result = await handler.Handle(new GetSessionStatusCountsQuery());

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Scheduled);
        Assert.Equal(1, result.Value.Completed);
        Assert.Equal(1, result.Value.Cancelled);
        Assert.Equal(1, result.Value.NoShow);
    }

    [Fact]
    public async Task Handle_returns_all_zero_counts_when_no_session_exists()
    {
        var repository = new InMemorySessionRepository();
        var handler = new GetSessionStatusCountsQueryHandler(repository);

        var result = await handler.Handle(new GetSessionStatusCountsQuery());

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Scheduled);
        Assert.Equal(0, result.Value.Completed);
        Assert.Equal(0, result.Value.Cancelled);
        Assert.Equal(0, result.Value.NoShow);
    }

    [Fact]
    public async Task Handle_sums_multiple_sessions_sharing_the_same_status()
    {
        var repository = new InMemorySessionRepository();
        await repository.AddAsync(BookSession());
        await repository.AddAsync(BookSession());
        await repository.AddAsync(BookSession());

        var handler = new GetSessionStatusCountsQueryHandler(repository);

        var result = await handler.Handle(new GetSessionStatusCountsQuery());

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Scheduled);
    }
}

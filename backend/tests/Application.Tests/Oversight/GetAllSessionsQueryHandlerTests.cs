using TutorFlow.Application.Oversight.Handlers;
using TutorFlow.Application.Oversight.Queries;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Tests.Oversight;

public class GetAllSessionsQueryHandlerTests
{
    private static Session BookSession() => AvailabilitySlot.Declare(
            TutorId.From(Guid.NewGuid()),
            DateTime.UtcNow.AddDays(1),
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.Online)
        .Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null);

    [Fact]
    public async Task Handle_returns_every_session_regardless_of_tutor_or_student()
    {
        var repository = new InMemorySessionRepository();
        var sessionA = BookSession();
        var sessionB = BookSession();
        await repository.AddAsync(sessionA);
        await repository.AddAsync(sessionB);
        var handler = new GetAllSessionsQueryHandler(repository);

        var result = await handler.Handle(new GetAllSessionsQuery(1, 20));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Contains(result.Value.Items, s => s.SessionId == sessionA.Id.Value);
        Assert.Contains(result.Value.Items, s => s.SessionId == sessionB.Id.Value);
    }

    [Fact]
    public async Task Handle_returns_empty_collection_when_no_session_exists()
    {
        var repository = new InMemorySessionRepository();
        var handler = new GetAllSessionsQueryHandler(repository);

        var result = await handler.Handle(new GetAllSessionsQuery(1, 20));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_paginates_the_result()
    {
        var repository = new InMemorySessionRepository();
        for (var i = 0; i < 3; i++)
        {
            await repository.AddAsync(BookSession());
        }

        var handler = new GetAllSessionsQueryHandler(repository);

        var result = await handler.Handle(new GetAllSessionsQuery(1, 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);
    }

    [Fact]
    public async Task Handle_returns_failure_for_page_less_than_one()
    {
        var repository = new InMemorySessionRepository();
        var handler = new GetAllSessionsQueryHandler(repository);

        var result = await handler.Handle(new GetAllSessionsQuery(0, 20));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_returns_failure_for_page_size_out_of_range()
    {
        var repository = new InMemorySessionRepository();
        var handler = new GetAllSessionsQueryHandler(repository);

        var result = await handler.Handle(new GetAllSessionsQuery(1, 1000));

        Assert.True(result.IsFailure);
    }
}

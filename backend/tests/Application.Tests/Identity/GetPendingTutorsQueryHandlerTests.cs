using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Identity.Queries;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;

namespace TutorFlow.Application.Tests.Identity;

public class GetPendingTutorsQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_only_unapproved_tutors()
    {
        var repository = new InMemoryTutorRepository();

        var pending = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(pending);

        var approved = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        approved.Approve();
        await repository.AddAsync(approved);

        var suspended = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        suspended.Approve();
        suspended.Suspend();
        await repository.AddAsync(suspended);

        var handler = new GetPendingTutorsQueryHandler(repository);

        var result = await handler.Handle(new GetPendingTutorsQuery(1, 20));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Single(result.Value.Items, t => t.TutorId == pending.Id.Value);
        Assert.DoesNotContain(result.Value.Items, t => t.TutorId == approved.Id.Value);
        Assert.DoesNotContain(result.Value.Items, t => t.TutorId == suspended.Id.Value);
    }

    [Fact]
    public async Task Handle_returns_empty_collection_when_no_tutor_is_pending()
    {
        var repository = new InMemoryTutorRepository();
        var handler = new GetPendingTutorsQueryHandler(repository);

        var result = await handler.Handle(new GetPendingTutorsQuery(1, 20));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_returns_failure_for_page_less_than_one()
    {
        var repository = new InMemoryTutorRepository();
        var handler = new GetPendingTutorsQueryHandler(repository);

        var result = await handler.Handle(new GetPendingTutorsQuery(0, 20));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_returns_failure_for_page_size_out_of_range()
    {
        var repository = new InMemoryTutorRepository();
        var handler = new GetPendingTutorsQueryHandler(repository);

        var result = await handler.Handle(new GetPendingTutorsQuery(1, 1000));

        Assert.True(result.IsFailure);
    }
}

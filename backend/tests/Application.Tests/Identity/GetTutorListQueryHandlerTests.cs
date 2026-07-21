using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Identity.Queries;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;

namespace TutorFlow.Application.Tests.Identity;

public class GetTutorListQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_only_approved_and_not_suspended_tutors()
    {
        var repository = new InMemoryTutorRepository();

        var pending = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(pending);

        var suspended = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        suspended.Approve();
        suspended.Suspend();
        await repository.AddAsync(suspended);

        var discoverable = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        discoverable.Approve();
        await repository.AddAsync(discoverable);

        var handler = new GetTutorListQueryHandler(repository);

        var result = await handler.Handle(new GetTutorListQuery());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value, t => t.TutorId == discoverable.Id.Value);
        Assert.DoesNotContain(result.Value, t => t.TutorId == pending.Id.Value);
        Assert.DoesNotContain(result.Value, t => t.TutorId == suspended.Id.Value);
    }

    [Fact]
    public async Task Handle_returns_empty_collection_when_no_tutor_is_discoverable()
    {
        var repository = new InMemoryTutorRepository();
        var handler = new GetTutorListQueryHandler(repository);

        var result = await handler.Handle(new GetTutorListQuery());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}

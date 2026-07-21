using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Identity.Queries;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;

namespace TutorFlow.Application.Tests.Identity;

public class GetTutorByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_tutor_regardless_of_discoverability_for_admin()
    {
        var repository = new InMemoryTutorRepository();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new GetTutorByIdQueryHandler(repository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetTutorByIdQuery(tutor.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(tutor.Id.Value, result.Value.TutorId);
        Assert.False(result.Value.IsDiscoverable);
    }

    [Fact]
    public async Task Handle_returns_tutor_for_the_tutor_themselves_when_not_discoverable()
    {
        var repository = new InMemoryTutorRepository();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new GetTutorByIdQueryHandler(repository, StubCurrentUserProvider.AsTutor(tutor.Id.Value));

        var result = await handler.Handle(new GetTutorByIdQuery(tutor.Id.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_returns_success_for_an_unauthenticated_caller_when_discoverable()
    {
        var repository = new InMemoryTutorRepository();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        tutor.Approve();
        await repository.AddAsync(tutor);
        var handler = new GetTutorByIdQueryHandler(repository, StubCurrentUserProvider.Unauthenticated());

        var result = await handler.Handle(new GetTutorByIdQuery(tutor.Id.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_returns_unauthenticated_error_for_an_anonymous_caller_when_not_discoverable()
    {
        var repository = new InMemoryTutorRepository();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new GetTutorByIdQueryHandler(repository, StubCurrentUserProvider.Unauthenticated());

        var result = await handler.Handle(new GetTutorByIdQuery(tutor.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
        Assert.Equal("GetTutorByIdQuery.Unauthenticated", result.Error.Code);
    }

    [Fact]
    public async Task Handle_returns_forbidden_for_an_unrelated_caller_when_not_discoverable()
    {
        var repository = new InMemoryTutorRepository();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new GetTutorByIdQueryHandler(repository, StubCurrentUserProvider.AsTutor(Guid.NewGuid()));

        var result = await handler.Handle(new GetTutorByIdQuery(tutor.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
    }

    [Fact]
    public async Task Handle_returns_not_found_for_unknown_tutor()
    {
        var repository = new InMemoryTutorRepository();
        var handler = new GetTutorByIdQueryHandler(repository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetTutorByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_returns_failure_for_empty_id()
    {
        var repository = new InMemoryTutorRepository();
        var handler = new GetTutorByIdQueryHandler(repository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetTutorByIdQuery(Guid.Empty));

        Assert.True(result.IsFailure);
    }
}

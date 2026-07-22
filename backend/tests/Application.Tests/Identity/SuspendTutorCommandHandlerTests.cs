using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;

namespace TutorFlow.Application.Tests.Identity;

public class SuspendTutorCommandHandlerTests
{
    [Fact]
    public async Task Handle_suspends_existing_tutor_and_calls_SaveChanges()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        tutor.Approve();
        await repository.AddAsync(tutor);
        var handler = new SuspendTutorCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(new SuspendTutorCommand(tutor.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_failure_when_tutor_not_found()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new SuspendTutorCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(new SuspendTutorCommand(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_translates_domain_error_into_failure()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        tutor.Approve();
        tutor.Suspend();
        await repository.AddAsync(tutor);
        var handler = new SuspendTutorCommandHandler(repository, unitOfWork);

        var result = await handler.Handle(new SuspendTutorCommand(tutor.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}

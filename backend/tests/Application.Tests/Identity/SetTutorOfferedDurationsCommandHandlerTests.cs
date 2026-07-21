using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;

namespace TutorFlow.Application.Tests.Identity;

public class SetTutorOfferedDurationsCommandHandlerTests
{
    [Fact]
    public async Task Handle_sets_the_offered_durations_and_saves()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new SetTutorOfferedDurationsCommandHandler(repository, StubCurrentUserProvider.AsTutor(tutor.Id.Value), unitOfWork);
        var durations = new[] { TimeSpan.FromMinutes(30), TimeSpan.FromHours(1) };

        var result = await handler.Handle(new SetTutorOfferedDurationsCommand(tutor.Id.Value, durations));

        Assert.True(result.IsSuccess);
        Assert.Equal(durations, tutor.OfferedDurations);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_failure_when_tutor_not_found()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var targetId = Guid.NewGuid();
        var handler = new SetTutorOfferedDurationsCommandHandler(repository, StubCurrentUserProvider.AsTutor(targetId), unitOfWork);

        var result = await handler.Handle(
            new SetTutorOfferedDurationsCommand(targetId, new[] { TimeSpan.FromHours(1) }));

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_failure_for_empty_durations()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new SetTutorOfferedDurationsCommandHandler(repository, StubCurrentUserProvider.AsTutor(tutor.Id.Value), unitOfWork);

        var result = await handler.Handle(new SetTutorOfferedDurationsCommand(tutor.Id.Value, Array.Empty<TimeSpan>()));

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_failure_for_a_non_positive_duration()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new SetTutorOfferedDurationsCommandHandler(repository, StubCurrentUserProvider.AsTutor(tutor.Id.Value), unitOfWork);

        var result = await handler.Handle(
            new SetTutorOfferedDurationsCommand(tutor.Id.Value, new[] { TimeSpan.Zero }));

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_forbidden_when_caller_is_not_the_tutor()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new SetTutorOfferedDurationsCommandHandler(repository, StubCurrentUserProvider.AsTutor(Guid.NewGuid()), unitOfWork);

        var result = await handler.Handle(
            new SetTutorOfferedDurationsCommand(tutor.Id.Value, new[] { TimeSpan.FromHours(1) }));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}

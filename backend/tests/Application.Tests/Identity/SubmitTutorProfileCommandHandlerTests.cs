using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Tests.Identity;

public class SubmitTutorProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_submits_a_complete_profile_and_calls_SaveChanges()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        tutor.SetSubject(Subject.Of("Mathematics"));
        tutor.SetHourlyRate(HourlyRate.Of(500_000m));
        await repository.AddAsync(tutor);
        var handler = new SubmitTutorProfileCommandHandler(repository, StubCurrentUserProvider.AsTutor(tutor.Id.Value), unitOfWork);

        var result = await handler.Handle(new SubmitTutorProfileCommand(tutor.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(TutorProfileStatus.Submitted, tutor.ProfileStatus);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_failure_for_an_incomplete_profile()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new SubmitTutorProfileCommandHandler(repository, StubCurrentUserProvider.AsTutor(tutor.Id.Value), unitOfWork);

        var result = await handler.Handle(new SubmitTutorProfileCommand(tutor.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal("SubmitTutorProfileCommand.Invalid", result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_forbidden_when_caller_is_not_the_tutor()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        tutor.SetSubject(Subject.Of("Mathematics"));
        tutor.SetHourlyRate(HourlyRate.Of(500_000m));
        await repository.AddAsync(tutor);
        var handler = new SubmitTutorProfileCommandHandler(repository, StubCurrentUserProvider.AsTutor(Guid.NewGuid()), unitOfWork);

        var result = await handler.Handle(new SubmitTutorProfileCommand(tutor.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}

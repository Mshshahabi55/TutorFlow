using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;

namespace TutorFlow.Application.Tests.Identity;

public class SetTutorPricingCommandHandlerTests
{
    [Fact]
    public async Task Handle_sets_hourly_rate_and_trial_lesson_pricing_and_calls_SaveChanges()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new SetTutorPricingCommandHandler(repository, StubCurrentUserProvider.AsTutor(tutor.Id.Value), unitOfWork);

        var result = await handler.Handle(new SetTutorPricingCommand(tutor.Id.Value, 500_000m, true, 100_000m));

        Assert.True(result.IsSuccess);
        Assert.Equal(500_000m, tutor.HourlyRate!.Amount);
        Assert.True(tutor.TrialLessonAvailable);
        Assert.Equal(100_000m, tutor.TrialLessonPrice!.Amount);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_leaves_hourly_rate_unchanged_when_amount_is_null()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        tutor.SetHourlyRate(TutorFlow.Domain.Identity.ValueObjects.HourlyRate.Of(500_000m));
        await repository.AddAsync(tutor);
        var handler = new SetTutorPricingCommandHandler(repository, StubCurrentUserProvider.AsTutor(tutor.Id.Value), unitOfWork);

        var result = await handler.Handle(new SetTutorPricingCommand(tutor.Id.Value, null, false, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(500_000m, tutor.HourlyRate!.Amount);
        Assert.False(tutor.TrialLessonAvailable);
    }

    [Fact]
    public async Task Handle_returns_failure_when_trial_lesson_available_with_no_price()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new SetTutorPricingCommandHandler(repository, StubCurrentUserProvider.AsTutor(tutor.Id.Value), unitOfWork);

        var result = await handler.Handle(new SetTutorPricingCommand(tutor.Id.Value, null, true, null));

        Assert.True(result.IsFailure);
        Assert.Equal("SetTutorPricingCommand.TrialLessonPrice.Required", result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_forbidden_when_caller_is_not_the_tutor()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new SetTutorPricingCommandHandler(repository, StubCurrentUserProvider.AsTutor(Guid.NewGuid()), unitOfWork);

        var result = await handler.Handle(new SetTutorPricingCommand(tutor.Id.Value, 500_000m, false, null));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}

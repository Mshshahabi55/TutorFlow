using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;

namespace TutorFlow.Application.Tests.Identity;

public class SetTutorPersonalInfoCommandHandlerTests
{
    [Fact]
    public async Task Handle_sets_personal_info_and_calls_SaveChanges()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new SetTutorPersonalInfoCommandHandler(repository, StubCurrentUserProvider.AsTutor(tutor.Id.Value), unitOfWork);

        var result = await handler.Handle(new SetTutorPersonalInfoCommand(
            tutor.Id.Value, "Jane Doe", "Friendly Tutor", "Bio", "Iran", "Tehran", new[] { "French" }));

        Assert.True(result.IsSuccess);
        Assert.Equal("Jane Doe", tutor.DisplayName);
        Assert.Equal(new[] { "French" }, tutor.OtherLanguages);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_failure_when_tutor_not_found()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var targetId = Guid.NewGuid();
        var handler = new SetTutorPersonalInfoCommandHandler(repository, StubCurrentUserProvider.AsTutor(targetId), unitOfWork);

        var result = await handler.Handle(new SetTutorPersonalInfoCommand(targetId, null, null, null, null, null, null));

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
        var handler = new SetTutorPersonalInfoCommandHandler(repository, StubCurrentUserProvider.AsTutor(Guid.NewGuid()), unitOfWork);

        var result = await handler.Handle(new SetTutorPersonalInfoCommand(tutor.Id.Value, null, null, null, null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    // Merge Readiness audit Critical 2: proves the Domain-level count
    // ceiling surfaces as an ordinary handler failure, the same path
    // length-ceiling violations already take.
    [Fact]
    public async Task Handle_returns_failure_when_otherLanguages_exceeds_the_max_collection_entries()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new SetTutorPersonalInfoCommandHandler(repository, StubCurrentUserProvider.AsTutor(tutor.Id.Value), unitOfWork);
        var tooManyLanguages = Enumerable.Range(0, 21).Select(i => $"Language{i}").ToList();

        var result = await handler.Handle(new SetTutorPersonalInfoCommand(
            tutor.Id.Value, null, null, null, null, null, tooManyLanguages));

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_failure_for_a_field_exceeding_the_length_ceiling()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new SetTutorPersonalInfoCommandHandler(repository, StubCurrentUserProvider.AsTutor(tutor.Id.Value), unitOfWork);

        var result = await handler.Handle(new SetTutorPersonalInfoCommand(
            tutor.Id.Value, new string('a', 201), null, null, null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}

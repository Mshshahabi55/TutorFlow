using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;

namespace TutorFlow.Application.Tests.Identity;

public class SetTutorTeachingInfoCommandHandlerTests
{
    [Fact]
    public async Task Handle_sets_teaching_info_including_multiple_subjects_and_calls_SaveChanges()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new SetTutorTeachingInfoCommandHandler(repository, StubCurrentUserProvider.AsTutor(tutor.Id.Value), unitOfWork);

        var result = await handler.Handle(new SetTutorTeachingInfoCommand(
            tutor.Id.Value,
            new[] { new TutorSubjectInput("Mathematics", "Beginner") },
            5,
            "BSc",
            "TEFL",
            "Socratic",
            new[] { "Exam prep" }));

        Assert.True(result.IsSuccess);
        Assert.Single(tutor.TutorSubjects);
        Assert.Equal("Mathematics", tutor.TutorSubjects.First().Subject);
        Assert.Equal(5, tutor.YearsOfExperience);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_failure_when_a_subject_is_blank()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new SetTutorTeachingInfoCommandHandler(repository, StubCurrentUserProvider.AsTutor(tutor.Id.Value), unitOfWork);

        var result = await handler.Handle(new SetTutorTeachingInfoCommand(
            tutor.Id.Value, new[] { new TutorSubjectInput("  ", null) }, null, null, null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("SetTutorTeachingInfoCommand.TutorSubjects.SubjectRequired", result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_failure_for_negative_years_of_experience()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new SetTutorTeachingInfoCommandHandler(repository, StubCurrentUserProvider.AsTutor(tutor.Id.Value), unitOfWork);

        var result = await handler.Handle(new SetTutorTeachingInfoCommand(
            tutor.Id.Value, null, -1, null, null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    // Merge Readiness audit Critical 2: proves the Domain-level count
    // ceiling (TutorProfileLimits.MaxCollectionEntries) surfaces as an
    // ordinary handler failure, the same path length-ceiling violations
    // already take — no separate Application-layer duplicate check exists
    // (ADR-007: single place of enforcement).
    [Fact]
    public async Task Handle_returns_failure_when_tutorSubjects_exceeds_the_max_collection_entries()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new SetTutorTeachingInfoCommandHandler(repository, StubCurrentUserProvider.AsTutor(tutor.Id.Value), unitOfWork);
        var tooManySubjects = Enumerable.Range(0, 21).Select(i => new TutorSubjectInput($"Subject{i}", null)).ToList();

        var result = await handler.Handle(new SetTutorTeachingInfoCommand(
            tutor.Id.Value, tooManySubjects, null, null, null, null, null));

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
        var handler = new SetTutorTeachingInfoCommandHandler(repository, StubCurrentUserProvider.AsTutor(Guid.NewGuid()), unitOfWork);

        var result = await handler.Handle(new SetTutorTeachingInfoCommand(
            tutor.Id.Value, null, null, null, null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}

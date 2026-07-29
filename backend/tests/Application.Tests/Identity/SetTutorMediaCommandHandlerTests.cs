using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;

namespace TutorFlow.Application.Tests.Identity;

public class SetTutorMediaCommandHandlerTests
{
    [Fact]
    public async Task Handle_sets_media_urls_and_calls_SaveChanges()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new SetTutorMediaCommandHandler(repository, StubCurrentUserProvider.AsTutor(tutor.Id.Value), unitOfWork);

        var result = await handler.Handle(new SetTutorMediaCommand(
            tutor.Id.Value, "https://example.com/photo.jpg", "https://example.com/intro.mp4", new[] { "https://example.com/1.jpg" }));

        Assert.True(result.IsSuccess);
        Assert.Equal("https://example.com/photo.jpg", tutor.PhotoUrl);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_failure_when_tutor_not_found()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var targetId = Guid.NewGuid();
        var handler = new SetTutorMediaCommandHandler(repository, StubCurrentUserProvider.AsTutor(targetId), unitOfWork);

        var result = await handler.Handle(new SetTutorMediaCommand(targetId, null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    // Merge Readiness audit Critical 2 / High 1: proves the Domain-level
    // collection ceiling and media URL scheme checks both surface as an
    // ordinary handler failure — no separate Application-layer duplicate
    // check exists (ADR-007: single place of enforcement).
    [Fact]
    public async Task Handle_returns_failure_when_galleryImageUrls_exceeds_the_max_collection_entries()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new SetTutorMediaCommandHandler(repository, StubCurrentUserProvider.AsTutor(tutor.Id.Value), unitOfWork);
        var tooManyUrls = Enumerable.Range(0, 21).Select(i => $"https://example.com/{i}.jpg").ToList();

        var result = await handler.Handle(new SetTutorMediaCommand(tutor.Id.Value, null, null, tooManyUrls));

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_failure_for_a_non_http_photo_url()
    {
        var repository = new InMemoryTutorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await repository.AddAsync(tutor);
        var handler = new SetTutorMediaCommandHandler(repository, StubCurrentUserProvider.AsTutor(tutor.Id.Value), unitOfWork);

        var result = await handler.Handle(new SetTutorMediaCommand(tutor.Id.Value, "javascript:alert(1)", null, null));

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
        var handler = new SetTutorMediaCommandHandler(repository, StubCurrentUserProvider.AsTutor(Guid.NewGuid()), unitOfWork);

        var result = await handler.Handle(new SetTutorMediaCommand(tutor.Id.Value, null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}

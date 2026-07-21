using TutorFlow.Application.Scheduling.Commands;
using TutorFlow.Application.Scheduling.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Tests.Scheduling;

public class CompleteSessionCommandHandlerTests
{
    private static Session BookSession(Guid tutorId) => AvailabilitySlot.Declare(
            TutorId.From(tutorId),
            DateTime.UtcNow.AddDays(1),
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.Online)
        .Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null);

    [Fact]
    public async Task Handle_completes_existing_session_and_saves()
    {
        var repository = new InMemorySessionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var session = BookSession(Guid.NewGuid());
        await repository.AddAsync(session);
        var handler = new CompleteSessionCommandHandler(
            repository, StubCurrentUserProvider.As(session.TutorId.Value, "Tutor"), unitOfWork);

        var result = await handler.Handle(new CompleteSessionCommand(session.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_failure_when_session_not_found()
    {
        var repository = new InMemorySessionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CompleteSessionCommandHandler(
            repository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()), unitOfWork);

        var result = await handler.Handle(new CompleteSessionCommand(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_translates_domain_error_when_already_completed()
    {
        var repository = new InMemorySessionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var session = BookSession(Guid.NewGuid());
        session.Complete();
        await repository.AddAsync(session);
        var handler = new CompleteSessionCommandHandler(
            repository, StubCurrentUserProvider.As(session.TutorId.Value, "Tutor"), unitOfWork);

        var result = await handler.Handle(new CompleteSessionCommand(session.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_completes_for_admin_who_is_not_the_assigned_tutor()
    {
        var repository = new InMemorySessionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var session = BookSession(Guid.NewGuid());
        await repository.AddAsync(session);
        var handler = new CompleteSessionCommandHandler(
            repository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()), unitOfWork);

        var result = await handler.Handle(new CompleteSessionCommand(session.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_forbidden_for_a_different_tutor()
    {
        var repository = new InMemorySessionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var session = BookSession(Guid.NewGuid());
        await repository.AddAsync(session);
        var handler = new CompleteSessionCommandHandler(
            repository, StubCurrentUserProvider.As(Guid.NewGuid(), "Tutor"), unitOfWork);

        var result = await handler.Handle(new CompleteSessionCommand(session.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}

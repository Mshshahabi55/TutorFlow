using TutorFlow.Application.Scheduling.Commands;
using TutorFlow.Application.Scheduling.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Tests.Scheduling;

public class CancelSessionCommandHandlerTests
{
    private static Session BookSession() => AvailabilitySlot.Declare(
            TutorId.From(Guid.NewGuid()),
            DateTime.UtcNow.AddDays(1),
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.Online)
        .Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null);

    [Fact]
    public async Task Handle_cancels_existing_session_and_calls_SaveChanges()
    {
        var repository = new InMemorySessionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var session = BookSession();
        await repository.AddAsync(session);
        var handler = new CancelSessionCommandHandler(
            repository, StubCurrentUserProvider.As(session.StudentId.Value, "Student"), unitOfWork);

        var result = await handler.Handle(new CancelSessionCommand(session.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_failure_when_session_not_found()
    {
        var repository = new InMemorySessionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CancelSessionCommandHandler(
            repository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()), unitOfWork);

        var result = await handler.Handle(new CancelSessionCommand(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_translates_domain_error_when_already_cancelled()
    {
        var repository = new InMemorySessionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var session = BookSession();
        session.Cancel();
        await repository.AddAsync(session);
        var handler = new CancelSessionCommandHandler(
            repository, StubCurrentUserProvider.As(session.StudentId.Value, "Student"), unitOfWork);

        var result = await handler.Handle(new CancelSessionCommand(session.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_cancels_for_admin_who_is_not_a_party()
    {
        var repository = new InMemorySessionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var session = BookSession();
        await repository.AddAsync(session);
        var handler = new CancelSessionCommandHandler(
            repository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()), unitOfWork);

        var result = await handler.Handle(new CancelSessionCommand(session.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_forbidden_when_caller_is_not_a_party_and_not_admin()
    {
        var repository = new InMemorySessionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var session = BookSession();
        await repository.AddAsync(session);
        var handler = new CancelSessionCommandHandler(
            repository, StubCurrentUserProvider.AsStudent(Guid.NewGuid()), unitOfWork);

        var result = await handler.Handle(new CancelSessionCommand(session.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }
}

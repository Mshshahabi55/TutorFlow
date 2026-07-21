using TutorFlow.Application.Scheduling.Handlers;
using TutorFlow.Application.Scheduling.Queries;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Tests.Scheduling;

public class GetSessionByIdQueryHandlerTests
{
    private static Session BookSession(out Guid studentId)
    {
        studentId = Guid.NewGuid();
        var slot = AvailabilitySlot.Declare(
            TutorId.From(Guid.NewGuid()),
            DateTime.UtcNow.AddDays(1),
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.Online);
        return slot.Book(StudentId.From(studentId), null);
    }

    [Fact]
    public async Task Handle_returns_session_for_the_booking_student()
    {
        var repository = new InMemorySessionRepository();
        var session = BookSession(out var studentId);
        await repository.AddAsync(session);
        var handler = new GetSessionByIdQueryHandler(repository, StubCurrentUserProvider.AsStudent(studentId));

        var result = await handler.Handle(new GetSessionByIdQuery(session.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(session.Id.Value, result.Value.SessionId);
    }

    [Fact]
    public async Task Handle_returns_session_for_admin()
    {
        var repository = new InMemorySessionRepository();
        var session = BookSession(out _);
        await repository.AddAsync(session);
        var handler = new GetSessionByIdQueryHandler(repository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetSessionByIdQuery(session.Id.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_returns_unauthenticated_error_for_an_anonymous_caller()
    {
        var repository = new InMemorySessionRepository();
        var session = BookSession(out _);
        await repository.AddAsync(session);
        var handler = new GetSessionByIdQueryHandler(repository, StubCurrentUserProvider.Unauthenticated());

        var result = await handler.Handle(new GetSessionByIdQuery(session.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
        Assert.Equal("GetSessionByIdQuery.Unauthenticated", result.Error.Code);
    }

    [Fact]
    public async Task Handle_returns_forbidden_for_an_unrelated_caller()
    {
        var repository = new InMemorySessionRepository();
        var session = BookSession(out _);
        await repository.AddAsync(session);
        var handler = new GetSessionByIdQueryHandler(repository, StubCurrentUserProvider.AsStudent(Guid.NewGuid()));

        var result = await handler.Handle(new GetSessionByIdQuery(session.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
    }

    [Fact]
    public async Task Handle_returns_not_found_for_unknown_session()
    {
        var repository = new InMemorySessionRepository();
        var handler = new GetSessionByIdQueryHandler(repository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetSessionByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsFailure);
    }
}

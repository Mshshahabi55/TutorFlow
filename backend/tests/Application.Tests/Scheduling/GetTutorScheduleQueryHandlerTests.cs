using TutorFlow.Application.Scheduling.Handlers;
using TutorFlow.Application.Scheduling.Queries;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Identity;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Tests.Scheduling;

public class GetTutorScheduleQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_every_session_for_the_tutor_regardless_of_status()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var sessionRepository = new InMemorySessionRepository();

        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await tutorRepository.AddAsync(tutor);
        var tutorId = TutorId.From(tutor.Id.Value);

        var slot1 = AvailabilitySlot.Declare(tutorId, DateTime.UtcNow.AddDays(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        var scheduled = slot1.Book(StudentId.From(Guid.NewGuid()), null);
        await sessionRepository.AddAsync(scheduled);

        var slot2 = AvailabilitySlot.Declare(tutorId, DateTime.UtcNow.AddDays(2), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        var cancelled = slot2.Book(StudentId.From(Guid.NewGuid()), null);
        cancelled.Cancel();
        await sessionRepository.AddAsync(cancelled);

        var otherTutorSlot = AvailabilitySlot.Declare(TutorId.From(Guid.NewGuid()), DateTime.UtcNow.AddDays(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online);
        var otherTutorSession = otherTutorSlot.Book(StudentId.From(Guid.NewGuid()), null);
        await sessionRepository.AddAsync(otherTutorSession);

        var handler = new GetTutorScheduleQueryHandler(
            tutorRepository, sessionRepository, StubCurrentUserProvider.AsTutor(tutor.Id.Value));

        var result = await handler.Handle(new GetTutorScheduleQuery(tutor.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(result.Value, s => s.SessionId == scheduled.Id.Value);
        Assert.Contains(result.Value, s => s.SessionId == cancelled.Id.Value);
        Assert.DoesNotContain(result.Value, s => s.SessionId == otherTutorSession.Id.Value);
    }

    [Fact]
    public async Task Handle_returns_schedule_for_admin()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var sessionRepository = new InMemorySessionRepository();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await tutorRepository.AddAsync(tutor);
        var handler = new GetTutorScheduleQueryHandler(
            tutorRepository, sessionRepository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetTutorScheduleQuery(tutor.Id.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_returns_forbidden_for_a_different_tutor()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var sessionRepository = new InMemorySessionRepository();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await tutorRepository.AddAsync(tutor);
        var handler = new GetTutorScheduleQueryHandler(
            tutorRepository, sessionRepository, StubCurrentUserProvider.AsTutor(Guid.NewGuid()));

        var result = await handler.Handle(new GetTutorScheduleQuery(tutor.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
    }

    [Fact]
    public async Task Handle_returns_unauthenticated_error_for_an_anonymous_caller()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var sessionRepository = new InMemorySessionRepository();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await tutorRepository.AddAsync(tutor);
        var handler = new GetTutorScheduleQueryHandler(
            tutorRepository, sessionRepository, StubCurrentUserProvider.Unauthenticated());

        var result = await handler.Handle(new GetTutorScheduleQuery(tutor.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal("GetTutorScheduleQuery.Unauthenticated", result.Error.Code);
    }

    [Fact]
    public async Task Handle_returns_not_found_for_unknown_tutor()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var sessionRepository = new InMemorySessionRepository();
        var handler = new GetTutorScheduleQueryHandler(
            tutorRepository, sessionRepository, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetTutorScheduleQuery(Guid.NewGuid()));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_returns_empty_collection_for_tutor_with_no_sessions()
    {
        var tutorRepository = new InMemoryTutorRepository();
        var sessionRepository = new InMemorySessionRepository();
        var tutor = Tutor.Register(TestCredentials.Email(), TestCredentials.Hash());
        await tutorRepository.AddAsync(tutor);
        var handler = new GetTutorScheduleQueryHandler(
            tutorRepository, sessionRepository, StubCurrentUserProvider.AsTutor(tutor.Id.Value));

        var result = await handler.Handle(new GetTutorScheduleQuery(tutor.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}

using TutorFlow.Application.Common;
using TutorFlow.Application.Meetings.Commands;
using TutorFlow.Application.Meetings.Handlers;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Tests.Meetings;

public class CreateMeetingCommandHandlerTests
{
    private static Session BookOnlineSession(Guid tutorId) => AvailabilitySlot.Declare(
            TutorId.From(tutorId),
            DateTime.UtcNow.AddDays(1),
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.Online)
        .Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null);

    private static Session BookInPersonSession(Guid tutorId) => AvailabilitySlot.Declare(
            TutorId.From(tutorId),
            DateTime.UtcNow.AddDays(1),
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.InPerson)
        .Book(StudentId.From(Guid.NewGuid()), parentGuardianId: null);

    private static CreateMeetingCommandHandler BuildHandler(
        InMemorySessionRepository sessionRepository,
        InMemoryMeetingRepository meetingRepository,
        FakeMeetingProvider provider,
        FakeMeetingProviderSettingsCatalog settingsCatalog,
        TutorFlow.Application.Common.ICurrentUserProvider currentUserProvider,
        FakeUnitOfWork unitOfWork) =>
        new(
            sessionRepository,
            meetingRepository,
            new FakeMeetingProviderResolver(provider),
            settingsCatalog,
            currentUserProvider,
            new FixedDateTimeProvider(DateTime.UtcNow),
            unitOfWork);

    [Fact]
    public async Task Handle_creates_a_meeting_for_the_sessions_own_tutor()
    {
        var sessionRepository = new InMemorySessionRepository();
        var meetingRepository = new InMemoryMeetingRepository();
        var provider = new FakeMeetingProvider();
        var unitOfWork = new FakeUnitOfWork();
        var session = BookOnlineSession(Guid.NewGuid());
        await sessionRepository.AddAsync(session);
        var handler = BuildHandler(
            sessionRepository, meetingRepository, provider, new FakeMeetingProviderSettingsCatalog(),
            StubCurrentUserProvider.As(session.TutorId.Value, "Tutor"), unitOfWork);

        var result = await handler.Handle(new CreateMeetingCommand(session.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(session.Id.Value, result.Value.SessionId);
        Assert.Equal(1, provider.CreateCallCount);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_is_idempotent_and_does_not_call_the_provider_twice()
    {
        var sessionRepository = new InMemorySessionRepository();
        var meetingRepository = new InMemoryMeetingRepository();
        var provider = new FakeMeetingProvider();
        var unitOfWork = new FakeUnitOfWork();
        var session = BookOnlineSession(Guid.NewGuid());
        await sessionRepository.AddAsync(session);
        var handler = BuildHandler(
            sessionRepository, meetingRepository, provider, new FakeMeetingProviderSettingsCatalog(),
            StubCurrentUserProvider.As(session.TutorId.Value, "Tutor"), unitOfWork);

        var first = await handler.Handle(new CreateMeetingCommand(session.Id.Value));
        var second = await handler.Handle(new CreateMeetingCommand(session.Id.Value));

        Assert.Equal(first.Value.MeetingId, second.Value.MeetingId);
        Assert.Equal(1, provider.CreateCallCount);
    }

    [Fact]
    public async Task Handle_returns_forbidden_for_a_different_tutor()
    {
        var sessionRepository = new InMemorySessionRepository();
        var meetingRepository = new InMemoryMeetingRepository();
        var provider = new FakeMeetingProvider();
        var unitOfWork = new FakeUnitOfWork();
        var session = BookOnlineSession(Guid.NewGuid());
        await sessionRepository.AddAsync(session);
        var handler = BuildHandler(
            sessionRepository, meetingRepository, provider, new FakeMeetingProviderSettingsCatalog(),
            StubCurrentUserProvider.As(Guid.NewGuid(), "Tutor"), unitOfWork);

        var result = await handler.Handle(new CreateMeetingCommand(session.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Authorization, result.Error.Type);
        Assert.Equal(0, provider.CreateCallCount);
    }

    [Fact]
    public async Task Handle_rejects_an_in_person_session()
    {
        var sessionRepository = new InMemorySessionRepository();
        var meetingRepository = new InMemoryMeetingRepository();
        var provider = new FakeMeetingProvider();
        var unitOfWork = new FakeUnitOfWork();
        var session = BookInPersonSession(Guid.NewGuid());
        await sessionRepository.AddAsync(session);
        var handler = BuildHandler(
            sessionRepository, meetingRepository, provider, new FakeMeetingProviderSettingsCatalog(),
            StubCurrentUserProvider.As(session.TutorId.Value, "Tutor"), unitOfWork);

        var result = await handler.Handle(new CreateMeetingCommand(session.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal("CreateMeetingCommand.SessionNotOnline", result.Error.Code);
    }

    [Fact]
    public async Task Handle_rejects_a_cancelled_session()
    {
        var sessionRepository = new InMemorySessionRepository();
        var meetingRepository = new InMemoryMeetingRepository();
        var provider = new FakeMeetingProvider();
        var unitOfWork = new FakeUnitOfWork();
        var session = BookOnlineSession(Guid.NewGuid());
        session.Cancel();
        await sessionRepository.AddAsync(session);
        var handler = BuildHandler(
            sessionRepository, meetingRepository, provider, new FakeMeetingProviderSettingsCatalog(),
            StubCurrentUserProvider.As(session.TutorId.Value, "Tutor"), unitOfWork);

        var result = await handler.Handle(new CreateMeetingCommand(session.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal("CreateMeetingCommand.SessionNotScheduled", result.Error.Code);
    }

    [Fact]
    public async Task Handle_returns_an_honest_ProviderNotConfigured_failure_and_never_calls_the_provider()
    {
        var sessionRepository = new InMemorySessionRepository();
        var meetingRepository = new InMemoryMeetingRepository();
        var provider = new FakeMeetingProvider();
        var unitOfWork = new FakeUnitOfWork();
        var session = BookOnlineSession(Guid.NewGuid());
        await sessionRepository.AddAsync(session);
        var handler = BuildHandler(
            sessionRepository, meetingRepository, provider,
            new FakeMeetingProviderSettingsCatalog { Configured = false },
            StubCurrentUserProvider.As(session.TutorId.Value, "Tutor"), unitOfWork);

        var result = await handler.Handle(new CreateMeetingCommand(session.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal("CreateMeetingCommand.ProviderNotConfigured", result.Error.Code);
        Assert.Equal(ErrorType.Infrastructure, result.Error.Type);
        Assert.Equal(0, provider.CreateCallCount);
    }

    [Fact]
    public async Task Handle_translates_a_provider_failure_into_a_Result_failure()
    {
        var sessionRepository = new InMemorySessionRepository();
        var meetingRepository = new InMemoryMeetingRepository();
        var provider = new FakeMeetingProvider { ThrowOnCreate = true };
        var unitOfWork = new FakeUnitOfWork();
        var session = BookOnlineSession(Guid.NewGuid());
        await sessionRepository.AddAsync(session);
        var handler = BuildHandler(
            sessionRepository, meetingRepository, provider, new FakeMeetingProviderSettingsCatalog(),
            StubCurrentUserProvider.As(session.TutorId.Value, "Tutor"), unitOfWork);

        var result = await handler.Handle(new CreateMeetingCommand(session.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal("CreateMeetingCommand.ProviderCallFailed", result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_returns_not_found_for_a_nonexistent_session()
    {
        var sessionRepository = new InMemorySessionRepository();
        var meetingRepository = new InMemoryMeetingRepository();
        var provider = new FakeMeetingProvider();
        var unitOfWork = new FakeUnitOfWork();
        var handler = BuildHandler(
            sessionRepository, meetingRepository, provider, new FakeMeetingProviderSettingsCatalog(),
            StubCurrentUserProvider.AsTutor(Guid.NewGuid()), unitOfWork);

        var result = await handler.Handle(new CreateMeetingCommand(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal("CreateMeetingCommand.SessionNotFound", result.Error.Code);
    }
}

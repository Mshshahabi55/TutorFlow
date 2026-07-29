using TutorFlow.Application.Meetings.Handlers;
using TutorFlow.Application.Meetings.Queries;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Meetings;
using TutorFlow.Domain.Meetings.ValueObjects;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Tests.Meetings;

public class GetMeetingBySessionQueryHandlerTests
{
    private static Session BookOnlineSession(Guid tutorId, Guid studentId) => AvailabilitySlot.Declare(
            TutorId.From(tutorId),
            DateTime.UtcNow.AddDays(1),
            SessionDuration.Of(TimeSpan.FromHours(1)),
            DeliveryMode.Online)
        .Book(StudentId.From(studentId), parentGuardianId: null);

    private static async Task<(InMemorySessionRepository Sessions, InMemoryMeetingRepository Meetings, Session Session)> SeedAsync(
        bool withMeeting)
    {
        var sessionRepository = new InMemorySessionRepository();
        var meetingRepository = new InMemoryMeetingRepository();
        var session = BookOnlineSession(Guid.NewGuid(), Guid.NewGuid());
        await sessionRepository.AddAsync(session);

        if (withMeeting)
        {
            var meeting = Meeting.Create(
                session.Id, MeetingProviderOption.GoogleMeet, "p1", "https://meet.example.com/join/1", null,
                session.ScheduledTimeUtc, session.EndTimeUtc, DateTime.UtcNow);
            await meetingRepository.AddAsync(meeting);
        }

        return (sessionRepository, meetingRepository, session);
    }

    [Fact]
    public async Task Handle_returns_the_meeting_for_the_sessions_own_tutor()
    {
        var (sessions, meetings, session) = await SeedAsync(withMeeting: true);
        var handler = new GetMeetingBySessionQueryHandler(sessions, meetings, StubCurrentUserProvider.As(session.TutorId.Value, "Tutor"));

        var result = await handler.Handle(new GetMeetingBySessionQuery(session.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(session.Id.Value, result.Value.SessionId);
    }

    [Fact]
    public async Task Handle_returns_the_meeting_for_the_sessions_own_student()
    {
        var (sessions, meetings, session) = await SeedAsync(withMeeting: true);
        var handler = new GetMeetingBySessionQueryHandler(
            sessions, meetings, StubCurrentUserProvider.As(session.StudentId.Value, "Student"));

        var result = await handler.Handle(new GetMeetingBySessionQuery(session.Id.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_returns_the_meeting_for_admin()
    {
        var (sessions, meetings, session) = await SeedAsync(withMeeting: true);
        var handler = new GetMeetingBySessionQueryHandler(sessions, meetings, StubCurrentUserProvider.AsAdminStaff(Guid.NewGuid()));

        var result = await handler.Handle(new GetMeetingBySessionQuery(session.Id.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_returns_forbidden_for_an_unrelated_student()
    {
        var (sessions, meetings, session) = await SeedAsync(withMeeting: true);
        var handler = new GetMeetingBySessionQueryHandler(sessions, meetings, StubCurrentUserProvider.As(Guid.NewGuid(), "Student"));

        var result = await handler.Handle(new GetMeetingBySessionQuery(session.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
    }

    [Fact]
    public async Task Handle_returns_not_found_when_no_meeting_has_been_started_yet()
    {
        var (sessions, meetings, session) = await SeedAsync(withMeeting: false);
        var handler = new GetMeetingBySessionQueryHandler(sessions, meetings, StubCurrentUserProvider.As(session.TutorId.Value, "Tutor"));

        var result = await handler.Handle(new GetMeetingBySessionQuery(session.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal("GetMeetingBySessionQuery.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_returns_not_found_for_a_nonexistent_session()
    {
        var sessions = new InMemorySessionRepository();
        var meetings = new InMemoryMeetingRepository();
        var handler = new GetMeetingBySessionQueryHandler(sessions, meetings, StubCurrentUserProvider.AsTutor(Guid.NewGuid()));

        var result = await handler.Handle(new GetMeetingBySessionQuery(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal("GetMeetingBySessionQuery.SessionNotFound", result.Error.Code);
    }
}

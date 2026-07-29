using TutorFlow.Application.Meetings.Handlers;
using TutorFlow.Application.Meetings.Queries;
using TutorFlow.Application.Tests.TestDoubles;
using TutorFlow.Domain.Communication;
using TutorFlow.Domain.Identity.ValueObjects;
using TutorFlow.Domain.Meetings;
using TutorFlow.Domain.Meetings.ValueObjects;
using TutorFlow.Domain.Scheduling;
using TutorFlow.Domain.Scheduling.ValueObjects;

namespace TutorFlow.Application.Tests.Meetings;

public class GetActiveMeetingForConversationQueryHandlerTests
{
    private static GetActiveMeetingForConversationQueryHandler BuildHandler(
        InMemoryConversationRepository conversations,
        InMemorySessionRepository sessions,
        InMemoryMeetingRepository meetings,
        TutorFlow.Application.Common.ICurrentUserProvider currentUserProvider,
        DateTime nowUtc) =>
        new(conversations, sessions, meetings, currentUserProvider, new FixedDateTimeProvider(nowUtc));

    [Fact]
    public async Task Handle_returns_the_meeting_when_the_conversation_has_an_upcoming_online_session_with_one_started()
    {
        var now = DateTime.UtcNow;
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var conversations = new InMemoryConversationRepository();
        var conversation = Conversation.Start(AccountId.From(studentId), AccountId.From(tutorId), now);
        await conversations.AddAsync(conversation);

        var sessions = new InMemorySessionRepository();
        var session = AvailabilitySlot.Declare(
                TutorId.From(tutorId), now.AddHours(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online)
            .Book(StudentId.From(studentId), parentGuardianId: null);
        await sessions.AddAsync(session);

        var meetingsRepo = new InMemoryMeetingRepository();
        var meeting = Meeting.Create(
            session.Id, MeetingProviderOption.GoogleMeet, "p1", "https://meet.example.com/join/1", null,
            session.ScheduledTimeUtc, session.EndTimeUtc, now);
        await meetingsRepo.AddAsync(meeting);

        var handler = BuildHandler(conversations, sessions, meetingsRepo, StubCurrentUserProvider.As(studentId, "Student"), now);

        var result = await handler.Handle(new GetActiveMeetingForConversationQuery(conversation.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(session.Id.Value, result.Value!.SessionId);
    }

    [Fact]
    public async Task Handle_returns_null_when_the_session_exists_but_no_meeting_has_been_started_yet()
    {
        var now = DateTime.UtcNow;
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var conversations = new InMemoryConversationRepository();
        var conversation = Conversation.Start(AccountId.From(studentId), AccountId.From(tutorId), now);
        await conversations.AddAsync(conversation);

        var sessions = new InMemorySessionRepository();
        var session = AvailabilitySlot.Declare(
                TutorId.From(tutorId), now.AddHours(1), SessionDuration.Of(TimeSpan.FromHours(1)), DeliveryMode.Online)
            .Book(StudentId.From(studentId), parentGuardianId: null);
        await sessions.AddAsync(session);

        var handler = BuildHandler(
            conversations, sessions, new InMemoryMeetingRepository(), StubCurrentUserProvider.As(studentId, "Student"), now);

        var result = await handler.Handle(new GetActiveMeetingForConversationQuery(conversation.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task Handle_returns_null_when_there_is_no_relevant_online_session_at_all()
    {
        var now = DateTime.UtcNow;
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var conversations = new InMemoryConversationRepository();
        var conversation = Conversation.Start(AccountId.From(studentId), AccountId.From(tutorId), now);
        await conversations.AddAsync(conversation);

        var handler = BuildHandler(
            conversations, new InMemorySessionRepository(), new InMemoryMeetingRepository(),
            StubCurrentUserProvider.As(studentId, "Student"), now);

        var result = await handler.Handle(new GetActiveMeetingForConversationQuery(conversation.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task Handle_returns_forbidden_for_a_caller_who_is_not_a_party_to_the_conversation()
    {
        var now = DateTime.UtcNow;
        var conversations = new InMemoryConversationRepository();
        var conversation = Conversation.Start(AccountId.New(), AccountId.New(), now);
        await conversations.AddAsync(conversation);

        var handler = BuildHandler(
            conversations, new InMemorySessionRepository(), new InMemoryMeetingRepository(),
            StubCurrentUserProvider.As(Guid.NewGuid(), "Student"), now);

        var result = await handler.Handle(new GetActiveMeetingForConversationQuery(conversation.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal(TutorFlow.Application.Common.ErrorType.Authorization, result.Error.Type);
    }

    [Fact]
    public async Task Handle_returns_not_found_for_a_nonexistent_conversation()
    {
        var handler = BuildHandler(
            new InMemoryConversationRepository(), new InMemorySessionRepository(), new InMemoryMeetingRepository(),
            StubCurrentUserProvider.As(Guid.NewGuid(), "Student"), DateTime.UtcNow);

        var result = await handler.Handle(new GetActiveMeetingForConversationQuery(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal("GetActiveMeetingForConversationQuery.NotFound", result.Error.Code);
    }
}

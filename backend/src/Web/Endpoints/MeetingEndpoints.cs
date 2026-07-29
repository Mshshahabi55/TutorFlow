using TutorFlow.Application.Authorization;
using TutorFlow.Application.Meetings.Commands;
using TutorFlow.Application.Meetings.DTOs;
using TutorFlow.Application.Meetings.Handlers;
using TutorFlow.Application.Meetings.Queries;
using TutorFlow.Web.Authorization;

namespace TutorFlow.Web.Endpoints;

// Presentation-layer endpoints only — see IdentityEndpoints. Implements
// docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md's API
// Shape.
public static class MeetingEndpoints
{
    private const string Tag = "Meetings";

    public static IEndpointRouteBuilder MapMeetingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/sessions/{sessionId:guid}/meeting", async (
            Guid sessionId,
            CreateMeetingCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new CreateMeetingCommand(sessionId), cancellationToken))
                .ToApiResult(logger, nameof(CreateMeetingCommandHandler)))
            .WithApiResultMetadata<MeetingDto>(
                "CreateMeeting", Tag, "Starts (or returns the already-started) online-lesson meeting for a Session.")
            .RequirePermission(Permission.ManageMeetings);

        app.MapGet("/sessions/{sessionId:guid}/meeting", async (
            Guid sessionId,
            GetMeetingBySessionQueryHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new GetMeetingBySessionQuery(sessionId), cancellationToken))
                .ToApiResult(logger, nameof(GetMeetingBySessionQueryHandler)))
            .WithApiResultMetadata<MeetingDto>("GetMeetingBySession", Tag, "Fetches the online-lesson meeting for a Session, if one has been started.");

        app.MapGet("/conversations/{conversationId:guid}/active-meeting", async (
            Guid conversationId,
            GetActiveMeetingForConversationQueryHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new GetActiveMeetingForConversationQuery(conversationId), cancellationToken))
                .ToApiResult(logger, nameof(GetActiveMeetingForConversationQueryHandler)))
            .WithApiResultMetadata<MeetingDto?>(
                "GetActiveMeetingForConversation", Tag,
                "Fetches the Meeting for the nearest relevant online Session between a Conversation's two participants, if any (surfaces \"Join Lesson\" on the Conversation page).")
            .RequirePermission(Permission.UseMessaging);

        return app;
    }
}

using TutorFlow.Application.Authorization;
using TutorFlow.Application.Communication.Commands;
using TutorFlow.Application.Communication.DTOs;
using TutorFlow.Application.Communication.Handlers;
using TutorFlow.Application.Communication.Queries;
using TutorFlow.Web.Authorization;

namespace TutorFlow.Web.Endpoints;

// Presentation-layer endpoints only — see IdentityEndpoints. Implements
// docs/adr/ADR-022-communication-and-notifications-architecture.md's API
// Shape. Every route requires Permission.UseMessaging (coarse-grained,
// granted to all four roles per the ADR) plus a fine-grained,
// resource-instance check inside the owning Application-layer handler —
// same two-tier model ADR-003 already established for every other context.
public static class CommunicationEndpoints
{
    private const string Tag = "Communication";

    public static IEndpointRouteBuilder MapCommunicationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/conversations", async (
            StartConversationCommand command,
            StartConversationCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(command, cancellationToken))
                .ToApiResult(logger, nameof(StartConversationCommandHandler)))
            .WithApiResultMetadata<ConversationDto>(
                "StartConversation", Tag, "Starts a new Conversation with a target Account, or returns the existing one.")
            .RequirePermission(Permission.UseMessaging);

        app.MapGet("/conversations/mine", async (
            GetMyConversationsQueryHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new GetMyConversationsQuery(), cancellationToken))
                .ToApiResult(logger, nameof(GetMyConversationsQueryHandler)))
            .WithApiResultMetadata<IReadOnlyCollection<ConversationDto>>(
                "GetMyConversations", Tag, "Lists every Conversation the caller is a party to, most recently active first.")
            .RequirePermission(Permission.UseMessaging);

        app.MapGet("/conversations/{conversationId:guid}/messages", async (
            Guid conversationId,
            GetConversationMessagesQueryHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new GetConversationMessagesQuery(conversationId), cancellationToken))
                .ToApiResult(logger, nameof(GetConversationMessagesQueryHandler)))
            .WithApiResultMetadata<IReadOnlyCollection<MessageDto>>(
                "GetConversationMessages", Tag, "Lists every Message in a Conversation, oldest first.")
            .RequirePermission(Permission.UseMessaging);

        app.MapPost("/conversations/{conversationId:guid}/messages", async (
            Guid conversationId,
            SendMessageRequest request,
            SendMessageCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SendMessageCommand(conversationId, request.Body), cancellationToken))
                .ToApiResult(logger, nameof(SendMessageCommandHandler)))
            .WithApiResultMetadata<MessageDto>("SendMessage", Tag, "Sends a Message into an existing Conversation.")
            .RequirePermission(Permission.UseMessaging);

        app.MapPost("/conversations/{conversationId:guid}/read", async (
            Guid conversationId,
            MarkConversationReadCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new MarkConversationReadCommand(conversationId), cancellationToken))
                .ToApiResult(logger, nameof(MarkConversationReadCommandHandler)))
            .WithApiResultMetadata(
                "MarkConversationRead", Tag, "Marks every unread Message addressed to the caller in a Conversation as read.")
            .RequirePermission(Permission.UseMessaging);

        app.MapGet("/notifications/mine", async (
            GetMyNotificationsQueryHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new GetMyNotificationsQuery(), cancellationToken))
                .ToApiResult(logger, nameof(GetMyNotificationsQueryHandler)))
            .WithApiResultMetadata<IReadOnlyCollection<NotificationDto>>(
                "GetMyNotifications", Tag, "Lists every Notification addressed to the caller, most recent first.")
            .RequirePermission(Permission.UseMessaging);

        app.MapPost("/notifications/{notificationId:guid}/read", async (
            Guid notificationId,
            MarkNotificationReadCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new MarkNotificationReadCommand(notificationId), cancellationToken))
                .ToApiResult(logger, nameof(MarkNotificationReadCommandHandler)))
            .WithApiResultMetadata("MarkNotificationRead", Tag, "Marks a single Notification as read.")
            .RequirePermission(Permission.UseMessaging);

        app.MapPost("/notifications/mark-all-read", async (
            MarkAllNotificationsReadCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new MarkAllNotificationsReadCommand(), cancellationToken))
                .ToApiResult(logger, nameof(MarkAllNotificationsReadCommandHandler)))
            .WithApiResultMetadata("MarkAllNotificationsRead", Tag, "Marks every Notification addressed to the caller as read.")
            .RequirePermission(Permission.UseMessaging);

        return app;
    }

    // Minimal request shape needed only because SendMessageCommand's
    // ConversationId is bound from the route, not the body — this is the
    // "map request → Command" step, not business logic.
    internal sealed record SendMessageRequest(string Body);
}

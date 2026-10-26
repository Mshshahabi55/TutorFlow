using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Messaging.Infrastructure;
using TutorFlow.Modules.Messaging.Persistence;
using CreateOrGetConversationFeature = TutorFlow.Modules.Messaging.Features.CreateOrGetConversation.CreateOrGetConversation;
using GetMessagesFeature = TutorFlow.Modules.Messaging.Features.GetMessages.GetMessages;
using ListConversationsFeature = TutorFlow.Modules.Messaging.Features.ListConversations.ListConversations;
using MarkConversationReadFeature = TutorFlow.Modules.Messaging.Features.MarkConversationRead.MarkConversationRead;
using SendMessageFeature = TutorFlow.Modules.Messaging.Features.SendMessage.SendMessage;

namespace TutorFlow.Modules.Messaging;

public static class MessagingModule
{
    public static IServiceCollection AddMessagingModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<MessagingDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("MessagingDb")));

        services.AddScoped<CreateOrGetConversationFeature.Handler>();
        services.AddScoped<IValidator<CreateOrGetConversationFeature.Command>, CreateOrGetConversationFeature.Validator>();
        services.AddScoped<ListConversationsFeature.Handler>();
        services.AddScoped<GetMessagesFeature.Handler>();
        services.AddScoped<SendMessageFeature.Handler>();
        services.AddScoped<IValidator<SendMessageFeature.Command>, SendMessageFeature.Validator>();
        services.AddScoped<MarkConversationReadFeature.Handler>();

        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.StudentOnly, p => p.RequireRole(RoleNames.Student))
            .AddPolicy(PolicyNames.StudentOrTutorOnly, p => p.RequireRole(RoleNames.Student, RoleNames.Tutor));

        return services;
    }

    public static IEndpointRouteBuilder MapMessagingModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/messaging").WithTags("Messaging");

        group.MapPost("/conversations", CreateOrGetConversationAsync)
            .WithName("CreateOrGetConversation")
            .WithSummary("Create or retrieve the caller's conversation with a published tutor. Student only; no booking required.")
            .RequireAuthorization(PolicyNames.StudentOnly)
            .AddEndpointFilter<ValidationFilter<CreateOrGetConversationFeature.Command>>()
            .RequireRateLimiting("messaging-create-conversation");

        group.MapGet("/conversations", ListConversationsAsync)
            .WithName("ListConversations")
            .WithSummary("List the caller's own conversations, newest activity first.")
            .RequireAuthorization(PolicyNames.StudentOrTutorOnly);

        group.MapGet("/conversations/{id:guid}/messages", GetMessagesAsync)
            .WithName("GetConversationMessages")
            .WithSummary("Cursor-paginated messages for a conversation the caller is a participant in. Newest-first; 'before' pages into older history.")
            .RequireAuthorization(PolicyNames.StudentOrTutorOnly);

        group.MapPost("/conversations/{id:guid}/messages", SendMessageAsync)
            .WithName("SendMessage")
            .WithSummary("Send a message in a conversation the caller is a participant in.")
            .RequireAuthorization(PolicyNames.StudentOrTutorOnly)
            .AddEndpointFilter<ValidationFilter<SendMessageFeature.Command>>()
            .RequireRateLimiting("messaging-send-message");

        group.MapPost("/conversations/{id:guid}/read", MarkConversationReadAsync)
            .WithName("MarkConversationRead")
            .WithSummary("Mark the conversation as read up to now, for the caller's own side only.")
            .RequireAuthorization(PolicyNames.StudentOrTutorOnly);

        return app;
    }

    private static async Task<IResult> CreateOrGetConversationAsync(
        ClaimsPrincipal user,
        CreateOrGetConversationFeature.Command command,
        CreateOrGetConversationFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var studentUserId))
        {
            return TypedResults.Unauthorized();
        }

        var (outcome, response) = await handler.HandleAsync(studentUserId, command.TutorUserId, ct);
        return outcome switch
        {
            CreateOrGetConversationFeature.Outcome.Created => TypedResults.Created($"/api/messaging/conversations/{response!.Id}", response),
            CreateOrGetConversationFeature.Outcome.Existing => TypedResults.Ok(response),
            // Deliberately the same generic rejection for "tutor doesn't exist" and "tutor exists
            // but is not eligible" (ADR-0030) — the eligibility query itself never distinguishes
            // these cases either, so there is nothing more specific to leak here.
            _ => TypedResults.Problem(
                title: "This tutor cannot be messaged right now.",
                statusCode: StatusCodes.Status400BadRequest)
        };
    }

    private static async Task<IResult> ListConversationsAsync(
        ClaimsPrincipal user,
        ListConversationsFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(await handler.HandleAsync(userId, ct));
    }

    private static async Task<IResult> GetMessagesAsync(
        ClaimsPrincipal user,
        Guid id,
        long? before,
        int? take,
        GetMessagesFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var (outcome, response) = await handler.HandleAsync(userId, id, before, take, ct);
        return outcome switch
        {
            GetMessagesFeature.Outcome.Found => TypedResults.Ok(response),
            // Uniform 404 for "no such conversation" and "not a participant" (ADR-0030) — reduces
            // conversation-ID enumeration, a deliberate deviation from Bookings.GetBooking's
            // distinguished Forbidden/NotFound for this more private, 1:1 data.
            _ => TypedResults.NotFound()
        };
    }

    private static async Task<IResult> SendMessageAsync(
        ClaimsPrincipal user,
        Guid id,
        SendMessageFeature.Command command,
        SendMessageFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var (outcome, response) = await handler.HandleAsync(userId, id, command, ct);
        return outcome switch
        {
            SendMessageFeature.Outcome.Sent => TypedResults.Created($"/api/messaging/conversations/{id}/messages", response),
            _ => TypedResults.NotFound()
        };
    }

    private static async Task<IResult> MarkConversationReadAsync(
        ClaimsPrincipal user,
        Guid id,
        MarkConversationReadFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var outcome = await handler.HandleAsync(userId, id, ct);
        return outcome switch
        {
            MarkConversationReadFeature.Outcome.Updated => TypedResults.NoContent(),
            _ => TypedResults.NotFound()
        };
    }
}

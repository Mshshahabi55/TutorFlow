using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Availability.Infrastructure;
using TutorFlow.Modules.Availability.Persistence;
using TutorFlow.Shared;
using AddTimeOffFeature = TutorFlow.Modules.Availability.Features.AddTimeOff.AddTimeOff;
using GetComputedSlotsFeature = TutorFlow.Modules.Availability.Features.GetComputedSlots.GetComputedSlots;
using GetCurrentConfigurationFeature = TutorFlow.Modules.Availability.Features.GetCurrentConfiguration.GetCurrentConfiguration;
using GetTutorAvailabilityPreviewFeature = TutorFlow.Modules.Availability.Features.GetTutorAvailabilityPreview.GetTutorAvailabilityPreview;
using GetTutorSlotsFeature = TutorFlow.Modules.Availability.Features.GetTutorSlots.GetTutorSlots;
using RemoveTimeOffFeature = TutorFlow.Modules.Availability.Features.RemoveTimeOff.RemoveTimeOff;
using ReplaceWeeklyRulesFeature = TutorFlow.Modules.Availability.Features.ReplaceWeeklyRules.ReplaceWeeklyRules;
using UpdateBookingSettingsFeature = TutorFlow.Modules.Availability.Features.UpdateBookingSettings.UpdateBookingSettings;

namespace TutorFlow.Modules.Availability;

public static class AvailabilityModule
{
    public static IServiceCollection AddAvailabilityModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AvailabilityDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("AvailabilityDb")));

        services.AddScoped<ReplaceWeeklyRulesFeature.Handler>();
        services.AddScoped<IValidator<ReplaceWeeklyRulesFeature.Command>, ReplaceWeeklyRulesFeature.Validator>();

        services.AddScoped<AddTimeOffFeature.Handler>();
        services.AddScoped<IValidator<AddTimeOffFeature.Command>, AddTimeOffFeature.Validator>();

        services.AddScoped<RemoveTimeOffFeature.Handler>();

        services.AddScoped<UpdateBookingSettingsFeature.Handler>();
        services.AddScoped<IValidator<UpdateBookingSettingsFeature.Command>, UpdateBookingSettingsFeature.Validator>();

        services.AddScoped<GetCurrentConfigurationFeature.Handler>();
        services.AddScoped<GetComputedSlotsFeature.Handler>();
        services.AddScoped<GetTutorSlotsFeature.Handler>();
        services.AddScoped<IValidator<GetTutorSlotsFeature.Query>, GetTutorSlotsFeature.Validator>();
        services.AddScoped<GetTutorAvailabilityPreviewFeature.Handler>();

        services.AddScoped<TutorBookabilityProjector>();
        services.AddScoped<AvailabilitySignalPublisher>();

        // ADR-0014: Availability implements the Shared query contract Bookings depends on via
        // DI. No project reference to TutorFlow.Modules.Bookings is added by this registration.
        services.AddScoped<ITutorAvailabilityQuery, TutorAvailabilityQuery>();

        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.TutorOnly, p => p.RequireRole(RoleNames.Tutor));

        return services;
    }

    public static IEndpointRouteBuilder MapAvailabilityModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/availability")
            .WithTags("Availability")
            .RequireAuthorization(PolicyNames.TutorOnly);

        group.MapPut("/me/weekly-rules", ReplaceWeeklyRulesAsync)
            .WithName("ReplaceWeeklyAvailabilityRules")
            .WithSummary("Replace the current user's full set of weekly availability rules.")
            .AddEndpointFilter<ValidationFilter<ReplaceWeeklyRulesFeature.Command>>();

        group.MapPost("/me/time-off", AddTimeOffAsync)
            .WithName("AddTimeOff")
            .WithSummary("Add a time-off period for the current user.")
            .AddEndpointFilter<ValidationFilter<AddTimeOffFeature.Command>>();

        group.MapDelete("/me/time-off/{timeOffId:guid}", RemoveTimeOffAsync)
            .WithName("RemoveTimeOff")
            .WithSummary("Remove one of the current user's time-off periods.");

        group.MapPut("/me/booking-settings", UpdateBookingSettingsAsync)
            .WithName("UpdateBookingSettings")
            .WithSummary("Create or update the current user's booking settings.")
            .AddEndpointFilter<ValidationFilter<UpdateBookingSettingsFeature.Command>>();

        group.MapGet("/me", GetCurrentConfigurationAsync)
            .WithName("GetCurrentAvailabilityConfiguration")
            .WithSummary("Return the current user's weekly rules, time off, booking settings, and bookability.");

        group.MapGet("/me/slots", GetComputedSlotsAsync)
            .WithName("GetComputedAvailabilitySlots")
            .WithSummary("Compute the current user's open slot starts from weekly rules, time off, and booking settings.");

        // Separate, non-self-service group: any authenticated user (not just Tutors) may browse
        // a different tutor's open slots — intentionally not nested under the TutorOnly-policy
        // group above (BOOKINGS_SPEC.md §5 prerequisite #2).
        var publicGroup = app.MapGroup("/api/availability")
            .WithTags("Availability")
            .RequireAuthorization();

        publicGroup.MapGet("/tutors/{tutorUserId:guid}/slots", GetTutorSlotsAsync)
            .WithName("GetTutorSlots")
            .WithSummary("Browse a tutor's open slots, excluding Bookings' active holds (public, non-self-service).")
            .AddEndpointFilter<ValidationFilter<GetTutorSlotsFeature.Query>>();

        // Anonymous — Guests see a coarse availability signal on the public Tutor Profile page
        // without authenticating; the full slot grid above stays behind auth (ADR-0022).
        var anonymousGroup = app.MapGroup("/api/availability").WithTags("Availability").AllowAnonymous();

        anonymousGroup.MapGet("/tutors/{tutorUserId:guid}/preview", GetTutorAvailabilityPreviewAsync)
            .WithName("GetTutorAvailabilityPreview")
            .WithSummary("Coarse has-availability signal for a tutor, visible to anonymous visitors (ADR-0022).");

        return app;
    }

    private static async Task<IResult> ReplaceWeeklyRulesAsync(
        ClaimsPrincipal user,
        ReplaceWeeklyRulesFeature.Command command,
        ReplaceWeeklyRulesFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var result = await handler.HandleAsync(userId, command, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : result.ToProblemDetails(StatusCodes.Status400BadRequest);
        }
        catch (WeeklyRulesConflictException ex)
        {
            return TypedResults.Problem(title: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    private static async Task<IResult> AddTimeOffAsync(
        ClaimsPrincipal user,
        AddTimeOffFeature.Command command,
        AddTimeOffFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(userId, command, ct);
        return result.IsSuccess
            ? TypedResults.Created($"/api/availability/me/time-off/{result.Value.Id}", result.Value)
            : result.ToProblemDetails(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> RemoveTimeOffAsync(
        ClaimsPrincipal user,
        Guid timeOffId,
        RemoveTimeOffFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(userId, timeOffId, ct);
        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.ToProblemDetails(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> UpdateBookingSettingsAsync(
        ClaimsPrincipal user,
        UpdateBookingSettingsFeature.Command command,
        UpdateBookingSettingsFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(userId, command, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetCurrentConfigurationAsync(
        ClaimsPrincipal user,
        GetCurrentConfigurationFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var response = await handler.HandleAsync(userId, ct);
        return TypedResults.Ok(response);
    }

    private static async Task<IResult> GetComputedSlotsAsync(
        ClaimsPrincipal user,
        GetComputedSlotsFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(userId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetTutorSlotsAsync(
        Guid tutorUserId,
        [AsParameters] GetTutorSlotsFeature.Query query,
        GetTutorSlotsFeature.Handler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(tutorUserId, query, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetTutorAvailabilityPreviewAsync(
        Guid tutorUserId,
        GetTutorAvailabilityPreviewFeature.Handler handler,
        CancellationToken ct)
    {
        return TypedResults.Ok(await handler.HandleAsync(tutorUserId, ct));
    }
}

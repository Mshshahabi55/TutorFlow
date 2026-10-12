using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Tutors.Infrastructure;
using TutorFlow.Modules.Tutors.Persistence;
using TutorFlow.Shared;
using ApproveApplicationFeature = TutorFlow.Modules.Tutors.Features.ApproveApplication.ApproveApplication;
using BeginReviewApplicationFeature = TutorFlow.Modules.Tutors.Features.BeginReviewApplication.BeginReviewApplication;
using CreateApplicationFeature = TutorFlow.Modules.Tutors.Features.CreateApplication.CreateApplication;
using GetCurrentApplicationFeature = TutorFlow.Modules.Tutors.Features.GetCurrentApplication.GetCurrentApplication;
using GetCurrentStatusFeature = TutorFlow.Modules.Tutors.Features.GetCurrentStatus.GetCurrentStatus;
using GetPendingTutorApplicationsFeature = TutorFlow.Modules.Tutors.Features.GetPendingTutorApplications.GetPendingTutorApplications;
using GetPublicProfileFeature = TutorFlow.Modules.Tutors.Features.GetPublicProfile.GetPublicProfile;
using GetTutorApplicationForAdminFeature = TutorFlow.Modules.Tutors.Features.GetTutorApplicationForAdmin.GetTutorApplicationForAdmin;
using PublishApplicationFeature = TutorFlow.Modules.Tutors.Features.PublishApplication.PublishApplication;
using RejectApplicationFeature = TutorFlow.Modules.Tutors.Features.RejectApplication.RejectApplication;
using ReopenApplicationForRevisionFeature = TutorFlow.Modules.Tutors.Features.ReopenApplicationForRevision.ReopenApplicationForRevision;
using SubmitApplicationFeature = TutorFlow.Modules.Tutors.Features.SubmitApplication.SubmitApplication;
using SuspendApplicationFeature = TutorFlow.Modules.Tutors.Features.SuspendApplication.SuspendApplication;
using UpdateDraftFeature = TutorFlow.Modules.Tutors.Features.UpdateDraft.UpdateDraft;

namespace TutorFlow.Modules.Tutors;

public static class TutorsModule
{
    public static IServiceCollection AddTutorsModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<TutorsDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("TutorsDb")));

        services.AddScoped<CreateApplicationFeature.Handler>();
        services.AddScoped<GetCurrentApplicationFeature.Handler>();
        services.AddScoped<SubmitApplicationFeature.Handler>();
        services.AddScoped<ReopenApplicationForRevisionFeature.Handler>();
        services.AddScoped<GetCurrentStatusFeature.Handler>();
        services.AddScoped<PublishApplicationFeature.Handler>();
        services.AddScoped<SuspendApplicationFeature.Handler>();
        services.AddScoped<GetPublicProfileFeature.Handler>();

        services.AddScoped<UpdateDraftFeature.Handler>();
        services.AddScoped<IValidator<UpdateDraftFeature.Command>, UpdateDraftFeature.Validator>();

        // Admin review pipeline (ADR-0028) — Composed Admin (ADR-0008): these handlers live in
        // Tutors, the owning module, not a separate Admin module.
        services.AddScoped<GetPendingTutorApplicationsFeature.Handler>();
        services.AddScoped<IValidator<GetPendingTutorApplicationsFeature.Query>, GetPendingTutorApplicationsFeature.Validator>();
        services.AddScoped<GetTutorApplicationForAdminFeature.Handler>();
        services.AddScoped<BeginReviewApplicationFeature.Handler>();
        services.AddScoped<ApproveApplicationFeature.Handler>();
        services.AddScoped<RejectApplicationFeature.Handler>();
        services.AddScoped<IValidator<RejectApplicationFeature.Command>, RejectApplicationFeature.Validator>();
        services.AddScoped<IValidator<SuspendApplicationFeature.Request>, SuspendApplicationFeature.Validator>();

        // ADR-0014: Tutors implements the Shared query contract Bookings depends on via DI. No
        // project reference to TutorFlow.Modules.Bookings is added by this registration.
        services.AddScoped<ITutorPricingQuery, TutorPricingQuery>();

        // ADR-0029: the reciprocal read-only query Notifications depends on via DI to compose
        // the abandoned-checkout recovery email. No project reference to
        // TutorFlow.Modules.Notifications is added by this registration.
        services.AddScoped<ITutorProfileSummaryQuery, TutorProfileSummaryQuery>();

        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.TutorOnly, p => p.RequireRole(RoleNames.Tutor))
            .AddPolicy(PolicyNames.AdminOnly, p => p.RequireRole(RoleNames.Admin));

        // Public, read-heavy, infrequently-mutated response — exactly the output-cache use case
        // the Kit's caching skill names first (ADR-0022). Tagged "tutors" so a future endpoint
        // that mutates a Published profile has a one-line eviction hook
        // (IOutputCacheStore.EvictByTagAsync) ready to use; nothing currently mutates a
        // Published application, so no eviction call exists yet — added when one does.
        services.AddOutputCache(options =>
            options.AddPolicy("TutorPublicProfile", policy =>
                policy.Expire(TimeSpan.FromMinutes(5)).SetVaryByRouteValue("slug").Tag("tutors")));

        return services;
    }

    public static IEndpointRouteBuilder MapTutorsModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tutors")
            .WithTags("Tutors")
            .RequireAuthorization(PolicyNames.TutorOnly);

        group.MapPost("/applications", CreateApplicationAsync)
            .WithName("CreateTutorApplication")
            .WithSummary("Create a new Draft tutor application for the current user.");

        group.MapGet("/applications/me", GetCurrentApplicationAsync)
            .WithName("GetCurrentTutorApplication")
            .WithSummary("Return the current user's tutor application (full draft/profile data).");

        group.MapPatch("/applications/me", UpdateDraftAsync)
            .WithName("UpdateTutorApplicationDraft")
            .WithSummary("Partially update the current user's Draft tutor application.")
            .AddEndpointFilter<ValidationFilter<UpdateDraftFeature.Command>>();

        group.MapPost("/applications/me/submit", SubmitApplicationAsync)
            .WithName("SubmitTutorApplication")
            .WithSummary("Submit the current user's Draft tutor application for review.");

        group.MapGet("/me", GetCurrentStatusAsync)
            .WithName("GetCurrentTutorStatus")
            .WithSummary("Return a lightweight status summary for the current user's tutor application.");

        group.MapPost("/applications/me/reopen", ReopenApplicationForRevisionAsync)
            .WithName("ReopenTutorApplicationForRevision")
            .WithSummary("Move a Rejected application back to Draft so it can be corrected and resubmitted (ADR-0026).");

        // Separate, anonymous-access group — the self-service group above requires
        // PolicyNames.TutorOnly for every route it defines, which the public profile route
        // must not inherit. ASP.NET Core's routing always prefers a literal segment (e.g.
        // "/me" above) over a parameterized one at the same position, so "/{slug}" cannot
        // shadow any of this module's existing literal routes.
        var publicGroup = app.MapGroup("/api/tutors").WithTags("Tutors").AllowAnonymous();

        publicGroup.MapGet("/{slug}", GetPublicProfileAsync)
            .WithName("GetTutorPublicProfile")
            .WithSummary("Return a Published tutor's public profile by slug.")
            .CacheOutput("TutorPublicProfile");

        // Composed Admin (ADR-0008, ADR-0028): a separate group, gated on AdminOnly rather than
        // TutorOnly — an admin account need not (and typically won't) also hold the Tutor role.
        var adminGroup = app.MapGroup("/api/tutors/admin")
            .WithTags("Tutors.Admin")
            .RequireAuthorization(PolicyNames.AdminOnly);

        adminGroup.MapGet("/applications", GetPendingApplicationsAsync)
            .WithName("GetPendingTutorApplications")
            .WithSummary("List tutor applications awaiting admin review (Submitted/UnderReview by default).")
            .AddEndpointFilter<ValidationFilter<GetPendingTutorApplicationsFeature.Query>>();

        adminGroup.MapGet("/applications/{id:guid}", GetApplicationForAdminAsync)
            .WithName("GetTutorApplicationForAdmin")
            .WithSummary("Return the full detail of a tutor application by its Id, for admin review.");

        adminGroup.MapPost("/applications/{id:guid}/begin-review", BeginReviewApplicationAsync)
            .WithName("BeginReviewTutorApplication")
            .WithSummary("Mark a Submitted application UnderReview.");

        adminGroup.MapPost("/applications/{id:guid}/approve", ApproveApplicationAsync)
            .WithName("ApproveTutorApplication")
            .WithSummary("Approve a Submitted/UnderReview application. Does not publish it (see PublishApplication).");

        adminGroup.MapPost("/applications/{id:guid}/reject", RejectApplicationAsync)
            .WithName("RejectTutorApplication")
            .WithSummary("Reject a Submitted/UnderReview application with a required reason.")
            .AddEndpointFilter<ValidationFilter<RejectApplicationFeature.Command>>();

        adminGroup.MapPost("/applications/{id:guid}/publish", PublishApplicationAsync)
            .WithName("PublishTutorApplication")
            .WithSummary("Publish an Approved application, making the tutor publicly discoverable.");

        adminGroup.MapPost("/applications/{id:guid}/suspend", SuspendApplicationAsync)
            .WithName("SuspendTutorApplication")
            .WithSummary("Suspend a Published application with a required reason, removing it from Search.")
            .AddEndpointFilter<ValidationFilter<SuspendApplicationFeature.Request>>();

        return app;
    }

    private static async Task<IResult> CreateApplicationAsync(
        ClaimsPrincipal user,
        CreateApplicationFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(userId, ct);
        return result.IsSuccess
            ? TypedResults.Created($"/api/tutors/applications/{result.Value.Id}", result.Value)
            : result.ToProblemDetails(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetCurrentApplicationAsync(
        ClaimsPrincipal user,
        GetCurrentApplicationFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(userId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> UpdateDraftAsync(
        ClaimsPrincipal user,
        UpdateDraftFeature.Command command,
        UpdateDraftFeature.Handler handler,
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

    private static async Task<IResult> SubmitApplicationAsync(
        ClaimsPrincipal user,
        SubmitApplicationFeature.Handler handler,
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

    private static async Task<IResult> GetCurrentStatusAsync(
        ClaimsPrincipal user,
        GetCurrentStatusFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(userId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> ReopenApplicationForRevisionAsync(
        ClaimsPrincipal user,
        ReopenApplicationForRevisionFeature.Handler handler,
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

    private static async Task<IResult> GetPublicProfileAsync(
        string slug,
        GetPublicProfileFeature.Handler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(slug, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetPendingApplicationsAsync(
        [AsParameters] GetPendingTutorApplicationsFeature.Query query,
        GetPendingTutorApplicationsFeature.Handler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(query, ct);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetApplicationForAdminAsync(
        Guid id,
        GetTutorApplicationForAdminFeature.Handler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Server-side "can't act on your own application" guard (Phase 4 of ADR-0028) — looked up
    /// fresh for every admin mutation rather than trusted from any client-supplied state. Returns
    /// null when the id doesn't exist at all (caller maps that to 404), so a single helper covers
    /// both the "not found" and "is this the caller's own application" checks.
    /// </summary>
    private static async Task<Guid?> GetApplicationOwnerUserIdAsync(TutorsDbContext db, Guid applicationId, CancellationToken ct) =>
        await db.TutorApplications.AsNoTracking()
            .Where(a => a.Id == applicationId)
            .Select(a => (Guid?)a.UserId)
            .FirstOrDefaultAsync(ct);

    private static async Task<IResult> BeginReviewApplicationAsync(
        Guid id,
        ClaimsPrincipal admin,
        TutorsDbContext db,
        BeginReviewApplicationFeature.Handler handler,
        CancellationToken ct)
    {
        if (!admin.TryGetUserId(out var adminUserId))
        {
            return TypedResults.Unauthorized();
        }

        var ownerUserId = await GetApplicationOwnerUserIdAsync(db, id, ct);
        if (ownerUserId is null)
        {
            return TypedResults.NotFound();
        }

        if (ownerUserId == adminUserId)
        {
            return TypedResults.Problem(title: "Cannot act on your own tutor application.", statusCode: StatusCodes.Status403Forbidden);
        }

        var result = await handler.HandleAsync(id, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> ApproveApplicationAsync(
        Guid id,
        ClaimsPrincipal admin,
        TutorsDbContext db,
        ApproveApplicationFeature.Handler handler,
        CancellationToken ct)
    {
        if (!admin.TryGetUserId(out var adminUserId))
        {
            return TypedResults.Unauthorized();
        }

        var ownerUserId = await GetApplicationOwnerUserIdAsync(db, id, ct);
        if (ownerUserId is null)
        {
            return TypedResults.NotFound();
        }

        if (ownerUserId == adminUserId)
        {
            return TypedResults.Problem(title: "Cannot act on your own tutor application.", statusCode: StatusCodes.Status403Forbidden);
        }

        var result = await handler.HandleAsync(id, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> RejectApplicationAsync(
        Guid id,
        RejectApplicationFeature.Command command,
        ClaimsPrincipal admin,
        TutorsDbContext db,
        RejectApplicationFeature.Handler handler,
        CancellationToken ct)
    {
        if (!admin.TryGetUserId(out var adminUserId))
        {
            return TypedResults.Unauthorized();
        }

        var ownerUserId = await GetApplicationOwnerUserIdAsync(db, id, ct);
        if (ownerUserId is null)
        {
            return TypedResults.NotFound();
        }

        if (ownerUserId == adminUserId)
        {
            return TypedResults.Problem(title: "Cannot act on your own tutor application.", statusCode: StatusCodes.Status403Forbidden);
        }

        var result = await handler.HandleAsync(id, command.Reason, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> PublishApplicationAsync(
        Guid id,
        ClaimsPrincipal admin,
        TutorsDbContext db,
        PublishApplicationFeature.Handler handler,
        CancellationToken ct)
    {
        if (!admin.TryGetUserId(out var adminUserId))
        {
            return TypedResults.Unauthorized();
        }

        var ownerUserId = await GetApplicationOwnerUserIdAsync(db, id, ct);
        if (ownerUserId is null)
        {
            return TypedResults.NotFound();
        }

        if (ownerUserId == adminUserId)
        {
            return TypedResults.Problem(title: "Cannot act on your own tutor application.", statusCode: StatusCodes.Status403Forbidden);
        }

        var result = await handler.HandleAsync(id, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> SuspendApplicationAsync(
        Guid id,
        SuspendApplicationFeature.Request request,
        ClaimsPrincipal admin,
        TutorsDbContext db,
        SuspendApplicationFeature.Handler handler,
        CancellationToken ct)
    {
        if (!admin.TryGetUserId(out var adminUserId))
        {
            return TypedResults.Unauthorized();
        }

        var ownerUserId = await GetApplicationOwnerUserIdAsync(db, id, ct);
        if (ownerUserId is null)
        {
            return TypedResults.NotFound();
        }

        if (ownerUserId == adminUserId)
        {
            return TypedResults.Problem(title: "Cannot act on your own tutor application.", statusCode: StatusCodes.Status403Forbidden);
        }

        var result = await handler.HandleAsync(id, request.Reason, ct);
        return result.IsSuccess
            ? TypedResults.Ok()
            : result.ToProblemDetails(StatusCodes.Status400BadRequest);
    }
}

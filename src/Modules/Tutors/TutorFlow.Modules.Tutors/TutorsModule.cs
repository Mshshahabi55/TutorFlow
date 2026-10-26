using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Infrastructure;
using TutorFlow.Modules.Tutors.Persistence;
using TutorFlow.Shared;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using ApproveApplicationFeature = TutorFlow.Modules.Tutors.Features.ApproveApplication.ApproveApplication;
using BeginReviewApplicationFeature = TutorFlow.Modules.Tutors.Features.BeginReviewApplication.BeginReviewApplication;
using CreateApplicationFeature = TutorFlow.Modules.Tutors.Features.CreateApplication.CreateApplication;
using DownloadCertificateDocumentFeature = TutorFlow.Modules.Tutors.Features.DownloadCertificateDocument.DownloadCertificateDocument;
using GetCurrentApplicationFeature = TutorFlow.Modules.Tutors.Features.GetCurrentApplication.GetCurrentApplication;
using GetCurrentStatusFeature = TutorFlow.Modules.Tutors.Features.GetCurrentStatus.GetCurrentStatus;
using GetPendingTutorApplicationsFeature = TutorFlow.Modules.Tutors.Features.GetPendingTutorApplications.GetPendingTutorApplications;
using GetPublicProfileFeature = TutorFlow.Modules.Tutors.Features.GetPublicProfile.GetPublicProfile;
using GetTutorApplicationForAdminFeature = TutorFlow.Modules.Tutors.Features.GetTutorApplicationForAdmin.GetTutorApplicationForAdmin;
using PublishApplicationFeature = TutorFlow.Modules.Tutors.Features.PublishApplication.PublishApplication;
using RejectApplicationFeature = TutorFlow.Modules.Tutors.Features.RejectApplication.RejectApplication;
using RejectCertificateFeature = TutorFlow.Modules.Tutors.Features.RejectCertificate.RejectCertificate;
using ReopenApplicationForRevisionFeature = TutorFlow.Modules.Tutors.Features.ReopenApplicationForRevision.ReopenApplicationForRevision;
using SubmitApplicationFeature = TutorFlow.Modules.Tutors.Features.SubmitApplication.SubmitApplication;
using SubmitCertificateDocumentFeature = TutorFlow.Modules.Tutors.Features.SubmitCertificateDocument.SubmitCertificateDocument;
using SuspendApplicationFeature = TutorFlow.Modules.Tutors.Features.SuspendApplication.SuspendApplication;
using UpdateDraftFeature = TutorFlow.Modules.Tutors.Features.UpdateDraft.UpdateDraft;
using UploadProfilePhotoFeature = TutorFlow.Modules.Tutors.Features.UploadProfilePhoto.UploadProfilePhoto;
using VerifyCertificateFeature = TutorFlow.Modules.Tutors.Features.VerifyCertificate.VerifyCertificate;
using WithdrawCertificateDocumentFeature = TutorFlow.Modules.Tutors.Features.WithdrawCertificateDocument.WithdrawCertificateDocument;

namespace TutorFlow.Modules.Tutors;

public static class TutorsModule
{
    public static IServiceCollection AddTutorsModule(this IServiceCollection services, IConfiguration config)
    {
        // A44: see BookingsModule's identical comment on AddDbContextWithWolverineIntegration.
        services.AddDbContextWithWolverineIntegration<TutorsDbContext>(
            options => options.UseNpgsql(config.GetConnectionString("TutorsDb"), npgsql => npgsql.EnableRetryOnFailure()),
            "tutors");

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

        // Local-disk photo storage (see TutorPhotoStorage's own doc comment for why: no
        // object-storage provider exists in this codebase, and no production deployment target
        // is defined yet either).
        services.Configure<TutorPhotoStorageOptions>(config.GetSection("TutorPhotoStorage"));
        services.AddScoped<UploadProfilePhotoFeature.Handler>();

        // Stage 2 (Certification) — private, non-static-served document storage; see
        // CertificateDocumentStorage's own doc comment for why this is deliberately not
        // TutorPhotoStorage's pattern. Signature/extension/size validation only — no real image
        // decode (SixLabors.ImageSharp was evaluated and explicitly declined; see the module's
        // production-readiness notes). The cleanup sweep is the retry mechanism for a failed
        // post-decision delete or an abandoned/crashed upload — same BackgroundService +
        // PeriodicTimer precedent as Notifications' AbandonedCheckoutClaimReconciliationService.
        services.Configure<CertificateDocumentStorageOptions>(config.GetSection("CertificateDocumentStorage"));
        services.AddScoped<ICertificateDocumentStorage, LocalCertificateDocumentStorage>();
        services.Configure<CertificateDocumentCleanupOptions>(config.GetSection("CertificateDocumentCleanup"));
        services.AddHostedService<CertificateDocumentCleanupService>();
        services.AddScoped<SubmitCertificateDocumentFeature.Handler>();
        services.AddScoped<WithdrawCertificateDocumentFeature.Handler>();
        services.AddScoped<DownloadCertificateDocumentFeature.Handler>();
        services.AddScoped<VerifyCertificateFeature.Handler>();
        services.AddScoped<RejectCertificateFeature.Handler>();
        services.AddScoped<IValidator<RejectCertificateFeature.Command>, RejectCertificateFeature.Validator>();

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
        // the Kit's caching skill names first (ADR-0022). Tagged "tutors" — VerifyCertificate and
        // RejectCertificate now call IOutputCacheStore.EvictByTagAsync("tutors") on every decision,
        // so a verification-badge change is visible immediately rather than waiting out the
        // 5-minute TTL below.
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

        group.MapPost("/me/photo", UploadProfilePhotoAsync)
            .WithName("UploadTutorProfilePhoto")
            .WithSummary("Upload a profile photo for the current user (JPEG/PNG/WebP, max 5 MB). Returns an absolute URL to pass to UpdateDraft's profilePhotoReference.")
            .Accepts<IFormFile>("multipart/form-data")
            // Minimal APIs require an explicit opt-out for any endpoint binding form data
            // (ASP.NET Core 10 default). No antiforgery middleware exists anywhere in this app —
            // every other mutating endpoint already relies solely on the auth cookie's
            // SameSite=Lax setting (AuthCookie.cs) for CSRF protection, which already blocks
            // cross-site POSTs; this endpoint is consistent with that existing posture, not a
            // new gap.
            .DisableAntiforgery();

        group.MapPost("/me/certificates/{certificateId:guid}/document", SubmitCertificateDocumentAsync)
            .WithName("SubmitCertificateDocument")
            .WithSummary("Upload/replace a document for one of the current user's own certificates (JPEG/PNG, max 20 MB). Always moves it to PendingReview.")
            .Accepts<IFormFile>("multipart/form-data")
            .DisableAntiforgery();

        group.MapDelete("/me/certificates/{certificateId:guid}/document", WithdrawCertificateDocumentAsync)
            .WithName("WithdrawCertificateDocument")
            .WithSummary("Withdraw a not-yet-decided or rejected certificate document without replacing it.");

        group.MapGet("/me/certificates/{certificateId:guid}/document", DownloadOwnCertificateDocumentAsync)
            .WithName("DownloadOwnCertificateDocument")
            .WithSummary("Stream the current user's own certificate document (private storage — never a public URL).");

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

        adminGroup.MapGet("/applications/{id:guid}/certificates/{certificateId:guid}/document", DownloadCertificateDocumentForAdminAsync)
            .WithName("DownloadCertificateDocumentForAdmin")
            .WithSummary("Stream a certificate document for admin review (private storage — never a public URL).");

        adminGroup.MapPost("/applications/{id:guid}/certificates/{certificateId:guid}/verify", VerifyCertificateAsync)
            .WithName("VerifyCertificate")
            .WithSummary("Approve a PendingReview certificate. Independent of application approval — never implied by it.");

        adminGroup.MapPost("/applications/{id:guid}/certificates/{certificateId:guid}/reject", RejectCertificateAsync)
            .WithName("RejectCertificate")
            .WithSummary("Reject a PendingReview certificate with a required reason. Notifies the tutor.")
            .AddEndpointFilter<ValidationFilter<RejectCertificateFeature.Command>>();

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

        try
        {
            var result = await handler.HandleAsync(userId, command, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : result.ToProblemDetails(StatusCodes.Status400BadRequest);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Audit finding F1 — another request (a certificate decision, another draft save)
            // committed against this same application between our read and our write. Letting
            // that silently overwrite the newer state is exactly the bug being fixed; 409 tells
            // the caller to reload and retry, matching this codebase's existing convention for
            // every other optimistic-conflict case (BookingsModule/AvailabilityModule).
            return TypedResults.Problem(
                title: "This application was updated by another request. Reload and try again.",
                statusCode: StatusCodes.Status409Conflict);
        }
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

    private static async Task<IResult> UploadProfilePhotoAsync(
        ClaimsPrincipal user,
        IFormFile? file,
        HttpRequest request,
        UploadProfilePhotoFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(userId, file, ct);
        if (result.IsFailure)
        {
            return result.ToProblemDetails(StatusCodes.Status400BadRequest);
        }

        var url = $"{request.Scheme}://{request.Host}/uploads/tutor-photos/{result.Value.FileName}";
        return TypedResults.Ok(new { url });
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
        IMessageBus bus,
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

        // A44 #103: dispatched via Wolverine (PublishApplicationConsumer), not called directly,
        // so the business write and the outgoing TutorPublished event commit atomically.
        var result = await bus.InvokeAsync<Result<GetCurrentApplicationFeature.Response>>(new PublishApplicationFeature.Command(id), ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> SuspendApplicationAsync(
        Guid id,
        SuspendApplicationFeature.Request request,
        ClaimsPrincipal admin,
        TutorsDbContext db,
        IMessageBus bus,
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

        // A44 #103: dispatched via Wolverine (SuspendApplicationConsumer), not called directly,
        // so the business write and the outgoing TutorUnpublished event commit atomically.
        var result = await bus.InvokeAsync<Result>(new SuspendApplicationFeature.Command(id, request.Reason, adminUserId), ct);
        return result.IsSuccess
            ? TypedResults.Ok()
            : result.ToProblemDetails(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> SubmitCertificateDocumentAsync(
        ClaimsPrincipal user,
        Guid certificateId,
        IFormFile? file,
        SubmitCertificateDocumentFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var result = await handler.HandleAsync(userId, certificateId, file, ct);
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : result.ToProblemDetails(StatusCodes.Status400BadRequest);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Audit finding F1 — see UpdateDraftAsync's identical catch for the full rationale.
            return TypedResults.Problem(
                title: "This application was updated by another request. Reload and try again.",
                statusCode: StatusCodes.Status409Conflict);
        }
    }

    private static async Task<IResult> WithdrawCertificateDocumentAsync(
        ClaimsPrincipal user,
        Guid certificateId,
        WithdrawCertificateDocumentFeature.Handler handler,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var result = await handler.HandleAsync(userId, certificateId, ct);
            // NoContent (204), not Ok() — the frontend's void-typed apiDelete only special-cases
            // 204 when deciding whether to skip JSON-parsing the response body; Ok() with no
            // value still writes a genuinely empty 200 body, which fetch's .json() throws on
            // (confirmed live: the withdraw succeeded server-side every time, but the UI falsely
            // reported failure before this fix).
            return result.IsSuccess
                ? TypedResults.NoContent()
                : result.ToProblemDetails(StatusCodes.Status400BadRequest);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Audit finding F1 — see UpdateDraftAsync's identical catch for the full rationale.
            return TypedResults.Problem(
                title: "This application was updated by another request. Reload and try again.",
                statusCode: StatusCodes.Status409Conflict);
        }
    }

    /// <summary>Private-storage streaming — never a redirect to a public URL, never
    /// <c>UseStaticFiles</c>. <c>Cache-Control: no-store</c> and an inline, filename-free
    /// Content-Disposition keep a confidential document out of any shared/browser cache and avoid
    /// ever echoing a client-controlled or original filename back in a response header.</summary>
    private static IResult StreamDocument(DownloadCertificateDocumentFeature.Document document)
    {
        return TypedResults.Stream(document.Content, document.ContentType, lastModified: null, entityTag: null);
    }

    private static async Task<IResult> DownloadOwnCertificateDocumentAsync(
        ClaimsPrincipal user,
        Guid certificateId,
        DownloadCertificateDocumentFeature.Handler handler,
        HttpResponse response,
        CancellationToken ct)
    {
        if (!user.TryGetUserId(out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleForTutorAsync(userId, certificateId, ct);
        if (result.IsFailure)
        {
            return result.ToProblemDetails(StatusCodes.Status404NotFound);
        }

        response.Headers.CacheControl = "no-store";
        response.Headers.ContentDisposition = "inline";
        return StreamDocument(result.Value);
    }

    private static async Task<IResult> DownloadCertificateDocumentForAdminAsync(
        Guid id,
        Guid certificateId,
        ClaimsPrincipal admin,
        TutorsDbContext db,
        DownloadCertificateDocumentFeature.Handler handler,
        HttpResponse response,
        CancellationToken ct)
    {
        // Audit finding F4 — every other admin action on an application (BeginReview/Approve/
        // Reject/Publish/Suspend/VerifyCertificate/RejectCertificate) already refuses to let an
        // admin act on their own tutor application; this download endpoint was the one
        // certificate-review action missing that same check. Viewing alone changes no state (an
        // admin-as-tutor could already view this exact document via their own
        // DownloadOwnCertificateDocumentAsync endpoint regardless), but the guard should be
        // applied consistently rather than carved out silently.
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

        var result = await handler.HandleForAdminAsync(id, certificateId, ct);
        if (result.IsFailure)
        {
            return result.ToProblemDetails(StatusCodes.Status404NotFound);
        }

        response.Headers.CacheControl = "no-store";
        response.Headers.ContentDisposition = "inline";
        return StreamDocument(result.Value);
    }

    private static async Task<IResult> VerifyCertificateAsync(
        Guid id,
        Guid certificateId,
        ClaimsPrincipal admin,
        TutorsDbContext db,
        VerifyCertificateFeature.Handler handler,
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

        try
        {
            var result = await handler.HandleAsync(id, certificateId, adminUserId, ct);
            // NoContent (204) — same reasoning as WithdrawCertificateDocumentAsync.
            return result.IsSuccess
                ? TypedResults.NoContent()
                : result.ToProblemDetails(StatusCodes.Status400BadRequest);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Audit finding F1 — another admin action (verify/reject on a different certificate,
            // a draft save) committed against this same application between our read and our
            // write. Without a concurrency token this would silently overwrite that decision;
            // 409 tells this caller their view is stale and the decision was NOT applied — see
            // UpdateDraftAsync's identical catch for the full rationale.
            return TypedResults.Problem(
                title: "This application was updated by another request. Reload and try again.",
                statusCode: StatusCodes.Status409Conflict);
        }
    }

    private static async Task<IResult> RejectCertificateAsync(
        Guid id,
        Guid certificateId,
        RejectCertificateFeature.Command command,
        ClaimsPrincipal admin,
        TutorsDbContext db,
        IMessageBus bus,
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

        try
        {
            // A44 #103: dispatched via Wolverine (RejectCertificateConsumer), not called
            // directly — see RejectCertificate.cs's own remarks on what this does and does not
            // make atomic.
            var result = await bus.InvokeAsync<Result>(
                new RejectCertificateFeature.Dispatch(id, certificateId, command.Reason, adminUserId), ct);
            // NoContent (204) — same reasoning as WithdrawCertificateDocumentAsync.
            return result.IsSuccess
                ? TypedResults.NoContent()
                : result.ToProblemDetails(StatusCodes.Status400BadRequest);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Audit finding F1 — see VerifyCertificateAsync's identical catch for the full
            // rationale.
            return TypedResults.Problem(
                title: "This application was updated by another request. Reload and try again.",
                statusCode: StatusCodes.Status409Conflict);
        }
    }
}

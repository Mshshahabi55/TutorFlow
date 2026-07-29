using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Commands;
using TutorFlow.Application.Identity.DTOs;
using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Identity.Queries;
using TutorFlow.Web.Authorization;

namespace TutorFlow.Web.Endpoints;

// Presentation-layer endpoints only: receive request, map request to
// Command, invoke the Application handler, return the Result (via
// ToApiResult — see ResultMapping.cs). No business rule, validation,
// authorization, repository access, or persistence logic is added here
// (docs/adr/ADR-010-api-boundary.md).
public static class IdentityEndpoints
{
    private const string Tag = "Identity";

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/tutors", async (
            RegisterTutorCommand command,
            RegisterTutorCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(command, cancellationToken))
                .ToApiResult(logger, nameof(RegisterTutorCommandHandler)))
            .WithApiResultMetadata<TutorDto>("RegisterTutor", Tag, "Registers a new Tutor account.");

        app.MapPost("/tutors/{tutorId:guid}/approve", async (
            Guid tutorId,
            ApproveTutorCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new ApproveTutorCommand(tutorId), cancellationToken))
                .ToApiResult(logger, nameof(ApproveTutorCommandHandler)))
            .WithApiResultMetadata("ApproveTutor", Tag, "Approves a pending Tutor, making them discoverable.")
            .RequirePermission(Permission.ApproveTutor);

        app.MapPost("/tutors/{tutorId:guid}/suspend", async (
            Guid tutorId,
            SuspendTutorCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SuspendTutorCommand(tutorId), cancellationToken))
                .ToApiResult(logger, nameof(SuspendTutorCommandHandler)))
            .WithApiResultMetadata("SuspendTutor", Tag, "Suspends a Tutor, removing discoverability and bookability.")
            .RequirePermission(Permission.SuspendTutor);

        app.MapPatch("/tutors/{tutorId:guid}/hourly-rate", async (
            Guid tutorId,
            SetHourlyRateRequest request,
            SetTutorHourlyRateCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SetTutorHourlyRateCommand(tutorId, request.Amount), cancellationToken))
                .ToApiResult(logger, nameof(SetTutorHourlyRateCommandHandler)))
            .WithApiResultMetadata("SetTutorHourlyRate", Tag, "Sets a Tutor's hourly rate.")
            .RequirePermission(Permission.ManageTutorOffering);

        app.MapPatch("/tutors/{tutorId:guid}/subject", async (
            Guid tutorId,
            SetSubjectRequest request,
            SetTutorSubjectCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SetTutorSubjectCommand(tutorId, request.Subject), cancellationToken))
                .ToApiResult(logger, nameof(SetTutorSubjectCommandHandler)))
            .WithApiResultMetadata("SetTutorSubject", Tag, "Sets a Tutor's subject.")
            .RequirePermission(Permission.ManageTutorOffering);

        app.MapPatch("/tutors/{tutorId:guid}/language", async (
            Guid tutorId,
            SetLanguageRequest request,
            SetTutorLanguageCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SetTutorLanguageCommand(tutorId, request.Language), cancellationToken))
                .ToApiResult(logger, nameof(SetTutorLanguageCommandHandler)))
            .WithApiResultMetadata("SetTutorLanguage", Tag, "Sets a Tutor's teaching language.")
            .RequirePermission(Permission.ManageTutorOffering);

        app.MapPatch("/tutors/{tutorId:guid}/location", async (
            Guid tutorId,
            SetLocationRequest request,
            SetTutorLocationCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SetTutorLocationCommand(tutorId, request.Location), cancellationToken))
                .ToApiResult(logger, nameof(SetTutorLocationCommandHandler)))
            .WithApiResultMetadata("SetTutorLocation", Tag, "Sets a Tutor's location.")
            .RequirePermission(Permission.ManageTutorOffering);

        app.MapPatch("/tutors/{tutorId:guid}/offered-durations", async (
            Guid tutorId,
            SetOfferedDurationsRequest request,
            SetTutorOfferedDurationsCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SetTutorOfferedDurationsCommand(tutorId, request.Durations), cancellationToken))
                .ToApiResult(logger, nameof(SetTutorOfferedDurationsCommandHandler)))
            .WithApiResultMetadata("SetTutorOfferedDurations", Tag, "Sets the session duration(s) a Tutor offers.")
            .RequirePermission(Permission.ManageTutorOffering);

        // ADR-024 (Accepted, 2026-07-28) — Tutor Onboarding Wizard. Every
        // endpoint below is Tutor-self-service, same ManageTutorOffering
        // permission + resource-ownership shape as the five PATCH endpoints
        // above.
        app.MapPatch("/tutors/{tutorId:guid}/personal-info", async (
            Guid tutorId,
            SetPersonalInfoRequest request,
            SetTutorPersonalInfoCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(
                new SetTutorPersonalInfoCommand(
                    tutorId, request.DisplayName, request.Headline, request.Biography,
                    request.Country, request.City, request.OtherLanguages),
                cancellationToken))
                .ToApiResult(logger, nameof(SetTutorPersonalInfoCommandHandler)))
            .WithApiResultMetadata("SetTutorPersonalInfo", Tag, "Sets a Tutor's personal-information profile fields.")
            .RequirePermission(Permission.ManageTutorOffering);

        app.MapPatch("/tutors/{tutorId:guid}/teaching-info", async (
            Guid tutorId,
            SetTeachingInfoRequest request,
            SetTutorTeachingInfoCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(
                new SetTutorTeachingInfoCommand(
                    tutorId, request.TutorSubjects, request.YearsOfExperience, request.Education,
                    request.Certifications, request.TeachingMethodology, request.LessonSpecialties),
                cancellationToken))
                .ToApiResult(logger, nameof(SetTutorTeachingInfoCommandHandler)))
            .WithApiResultMetadata("SetTutorTeachingInfo", Tag, "Sets a Tutor's teaching-information profile fields.")
            .RequirePermission(Permission.ManageTutorOffering);

        app.MapPatch("/tutors/{tutorId:guid}/media", async (
            Guid tutorId,
            SetMediaRequest request,
            SetTutorMediaCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(
                new SetTutorMediaCommand(tutorId, request.PhotoUrl, request.IntroVideoUrl, request.GalleryImageUrls),
                cancellationToken))
                .ToApiResult(logger, nameof(SetTutorMediaCommandHandler)))
            .WithApiResultMetadata("SetTutorMedia", Tag, "Sets a Tutor's profile media URLs.")
            .RequirePermission(Permission.ManageTutorOffering);

        app.MapPatch("/tutors/{tutorId:guid}/pricing", async (
            Guid tutorId,
            SetPricingRequest request,
            SetTutorPricingCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(
                new SetTutorPricingCommand(
                    tutorId, request.HourlyRateAmount, request.TrialLessonAvailable, request.TrialLessonPriceAmount),
                cancellationToken))
                .ToApiResult(logger, nameof(SetTutorPricingCommandHandler)))
            .WithApiResultMetadata("SetTutorPricing", Tag, "Sets a Tutor's hourly rate and trial-lesson pricing.")
            .RequirePermission(Permission.ManageTutorOffering);

        app.MapPost("/tutors/{tutorId:guid}/submit", async (
            Guid tutorId,
            SubmitTutorProfileCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new SubmitTutorProfileCommand(tutorId), cancellationToken))
                .ToApiResult(logger, nameof(SubmitTutorProfileCommandHandler)))
            .WithApiResultMetadata("SubmitTutorProfile", Tag, "Submits a Tutor's onboarding profile for Admin review.")
            .RequirePermission(Permission.ManageTutorOffering);

        app.MapPost("/students", async (
            RegisterStudentCommand command,
            RegisterStudentCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(command, cancellationToken))
                .ToApiResult(logger, nameof(RegisterStudentCommandHandler)))
            .WithApiResultMetadata<StudentDto>("RegisterStudent", Tag, "Registers a new Student account.");

        app.MapPost("/parent-guardians", async (
            RegisterParentGuardianCommand command,
            RegisterParentGuardianCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(command, cancellationToken))
                .ToApiResult(logger, nameof(RegisterParentGuardianCommandHandler)))
            .WithApiResultMetadata<ParentGuardianDto>("RegisterParentGuardian", Tag, "Registers a new Parent/Guardian account.");

        app.MapPost("/relationships", async (
            CreateRelationshipInvitationCommand command,
            CreateRelationshipInvitationCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(command, cancellationToken))
                .ToApiResult(logger, nameof(CreateRelationshipInvitationCommandHandler)))
            .WithApiResultMetadata<RelationshipDto>("CreateRelationshipInvitation", Tag, "Invites a Parent/Guardian-Student Relationship.")
            .RequirePermission(Permission.InviteRelationship);

        app.MapPost("/relationships/{relationshipId:guid}/confirm", async (
            Guid relationshipId,
            ConfirmRelationshipCommandHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new ConfirmRelationshipCommand(relationshipId), cancellationToken))
                .ToApiResult(logger, nameof(ConfirmRelationshipCommandHandler)))
            .WithApiResultMetadata("ConfirmRelationship", Tag, "Confirms an invited Relationship.")
            .RequirePermission(Permission.ConfirmRelationship);

        app.MapGet("/tutors/pending", async (
            GetPendingTutorsQueryHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken,
            int page = PageRequest.DefaultPage,
            int pageSize = PageRequest.DefaultPageSize) =>
            (await handler.Handle(new GetPendingTutorsQuery(page, pageSize), cancellationToken))
                .ToApiResult(logger, nameof(GetPendingTutorsQueryHandler)))
            .WithApiResultMetadata<PagedResult<TutorDto>>("GetPendingTutors", Tag, "Lists Tutors awaiting Admin approval.")
            .RequirePermission(Permission.ApproveTutor);

        app.MapGet("/tutors/{tutorId:guid}", async (
            Guid tutorId,
            GetTutorByIdQueryHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new GetTutorByIdQuery(tutorId), cancellationToken))
                .ToApiResult(logger, nameof(GetTutorByIdQueryHandler)))
            .WithApiResultMetadata<TutorDto>("GetTutorById", Tag, "Fetches a single Tutor by id.");

        app.MapGet("/tutors", async (
            GetTutorListQueryHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new GetTutorListQuery(), cancellationToken))
                .ToApiResult(logger, nameof(GetTutorListQueryHandler)))
            .WithApiResultMetadata<IReadOnlyCollection<TutorDto>>("GetTutorList", Tag, "Lists every discoverable Tutor.");

        app.MapGet("/students/{studentId:guid}", async (
            Guid studentId,
            GetStudentByIdQueryHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new GetStudentByIdQuery(studentId), cancellationToken))
                .ToApiResult(logger, nameof(GetStudentByIdQueryHandler)))
            .WithApiResultMetadata<StudentDto>("GetStudentById", Tag, "Fetches a single Student by id.");

        app.MapGet("/parent-guardians/{parentGuardianId:guid}", async (
            Guid parentGuardianId,
            GetParentGuardianByIdQueryHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new GetParentGuardianByIdQuery(parentGuardianId), cancellationToken))
                .ToApiResult(logger, nameof(GetParentGuardianByIdQueryHandler)))
            .WithApiResultMetadata<ParentGuardianDto>("GetParentGuardianById", Tag, "Fetches a single Parent/Guardian by id.");

        app.MapGet("/relationships/{relationshipId:guid}", async (
            Guid relationshipId,
            GetRelationshipByIdQueryHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new GetRelationshipByIdQuery(relationshipId), cancellationToken))
                .ToApiResult(logger, nameof(GetRelationshipByIdQueryHandler)))
            .WithApiResultMetadata<RelationshipDto>("GetRelationshipById", Tag, "Fetches a single Relationship by id.");

        app.MapGet("/accounts/{accountId:guid}/relationships", async (
            Guid accountId,
            GetRelationshipsByAccountIdQueryHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
            (await handler.Handle(new GetRelationshipsByAccountIdQuery(accountId), cancellationToken))
                .ToApiResult(logger, nameof(GetRelationshipsByAccountIdQueryHandler)))
            .WithApiResultMetadata<IReadOnlyCollection<RelationshipDto>>("GetRelationshipsByAccountId", Tag, "Lists every Relationship an account participates in.");

        return app;
    }

    // Minimal request shapes needed only because these commands' TutorId is
    // bound from the route, not the body — the "map request → Command" step,
    // not business logic.
    internal sealed record SetHourlyRateRequest(decimal Amount);

    internal sealed record SetSubjectRequest(string Subject);

    internal sealed record SetLanguageRequest(string Language);

    internal sealed record SetLocationRequest(string Location);

    internal sealed record SetOfferedDurationsRequest(IReadOnlyCollection<TimeSpan> Durations);

    // ADR-024 (Accepted, 2026-07-28) — Tutor Onboarding Wizard request shapes.
    internal sealed record SetPersonalInfoRequest(
        string? DisplayName,
        string? Headline,
        string? Biography,
        string? Country,
        string? City,
        IReadOnlyCollection<string>? OtherLanguages);

    internal sealed record SetTeachingInfoRequest(
        IReadOnlyCollection<TutorSubjectInput>? TutorSubjects,
        int? YearsOfExperience,
        string? Education,
        string? Certifications,
        string? TeachingMethodology,
        IReadOnlyCollection<string>? LessonSpecialties);

    internal sealed record SetMediaRequest(
        string? PhotoUrl,
        string? IntroVideoUrl,
        IReadOnlyCollection<string>? GalleryImageUrls);

    internal sealed record SetPricingRequest(
        decimal? HourlyRateAmount,
        bool TrialLessonAvailable,
        decimal? TrialLessonPriceAmount);
}

using TutorFlow.Application.Audit.Handlers;
using TutorFlow.Application.Authorization;
using TutorFlow.Application.Communication.Handlers;
using TutorFlow.Application.Discovery.Handlers;
using TutorFlow.Application.Meetings.Handlers;
using TutorFlow.Application.Identity.Handlers;
using TutorFlow.Application.Oversight.Handlers;
using TutorFlow.Application.Scheduling.Handlers;
using TutorFlow.Web.Endpoints;

namespace TutorFlow.Web.DependencyInjection;

public static class ApplicationEndpointRegistration
{
    public static IServiceCollection AddApplicationHandlers(this IServiceCollection services)
    {
        services.AddScoped<RegisterTutorCommandHandler>();
        services.AddScoped<ApproveTutorCommandHandler>();
        services.AddScoped<SuspendTutorCommandHandler>();
        services.AddScoped<RegisterStudentCommandHandler>();
        services.AddScoped<RegisterParentGuardianCommandHandler>();
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<LogoutCommandHandler>();
        services.AddScoped<AdminResetPasswordCommandHandler>();
        services.AddScoped<CreateRelationshipInvitationCommandHandler>();
        services.AddScoped<ConfirmRelationshipCommandHandler>();
        services.AddScoped<GetTutorByIdQueryHandler>();
        services.AddScoped<GetTutorListQueryHandler>();
        services.AddScoped<GetStudentByIdQueryHandler>();
        services.AddScoped<GetRelationshipByIdQueryHandler>();

        services.AddScoped<SetTutorHourlyRateCommandHandler>();
        services.AddScoped<SetTutorSubjectCommandHandler>();
        services.AddScoped<SetTutorLanguageCommandHandler>();
        services.AddScoped<SetTutorLocationCommandHandler>();
        services.AddScoped<SetTutorOfferedDurationsCommandHandler>();
        // ADR-024 (Accepted, 2026-07-28) — Tutor Onboarding Wizard.
        services.AddScoped<SetTutorPersonalInfoCommandHandler>();
        services.AddScoped<SetTutorTeachingInfoCommandHandler>();
        services.AddScoped<SetTutorMediaCommandHandler>();
        services.AddScoped<SetTutorPricingCommandHandler>();
        services.AddScoped<SubmitTutorProfileCommandHandler>();
        services.AddScoped<GetPendingTutorsQueryHandler>();
        services.AddScoped<GetParentGuardianByIdQueryHandler>();
        services.AddScoped<GetRelationshipsByAccountIdQueryHandler>();

        services.AddScoped<DeclareAvailabilityCommandHandler>();
        services.AddScoped<BookSessionCommandHandler>();
        services.AddScoped<RescheduleSessionCommandHandler>();
        services.AddScoped<CancelSessionCommandHandler>();
        services.AddScoped<CompleteSessionCommandHandler>();
        services.AddScoped<MarkSessionNoShowCommandHandler>();
        services.AddScoped<GetAvailabilitySlotByIdQueryHandler>();
        services.AddScoped<GetSessionByIdQueryHandler>();
        services.AddScoped<GetStudentScheduleQueryHandler>();
        services.AddScoped<GetTutorScheduleQueryHandler>();
        services.AddScoped<GetTutorAvailabilitySlotsQueryHandler>();

        services.AddScoped<SearchTutorsQueryHandler>();

        services.AddScoped<GetAllSessionsQueryHandler>();

        services.AddScoped<GetAuditEntriesQueryHandler>();

        services.AddScoped<StartConversationCommandHandler>();
        services.AddScoped<SendMessageCommandHandler>();
        services.AddScoped<MarkConversationReadCommandHandler>();
        services.AddScoped<MarkNotificationReadCommandHandler>();
        services.AddScoped<MarkAllNotificationsReadCommandHandler>();
        services.AddScoped<GetMyConversationsQueryHandler>();
        services.AddScoped<GetConversationMessagesQueryHandler>();
        services.AddScoped<GetMyNotificationsQueryHandler>();

        services.AddScoped<CreateMeetingCommandHandler>();
        services.AddScoped<GetMeetingBySessionQueryHandler>();
        services.AddScoped<GetActiveMeetingForConversationQueryHandler>();

        // Permission evaluation (Launch Preparation, Priority 2, WP2) —
        // stateless, so Singleton; not yet consumed by any endpoint
        // (endpoint protection is WP4's scope), registered now so WP3/WP4
        // can inject it without a later registration change.
        services.AddSingleton<IPermissionEvaluator, PermissionEvaluator>();

        return services;
    }

    public static IEndpointRouteBuilder MapApplicationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapIdentityEndpoints();
        app.MapAuthEndpoints();
        app.MapSchedulingEndpoints();
        app.MapDiscoveryEndpoints();
        app.MapOversightEndpoints();
        app.MapAuditEndpoints();
        app.MapCommunicationEndpoints();
        app.MapMeetingEndpoints();

        return app;
    }
}

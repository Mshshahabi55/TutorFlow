using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TutorFlow.Application.Audit.Interfaces;
using TutorFlow.Application.Common;
using TutorFlow.Application.Communication.Interfaces;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Meetings.Interfaces;
using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Domain.Meetings.ValueObjects;
using TutorFlow.Infrastructure.Audit;
using TutorFlow.Infrastructure.Common;
using TutorFlow.Infrastructure.Communication;
using TutorFlow.Infrastructure.Communication.Repositories;
using TutorFlow.Infrastructure.Identity.Repositories;
using TutorFlow.Infrastructure.Meetings;
using TutorFlow.Infrastructure.Meetings.Configuration;
using TutorFlow.Infrastructure.Meetings.Providers;
using TutorFlow.Infrastructure.Meetings.Repositories;
using TutorFlow.Infrastructure.Persistence;
using TutorFlow.Infrastructure.Scheduling.Configuration;
using TutorFlow.Infrastructure.Scheduling.Repositories;

namespace TutorFlow.Infrastructure;

public static class DependencyInjection
{
    // configureDbContext: the composition root (Web) supplies the provider —
    // UseNpgsql(connectionString) in production, per
    // docs/adr/ADR-013-persistence-technology.md. Kept as a delegate rather
    // than a hard-coded UseNpgsql call so a test host can substitute a
    // different provider (e.g., SQLite) without this method ever
    // registering Npgsql's services in the first place.
    // isDevelopment: docs/adr/ADR-023-...'s MockMeetingProvider is
    // registered only when true, mirroring DevelopmentSeeder's own
    // environment guard — Program.cs supplies builder.Environment.IsDevelopment().
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, Action<DbContextOptionsBuilder> configureDbContext, bool isDevelopment)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // HttpCurrentUserProvider is the real, request-populated identity
        // (docs/adr/ADR-017-authentication-mechanism-decision.md);
        // registered under both its concrete type (AuthenticationMiddleware
        // needs the settable surface) and ICurrentUserProvider (everything
        // else), resolving to the same Scoped instance per request.
        // NullCurrentUserProvider remains available as an explicit
        // "nobody is authenticated" test double, just no longer the default.
        services.AddScoped<HttpCurrentUserProvider>();
        services.AddScoped<ICurrentUserProvider>(sp => sp.GetRequiredService<HttpCurrentUserProvider>());
        services.AddScoped<ICurrentUserWriter>(sp => sp.GetRequiredService<HttpCurrentUserProvider>());

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenGenerator, TokenGenerator>();

        // Implements docs/adr/ADR-012-domain-event-dispatch-semantics.md's
        // four decisions.
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // The first real IDomainEventHandler, per accepted
        // docs/adr/ADR-016-audit-durability-strategy.md. Scoped to match
        // TutorFlowDbContext's own lifetime — same-transaction durability
        // depends on this handler and EfUnitOfWork sharing the exact same
        // DbContext instance within a request.
        services.AddScoped<IDomainEventHandler, AuditDomainEventHandler>();

        // Second IDomainEventHandler, per docs/adr/ADR-022-communication-and-notifications-architecture.md
        // — DomainEventDispatcher already fans out to every registered
        // handler, so this runs alongside AuditDomainEventHandler, not
        // instead of it.
        services.AddScoped<IDomainEventHandler, NotificationDomainEventHandler>();

        // Third IDomainEventHandler, per docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md
        // — best-effort, non-blocking sync of an existing Meeting when its
        // Session reschedules/cancels (see that class's own remarks).
        services.AddScoped<IDomainEventHandler, MeetingSyncDomainEventHandler>();

        services.AddDbContext<TutorFlowDbContext>(configureDbContext);

        // Real persistence (docs/adr/ADR-013-persistence-technology.md).
        // Scoped, matching TutorFlowDbContext's own default lifetime — a
        // DbContext is not thread-safe and must not be shared across
        // requests, unlike the temporary in-memory Singletons this replaces.
        services.AddScoped<ITutorRepository, TutorRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IParentGuardianRepository, ParentGuardianRepository>();
        services.AddScoped<IAdminStaffRepository, AdminStaffRepository>();
        services.AddScoped<IAuthTokenRepository, AuthTokenRepository>();
        services.AddScoped<IRelationshipRepository, RelationshipRepository>();
        services.AddScoped<IAvailabilitySlotRepository, AvailabilitySlotRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IMeetingRepository, MeetingRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        // docs/adr/ADR-025-... Addendum — Booking Notice & Horizon (Accepted
        // 2026-07-29). Same ValidateOnStart discipline as MeetingProviderSettings
        // below: a misconfigured section fails at host startup, not on the
        // first real booking attempt.
        services.AddOptions<SchedulingConstraintsSettings>()
            .BindConfiguration(SchedulingConstraintsSettings.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SchedulingConstraintsSettings>, SchedulingConstraintsSettingsValidator>();
        services.AddScoped<ISchedulingConstraintsProvider, SchedulingConstraintsProvider>();

        // docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md —
        // one configuration section, no hardcoded credentials (CLAUDE.md).
        // ValidateOnStart forces the binder (including DefaultProvider's
        // enum parse) to run during host startup rather than lazily on the
        // first "Start Lesson" request — a typo'd provider name fails fast,
        // the same principle Program.cs already applies to a placeholder
        // connection string.
        services.AddOptions<MeetingProviderSettings>()
            .BindConfiguration(MeetingProviderSettings.SectionName)
            .ValidateOnStart();
        services.AddScoped<IMeetingProviderSettingsCatalog, MeetingProviderSettingsCatalog>();
        services.AddScoped<IMeetingProviderResolver, MeetingProviderResolver>();

        // Each real provider is a typed HttpClient (pooled connection
        // lifetime managed by IHttpClientFactory) plus a keyed IMeetingProvider
        // registration resolving to that same typed client instance —
        // IMeetingProviderResolver is the only place that ever asks for a
        // specific key. Adding a future provider (Cisco Webex, Jitsi Meet,
        // BigBlueButton, ...) means exactly these two lines, nothing else.
        services.AddHttpClient<GoogleMeetProvider>();
        services.AddKeyedScoped<IMeetingProvider>(
            MeetingProviderOption.GoogleMeet, (sp, _) => sp.GetRequiredService<GoogleMeetProvider>());

        services.AddHttpClient<MicrosoftTeamsProvider>();
        services.AddKeyedScoped<IMeetingProvider>(
            MeetingProviderOption.MicrosoftTeams, (sp, _) => sp.GetRequiredService<MicrosoftTeamsProvider>());

        services.AddHttpClient<ZoomProvider>();
        services.AddKeyedScoped<IMeetingProvider>(
            MeetingProviderOption.Zoom, (sp, _) => sp.GetRequiredService<ZoomProvider>());

        // MockMeetingProvider — Development only (docs/adr/ADR-023-...;
        // mirrors DevelopmentSeeder's own environment guard).
        if (isDevelopment)
        {
            services.AddKeyedScoped<IMeetingProvider, MockMeetingProvider>(MeetingProviderOption.Mock);
        }

        // Read-only access to the audit trail ADR-016 already writes
        // (Backend Completion Phase, Track A, Phase A1).
        services.AddScoped<IAuditEntryRepository, AuditEntryRepository>();

        // Session cleanup (Launch Preparation, Priority 1) — registered here,
        // not from Program.cs, since it references an Infrastructure-internal
        // type; runs in every environment including Testing, which is safe
        // (see AuthTokenCleanupService's own remarks).
        services.AddHostedService<AuthTokenCleanupService>();

        return services;
    }
}

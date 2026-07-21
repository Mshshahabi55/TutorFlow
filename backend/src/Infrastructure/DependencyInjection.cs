using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Application.Audit.Interfaces;
using TutorFlow.Application.Common;
using TutorFlow.Application.Identity.Interfaces;
using TutorFlow.Application.Scheduling.Interfaces;
using TutorFlow.Infrastructure.Audit;
using TutorFlow.Infrastructure.Common;
using TutorFlow.Infrastructure.Identity.Repositories;
using TutorFlow.Infrastructure.Persistence;
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
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDbContext)
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
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

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

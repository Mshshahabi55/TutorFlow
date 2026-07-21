using TutorFlow.Application.Audit.DTOs;
using TutorFlow.Application.Audit.Handlers;
using TutorFlow.Application.Audit.Queries;
using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Web.Authorization;

namespace TutorFlow.Web.Endpoints;

// Presentation-layer endpoints only — see IdentityEndpoints. Exposes the
// audit trail ADR-016 already writes; introduces no new capability beyond
// reading it back (Backend Completion Phase, Track A, Phase A1). Not folded
// into OversightEndpoints: per ADR-009, audit is "not modeled as a fifth,
// ownerless bounded context," but it is also not Marketplace Oversight's own
// data — it is cross-cutting, derived data belonging to whichever context
// raised the underlying event. Its own endpoint file reflects that it isn't
// naturally either.
public static class AuditEndpoints
{
    private const string Tag = "Audit";

    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/audit-entries", async (
            GetAuditEntriesQueryHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken,
            Guid? subjectId = null,
            int page = PageRequest.DefaultPage,
            int pageSize = PageRequest.DefaultPageSize) =>
            (await handler.Handle(new GetAuditEntriesQuery(subjectId, page, pageSize), cancellationToken))
                .ToApiResult(logger, nameof(GetAuditEntriesQueryHandler)))
            .WithApiResultMetadata<PagedResult<AuditEntryDto>>(
                "GetAuditEntries", Tag, "Lists audit entries, optionally filtered by subject id.")
            .RequirePermission(Permission.ViewAuditEntries);

        return app;
    }
}

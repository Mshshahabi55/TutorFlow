using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Application.Oversight.Handlers;
using TutorFlow.Application.Oversight.Queries;
using TutorFlow.Application.Scheduling.DTOs;
using TutorFlow.Web.Authorization;

namespace TutorFlow.Web.Endpoints;

// Presentation-layer endpoints only — see IdentityEndpoints. First real
// endpoint for the Marketplace Oversight Application module (ARCHITECTURE.md
// Section 4).
public static class OversightEndpoints
{
    private const string Tag = "Oversight";

    public static IEndpointRouteBuilder MapOversightEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/sessions", async (
            GetAllSessionsQueryHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken,
            int page = PageRequest.DefaultPage,
            int pageSize = PageRequest.DefaultPageSize) =>
            (await handler.Handle(new GetAllSessionsQuery(page, pageSize), cancellationToken))
                .ToApiResult(logger, nameof(GetAllSessionsQueryHandler)))
            .WithApiResultMetadata<PagedResult<SessionDto>>(
                "GetAllSessions", Tag, "Lists every Session across the platform (ADM-3: view all schedules).")
            .RequirePermission(Permission.ViewAllSchedules);

        return app;
    }
}

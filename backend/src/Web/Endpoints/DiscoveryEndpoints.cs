using TutorFlow.Application.Common;
using TutorFlow.Application.Discovery.Handlers;
using TutorFlow.Application.Discovery.Queries;
using TutorFlow.Application.Identity.DTOs;

namespace TutorFlow.Web.Endpoints;

// Presentation-layer endpoints only — see IdentityEndpoints. First real
// endpoint for the Discovery Application module (ARCHITECTURE.md Section 4).
public static class DiscoveryEndpoints
{
    private const string Tag = "Discovery";

    public static IEndpointRouteBuilder MapDiscoveryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/tutors/search", async (
            SearchTutorsQueryHandler handler,
            ILogger<Program> logger,
            CancellationToken cancellationToken,
            string? subject = null,
            string? language = null,
            string? location = null,
            DateTime? availableFrom = null,
            int page = PageRequest.DefaultPage,
            int pageSize = PageRequest.DefaultPageSize) =>
            (await handler.Handle(
                new SearchTutorsQuery(subject, language, location, availableFrom, page, pageSize),
                cancellationToken))
                .ToApiResult(logger, nameof(SearchTutorsQueryHandler)))
            .WithApiResultMetadata<PagedResult<TutorDto>>(
                "SearchTutors", Tag, "Searches discoverable Tutors by subject, language, location, and availability.");

        return app;
    }
}

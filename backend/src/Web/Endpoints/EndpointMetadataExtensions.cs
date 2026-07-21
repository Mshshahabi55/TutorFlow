namespace TutorFlow.Web.Endpoints;

// Shared OpenAPI annotation for every endpoint's uniform result envelope
// (Backend Gap Closure Plan, Sprint 3: OpenAPI Response-Type Annotations).
// Declares the full set of outcomes ResultMapping.StatusCodeFor can ever
// produce; a specific endpoint may not reach every one of them today, but
// none is inaccurate — this is the same, honest envelope every endpoint
// already shares. Tooling/documentation only — no business decision, no
// change to any endpoint's actual behavior.
internal static class EndpointMetadataExtensions
{
    public static RouteHandlerBuilder WithApiResultMetadata<TValue>(
        this RouteHandlerBuilder builder, string name, string tag, string summary) =>
        builder
            .WithName(name)
            .WithTags(tag)
            .WithSummary(summary)
            .Produces<ApiResponse<TValue>>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status409Conflict)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

    public static RouteHandlerBuilder WithApiResultMetadata(
        this RouteHandlerBuilder builder, string name, string tag, string summary) =>
        builder
            .WithName(name)
            .WithSummary(summary)
            .WithTags(tag)
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status409Conflict)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
}

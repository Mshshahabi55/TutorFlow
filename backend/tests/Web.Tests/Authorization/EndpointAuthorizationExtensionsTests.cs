using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TutorFlow.Application.Authorization;
using TutorFlow.Web.Authorization;

namespace TutorFlow.Web.Tests.Authorization;

// Verifies RequirePermission(...) attaches metadata AuthorizationMiddleware
// can actually read back off a real, routed Endpoint — not just that the
// extension method compiles (Launch Preparation, Priority 2, WP3).
public class EndpointAuthorizationExtensionsTests
{
    private static RouteEndpoint MapAndBuild(Action<IEndpointRouteBuilder> map, string routePattern)
    {
        var app = WebApplication.CreateBuilder().Build();
        IEndpointRouteBuilder routeBuilder = app;

        map(routeBuilder);

        return routeBuilder.DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.RoutePattern.RawText == routePattern);
    }

    [Fact]
    public void RequirePermission_attaches_RequiredPermissionMetadata_carrying_the_given_permission()
    {
        var endpoint = MapAndBuild(
            routes => routes.MapGet("/test/protected", () => Results.Ok()).RequirePermission(Permission.ApproveTutor),
            "/test/protected");

        var metadata = endpoint.Metadata.GetMetadata<RequiredPermissionMetadata>();

        Assert.NotNull(metadata);
        Assert.Equal(Permission.ApproveTutor, metadata!.Permission);
    }

    [Fact]
    public void An_endpoint_that_never_calls_RequirePermission_carries_no_RequiredPermissionMetadata()
    {
        var endpoint = MapAndBuild(
            routes => routes.MapGet("/test/unprotected", () => Results.Ok()),
            "/test/unprotected");

        Assert.Null(endpoint.Metadata.GetMetadata<RequiredPermissionMetadata>());
    }
}

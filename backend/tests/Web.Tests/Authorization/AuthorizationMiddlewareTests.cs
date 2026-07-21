using System.Text.Json;
using Microsoft.AspNetCore.Http;
using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Web.Authorization;
using TutorFlow.Web.Middleware;

namespace TutorFlow.Web.Tests.Authorization;

// Unit tests against AuthorizationMiddleware.InvokeAsync directly — no
// endpoint calls RequirePermission(...) yet (WP4's scope, not WP3's), so
// there is no real protected route through the full HTTP pipeline to
// exercise; the middleware's own decision logic is fully exercised here
// instead (Launch Preparation, Priority 2, WP3).
public class AuthorizationMiddlewareTests
{
    private sealed class StubCurrentUserProvider : ICurrentUserProvider
    {
        public bool IsAuthenticated { get; init; }

        public string? UserId { get; init; }

        public string? Role { get; init; }
    }

    private static HttpContext CreateContext(RequiredPermissionMetadata? metadata)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var metadataCollection = metadata is null
            ? EndpointMetadataCollection.Empty
            : new EndpointMetadataCollection(metadata);
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, metadataCollection, "test-endpoint"));

        return context;
    }

    private static async Task<JsonElement> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return document.RootElement.Clone();
    }

    [Fact]
    public async Task Passes_through_unchanged_when_the_endpoint_has_no_RequiredPermissionMetadata()
    {
        var context = CreateContext(metadata: null);
        var nextCalled = false;
        var middleware = new AuthorizationMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(
            context,
            new StubCurrentUserProvider { IsAuthenticated = false },
            new PermissionEvaluator());

        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task Returns_401_when_the_endpoint_is_protected_and_the_caller_is_unauthenticated()
    {
        var context = CreateContext(new RequiredPermissionMetadata(Permission.CancelSession));
        var nextCalled = false;
        var middleware = new AuthorizationMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(
            context,
            new StubCurrentUserProvider { IsAuthenticated = false },
            new PermissionEvaluator());

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);

        var body = await ReadBodyAsync(context);
        Assert.False(body.GetProperty("isSuccess").GetBoolean());
        Assert.Equal("Authorization.Unauthenticated", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Returns_403_when_the_authenticated_role_lacks_the_required_permission()
    {
        var context = CreateContext(new RequiredPermissionMetadata(Permission.ApproveTutor));
        var nextCalled = false;
        var middleware = new AuthorizationMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(
            context,
            new StubCurrentUserProvider { IsAuthenticated = true, UserId = Guid.NewGuid().ToString(), Role = "Tutor" },
            new PermissionEvaluator());

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);

        var body = await ReadBodyAsync(context);
        Assert.False(body.GetProperty("isSuccess").GetBoolean());
        Assert.Equal("Authorization.Forbidden", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Returns_403_when_the_role_string_does_not_parse_to_a_known_Role()
    {
        var context = CreateContext(new RequiredPermissionMetadata(Permission.CancelSession));
        var middleware = new AuthorizationMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(
            context,
            new StubCurrentUserProvider { IsAuthenticated = true, UserId = Guid.NewGuid().ToString(), Role = "NotARealRole" },
            new PermissionEvaluator());

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task Calls_next_when_the_authenticated_role_holds_the_required_permission()
    {
        var context = CreateContext(new RequiredPermissionMetadata(Permission.CancelSession));
        var nextCalled = false;
        var middleware = new AuthorizationMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(
            context,
            new StubCurrentUserProvider { IsAuthenticated = true, UserId = Guid.NewGuid().ToString(), Role = "Tutor" },
            new PermissionEvaluator());

        Assert.True(nextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }
}

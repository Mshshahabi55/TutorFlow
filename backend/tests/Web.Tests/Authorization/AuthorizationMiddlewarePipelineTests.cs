using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Web.Authorization;
using TutorFlow.Web.Endpoints;
using TutorFlow.Web.Middleware;

namespace TutorFlow.Web.Tests.Authorization;

// AuthorizationMiddlewareTests exercises InvokeAsync directly with a
// hand-built HttpContext; that proves the decision logic but not that
// context.GetEndpoint() actually resolves RequiredPermissionMetadata at
// AuthorizationMiddleware's real position in the pipeline (no production
// endpoint calls RequirePermission(...) yet, so Program.cs's own pipeline
// can't be used to prove this — every request there takes the
// no-metadata passthrough branch regardless of whether routing genuinely
// ran first). This builds a minimal standalone WebApplication with the
// same middleware/routing shape and a real RequirePermission(...) endpoint,
// through Microsoft.AspNetCore.TestHost, to close that gap
// (Launch Preparation, Priority 2, WP3).
public class AuthorizationMiddlewarePipelineTests
{
    private sealed class StubCurrentUserProvider : ICurrentUserProvider
    {
        public bool IsAuthenticated { get; set; }

        public string? UserId { get; set; }

        public string? Role { get; set; }
    }

    private static async Task<(HttpClient Client, StubCurrentUserProvider CurrentUser, IHost Host)> StartAppAsync()
    {
        var currentUser = new StubCurrentUserProvider();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<ICurrentUserProvider>(currentUser);
        builder.Services.AddSingleton<IPermissionEvaluator, PermissionEvaluator>();

        var app = builder.Build();
        app.UseMiddleware<AuthorizationMiddleware>();
        app.MapGet("/protected/cancel-session", () => Results.Ok()).RequirePermission(Permission.CancelSession);
        app.MapGet("/protected/approve-tutor", () => Results.Ok()).RequirePermission(Permission.ApproveTutor);
        app.MapGet("/unprotected", () => Results.Ok());

        await app.StartAsync();
        return (app.GetTestClient(), currentUser, app);
    }

    [Fact]
    public async Task An_unprotected_route_is_reachable_with_no_identity_at_all()
    {
        var (client, _, host) = await StartAppAsync();
        using var _ = host;

        var response = await client.GetAsync("/unprotected");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_RequirePermission_route_returns_401_for_an_unauthenticated_caller()
    {
        var (client, currentUser, host) = await StartAppAsync();
        using var _ = host;
        currentUser.IsAuthenticated = false;

        var response = await client.GetAsync("/protected/cancel-session");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>();
        Assert.Equal("Authorization.Unauthenticated", body!.Error!.Code);
    }

    [Fact]
    public async Task A_RequirePermission_route_returns_403_for_a_role_lacking_the_permission()
    {
        var (client, currentUser, host) = await StartAppAsync();
        using var _ = host;
        currentUser.IsAuthenticated = true;
        currentUser.UserId = Guid.NewGuid().ToString();
        currentUser.Role = "Student"; // Student does not hold ApproveTutor.

        var response = await client.GetAsync("/protected/approve-tutor");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>();
        Assert.Equal("Authorization.Forbidden", body!.Error!.Code);
    }

    [Fact]
    public async Task A_RequirePermission_route_returns_200_for_a_role_holding_the_permission()
    {
        var (client, currentUser, host) = await StartAppAsync();
        using var _ = host;
        currentUser.IsAuthenticated = true;
        currentUser.UserId = Guid.NewGuid().ToString();
        currentUser.Role = "Tutor"; // Tutor holds CancelSession.

        var response = await client.GetAsync("/protected/cancel-session");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

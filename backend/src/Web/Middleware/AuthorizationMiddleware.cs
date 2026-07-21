using TutorFlow.Application.Authorization;
using TutorFlow.Application.Common;
using TutorFlow.Web.Authorization;
using TutorFlow.Web.Endpoints;

namespace TutorFlow.Web.Middleware;

// Enforces only the coarse-grained (Role -> Permission) half of ADR-003's
// two-tier authorization model, and only against endpoints that opt in via
// RequirePermission(...) (Launch Preparation, Priority 2, WP3). An endpoint
// carrying no RequiredPermissionMetadata passes through unaffected — true of
// every endpoint today, since endpoint-by-endpoint protection is WP4's
// scope, not WP3's. Fine-grained, resource-instance checks (e.g. ADR-003's
// Addendum Decision 1: "the Tutor assigned to *that* Session") are never
// attempted here; per ADR-003's Resource Ownership rules they remain the
// responsibility of the owning bounded context's own Application-layer
// handler, which still runs after this middleware has let a request through.
// Must run after AuthenticationMiddleware (Program.cs), since it depends on
// ICurrentUserProvider already being populated for this request, and after
// routing has resolved context.GetEndpoint() and its metadata.
internal sealed class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public AuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUserProvider currentUserProvider,
        IPermissionEvaluator permissionEvaluator)
    {
        var metadata = context.GetEndpoint()?.Metadata.GetMetadata<RequiredPermissionMetadata>();
        if (metadata is null)
        {
            await _next(context);
            return;
        }

        if (!currentUserProvider.IsAuthenticated)
        {
            await WriteFailureAsync(
                context,
                StatusCodes.Status401Unauthorized,
                new ApiError(
                    "Authorization.Unauthenticated",
                    "Authentication is required to access this resource.",
                    ErrorType.Domain,
                    context.TraceIdentifier));
            return;
        }

        var role = currentUserProvider.Role.ToRole();
        if (role is null || !permissionEvaluator.HasPermission(role.Value, metadata.Permission))
        {
            await WriteFailureAsync(
                context,
                StatusCodes.Status403Forbidden,
                new ApiError(
                    "Authorization.Forbidden",
                    "You do not have permission to perform this action.",
                    ErrorType.Domain,
                    context.TraceIdentifier));
            return;
        }

        await _next(context);
    }

    private static async Task WriteFailureAsync(HttpContext context, int statusCode, ApiError error)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiResponse.Fail(error));
    }
}

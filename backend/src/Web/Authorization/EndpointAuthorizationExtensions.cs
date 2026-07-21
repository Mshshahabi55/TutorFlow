using TutorFlow.Application.Authorization;

namespace TutorFlow.Web.Authorization;

// The single call site every endpoint uses to opt into coarse-grained
// authorization enforcement (Launch Preparation, Priority 2, WP3). Built
// here, but not yet called from any endpoint file — endpoint-by-endpoint
// protection is WP4's scope, not WP3's. Calling this on zero endpoints
// today means AuthorizationMiddleware finds no RequiredPermissionMetadata
// anywhere and every current endpoint's behavior is unchanged.
public static class EndpointAuthorizationExtensions
{
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, Permission permission) =>
        builder.WithMetadata(new RequiredPermissionMetadata(permission));
}

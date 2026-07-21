using TutorFlow.Application.Authorization;

namespace TutorFlow.Web.Authorization;

// Endpoint metadata naming the single coarse-grained (role-based) Permission
// a caller must hold to reach this endpoint's handler
// (docs/adr/ADR-003-authentication-and-authorization.md: two-tier model —
// this expresses only the role-only half; any resource-instance-scoped
// check an endpoint additionally needs is evaluated separately, inside the
// owning bounded context's own Application-layer handler, never here).
// Presence of this metadata is exactly what AuthorizationMiddleware keys
// off to decide whether an endpoint is protected at all — an endpoint with
// none of this metadata is unaffected by AuthorizationMiddleware.
public sealed class RequiredPermissionMetadata
{
    public RequiredPermissionMetadata(Permission permission)
    {
        Permission = permission;
    }

    public Permission Permission { get; }
}

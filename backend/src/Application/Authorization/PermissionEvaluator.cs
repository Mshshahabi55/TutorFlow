using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Authorization;

// Implementation kept in Application, not deferred to Infrastructure —
// deliberately, a departure from this codebase's usual interface-in-
// Application/implementation-in-Infrastructure split (e.g. IPasswordHasher,
// ITokenGenerator). RolePermissionCatalog is fixed, in-code policy data with
// no technology to swap (no database, no HTTP, no file I/O) — routing it
// through Infrastructure would add a layer with nothing to implement.
// IPermissionEvaluator still exists as its own interface so a test double
// can substitute a different mapping without touching the real catalog.
// Public, unlike Infrastructure's "internal, registered from within its own
// assembly" convention: this type is registered from Web's
// ApplicationEndpointRegistration (a different assembly), and Application
// has no DI extension method of its own to register it from internally.
public sealed class PermissionEvaluator : IPermissionEvaluator
{
    public bool HasPermission(Role role, Permission permission) =>
        RolePermissionCatalog.GetPermissions(role).Contains(permission);

    public IReadOnlyCollection<Permission> GetPermissions(Role role) =>
        RolePermissionCatalog.GetPermissions(role);
}

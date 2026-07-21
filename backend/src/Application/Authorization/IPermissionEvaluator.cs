using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Authorization;

// Permission evaluation must become the single source of truth for "does
// this role have this permission" (Launch Preparation, Priority 2, WP2) —
// every future authorization check (middleware, endpoint protection,
// feature-level checks) consumes this instead of re-implementing its own
// role comparison. Deliberately a pure, context-free function of (Role,
// Permission): it does not read ICurrentUserProvider itself, does not
// resolve relationship-scoped (fine-grained) authorization, and makes no
// HTTP/endpoint decision — those are WP3/WP4's concern, not this one's.
public interface IPermissionEvaluator
{
    bool HasPermission(Role role, Permission permission);

    IReadOnlyCollection<Permission> GetPermissions(Role role);
}

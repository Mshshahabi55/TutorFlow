using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Application.Authorization;

// The single conversion point between the role string Authentication
// already produces (LoginCommandHandler, AuthToken.Role,
// ICurrentUserProvider.Role — none modified here) and the typed Role enum
// this Permission Model uses. Centralized so no future authorization check
// re-implements its own string comparison ("No duplicated permission
// logic" — Launch Preparation, Priority 2, WP2).
public static class RoleExtensions
{
    public static Role? ToRole(this string? roleName) =>
        Enum.TryParse<Role>(roleName, ignoreCase: false, out var role) ? role : null;
}

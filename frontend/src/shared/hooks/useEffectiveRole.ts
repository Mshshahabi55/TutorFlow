import { useAuth } from "@/shared/hooks/useAuth";
import { useCurrentActor } from "@/shared/hooks/useCurrentActor";
import type { ActorRole } from "@/shared/context/ActorContext";

export function normalizeActorRole(role: string | undefined): ActorRole | null {
  if (!role) {
    return null;
  }

  if (role === "Student" || role === "Tutor" || role === "ParentGuardian" || role === "AdminStaff") {
    return role;
  }

  return null;
}

/**
 * The authenticated user's real role when signed in, falling back to the
 * locally-selected dev preview role (RoleSwitcher) otherwise. Extracted from
 * NavSidebar (Phase 4.9) so every role-shaped piece of UI — navigation, the
 * Tutor detail page's actions, the route guard below — reads the same
 * signal instead of each re-deriving it. Once a real session exists, the
 * dev preview role can never win (`authenticatedRole ?? actor.role` only
 * falls through to the preview when nothing real is signed in), which is
 * exactly what keeps RoleSwitcher's manual override inert for a signed-in
 * user (Phase 4.9 Task 2).
 */
export function useEffectiveRole(): ActorRole | null {
  const { actor } = useCurrentActor();
  const { user, isAuthenticated } = useAuth();
  const authenticatedRole = isAuthenticated ? normalizeActorRole(user?.role) : null;

  return authenticatedRole ?? actor.role;
}

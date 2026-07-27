import type { ReactNode } from "react";
import { useEffectiveRole } from "@/shared/hooks/useEffectiveRole";
import { ForbiddenState } from "@/shared/components/feedback/ForbiddenState";
import { formatRoleList } from "@/shared/constants/actorRoleLabels";
import type { ActorRole } from "@/shared/context/ActorContext";

export interface RequireRoleProps {
  roles: ActorRole[];
  children: ReactNode;
}

/**
 * Phase 4.9 Task 4: a real route guard, not just nav-item hiding — a role
 * this route's own backend permission does not grant gets ForbiddenState
 * instead of the route's privileged content, even reached by direct URL.
 * Uses the same `useEffectiveRole` signal (and the same per-route role
 * lists) NavSidebar already uses for display — no second, client-only
 * permission model. `roles` is deliberately the coarse-grained role list
 * from `RolePermissionCatalog`/`AUTHORIZATION_MATRIX.md`, never a resource-
 * instance check (e.g. "is this the Tutor's own offering") — that
 * fine-grained half is, and remains, the backend handler's job. This
 * component denies when the role is unknown (unauthenticated, no dev
 * preview role picked) rather than NavSidebar's own "show everything until
 * a role is known" convenience, since an actual access-control boundary
 * must default to deny, not permissive discovery.
 */
export function RequireRole({ roles, children }: RequireRoleProps) {
  const role = useEffectiveRole();

  if (role === null || !roles.includes(role)) {
    return <ForbiddenState requiredRoleLabel={formatRoleList(roles)} />;
  }

  return <>{children}</>;
}

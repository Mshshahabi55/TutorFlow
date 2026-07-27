import type { ActorRole } from "@/shared/context/ActorContext";

/** The one human-readable label per Domain Actor, shared by RoleSwitcher, AuthStatus, and RequireRole's ForbiddenState so no two places word a role differently. */
export const ACTOR_ROLE_LABEL: Record<ActorRole, string> = {
  Student: "Student",
  Tutor: "Tutor",
  ParentGuardian: "Parent/Guardian",
  AdminStaff: "Admin/Staff",
};

export function formatRoleList(roles: ActorRole[]): string {
  const labels = roles.map((role) => ACTOR_ROLE_LABEL[role]);
  if (labels.length === 1) {
    return labels[0];
  }
  return `${labels.slice(0, -1).join(", ")} or ${labels[labels.length - 1]}`;
}

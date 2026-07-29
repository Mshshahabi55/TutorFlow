import { useAuth } from "@/shared/hooks/useAuth";
import { normalizeActorRole } from "@/shared/hooks/useEffectiveRole";
import { useRememberedId } from "@/shared/hooks/useRememberedId";
import type { ActorRole } from "@/shared/context/ActorContext";

export type OwnIdKind = "tutor" | "student" | "parentGuardian";

const KIND_FOR_ROLE: Record<ActorRole, OwnIdKind | null> = {
  Tutor: "tutor",
  Student: "student",
  ParentGuardian: "parentGuardian",
  AdminStaff: null,
};

export interface UseOwnIdResult {
  /** The resolved id, or undefined if none is known yet. */
  id: string | undefined;
  /** True once `id` came from the real authenticated session — Account IS the Tutor/Student/Parent-Guardian record (same id, ADR-017), so nothing was "entered" and there is nothing to forget. */
  isFromSession: boolean;
  /** Signed in, but under a role that doesn't match `kind` — a genuine "can't resolve this view" case, distinct from "not signed in yet." In practice the router's own role guard should prevent reaching this, but it's handled rather than assumed unreachable. */
  isRoleMismatch: boolean;
  remember: (id: string) => void;
  forget: () => void;
}

const NOOP = () => {};

/**
 * The caller's own Tutor/Student/Parent-Guardian id. Resolved automatically
 * from the real authenticated session whenever possible — a Tutor/Student/
 * Parent-Guardian Account *is* the Tutor/Student/Parent-Guardian record
 * (class-table inheritance, same primary key: `docs/adr/ADR-017-authentication-mechanism-decision.md`,
 * `docs/database/DOMAIN_DATA_MODEL.md` Section 9) — so the id never needs to
 * be entered, remembered, or looked up for a real signed-in user. Falls back
 * to the existing device-remembered-id workaround only when no real session
 * exists at all (the dev-only "Acting as" preview, which has no real Account
 * to resolve against).
 */
export function useOwnId(kind: OwnIdKind): UseOwnIdResult {
  const { user, isAuthenticated } = useAuth();
  const remembered = useRememberedId(kind);

  if (isAuthenticated && user) {
    const role = normalizeActorRole(user.role);
    const resolvedKind = role ? KIND_FOR_ROLE[role] : null;

    if (resolvedKind === kind) {
      return {
        id: user.accountId,
        isFromSession: true,
        isRoleMismatch: false,
        remember: NOOP,
        forget: NOOP,
      };
    }

    return {
      id: undefined,
      isFromSession: false,
      isRoleMismatch: true,
      remember: remembered.remember,
      forget: remembered.forget,
    };
  }

  return {
    id: remembered.id,
    isFromSession: false,
    isRoleMismatch: false,
    remember: remembered.remember,
    forget: remembered.forget,
  };
}

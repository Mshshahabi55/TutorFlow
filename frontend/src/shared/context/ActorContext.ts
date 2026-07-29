import { createContext } from "react";

/**
 * The four Domain Actors (PROJECT_CONSTITUTION.md: Project Scope;
 * DOMAIN_MODEL.md: Domain Actors). Not a role/permission system — no
 * authorization decision is made from this value anywhere; it exists only
 * to let the UI render a role-appropriate navigation shell before a real
 * authentication mechanism exists.
 */
export type ActorRole = "Student" | "Tutor" | "ParentGuardian" | "AdminStaff";

export interface CurrentActor {
  /** The locally-selected "acting as" role, or null if none is selected. */
  role: ActorRole | null;
  /**
   * Always false. This is a dev-only role-preview mechanism, not a session —
   * a real authentication mechanism exists (`docs/adr/ADR-017-authentication-mechanism-decision.md`;
   * see `useAuth`/`AuthContext`), and this value must never be confused with it.
   */
  isAuthenticated: false;
}

export interface ActorContextValue {
  actor: CurrentActor;
  setRole: (role: ActorRole | null) => void;
}

export const ActorContext = createContext<ActorContextValue | undefined>(undefined);

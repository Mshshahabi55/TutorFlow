import { createContext } from "react";

/**
 * The authenticated session established by POST /auth/login
 * (docs/adr/ADR-017-authentication-mechanism-decision.md). `email` is not
 * part of the backend's `LoginResultDto` (no name/email field is returned
 * by login — ADR-017's own Non-Goals) — it is captured client-side from
 * the login form's own input at the moment of a successful attempt, purely
 * for a friendlier header display than a raw Account id. No backend change,
 * no new field invented server-side.
 */
export interface AuthenticatedUser {
  token: string;
  accountId: string;
  role: string;
  expiresAtUtc: string;
  email: string;
}

export interface AuthContextValue {
  user: AuthenticatedUser | null;
  isAuthenticated: boolean;
  setUser: (user: AuthenticatedUser | null) => void;
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);

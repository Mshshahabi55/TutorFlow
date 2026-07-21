import { useMemo, useState, type ReactNode } from "react";
import { AuthContext, type AuthContextValue, type AuthenticatedUser } from "@/shared/context/AuthContext";
import { setAuthToken, clearAuthToken } from "@/services/api/apiClient";

/**
 * Holds the authenticated session in memory only — deliberately never
 * localStorage or sessionStorage (docs/adr/ADR-017-authentication-mechanism-decision.md).
 * A bearer token readable by JavaScript is inherently exposed to XSS if
 * persisted across page loads; keeping it in memory limits that exposure to
 * the current page load. Accepted, disclosed trade-off for this release: a
 * hard page reload signs the user out and requires logging in again — the
 * same trade-off a Security Hardening pass may revisit, not a silent gap.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUserState] = useState<AuthenticatedUser | null>(null);

  const setUser = (nextUser: AuthenticatedUser | null) => {
    setUserState(nextUser);
    if (nextUser) {
      setAuthToken(nextUser.token);
    } else {
      clearAuthToken();
    }
  };

  const value = useMemo<AuthContextValue>(
    () => ({ user, isAuthenticated: user !== null, setUser }),
    [user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

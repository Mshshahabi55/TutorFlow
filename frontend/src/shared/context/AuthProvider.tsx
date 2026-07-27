import { useMemo, useState, type ReactNode } from "react";
import { AuthContext, type AuthContextValue, type AuthenticatedUser } from "@/shared/context/AuthContext";
import { setAuthToken, clearAuthToken } from "@/services/api/apiClient";

const SESSION_STORAGE_KEY = "tutorflow.authSession";

function isAuthenticatedUser(value: unknown): value is AuthenticatedUser {
  return (
    typeof value === "object" &&
    value !== null &&
    typeof (value as AuthenticatedUser).token === "string" &&
    typeof (value as AuthenticatedUser).accountId === "string" &&
    typeof (value as AuthenticatedUser).role === "string" &&
    typeof (value as AuthenticatedUser).expiresAtUtc === "string"
  );
}

function isExpired(user: AuthenticatedUser): boolean {
  return Number.isNaN(Date.parse(user.expiresAtUtc)) || Date.parse(user.expiresAtUtc) <= Date.now();
}

function readStoredSession(): AuthenticatedUser | null {
  if (typeof window === "undefined") {
    return null;
  }

  const raw = window.sessionStorage.getItem(SESSION_STORAGE_KEY);
  if (!raw) {
    return null;
  }

  let parsed: unknown;
  try {
    parsed = JSON.parse(raw);
  } catch {
    window.sessionStorage.removeItem(SESSION_STORAGE_KEY);
    return null;
  }

  if (!isAuthenticatedUser(parsed) || isExpired(parsed)) {
    window.sessionStorage.removeItem(SESSION_STORAGE_KEY);
    return null;
  }

  return parsed;
}

/**
 * Phase 4.9: holds the authenticated session in `sessionStorage`, not
 * `localStorage` — `docs/adr/ADR-017-authentication-mechanism-decision.md`
 * still rules out `localStorage` for this bearer token (an indefinitely-
 * persisted, JS-readable token is a materially larger XSS-exposure window
 * than a tab-scoped one). This supersedes this file's own prior in-memory-
 * only policy: that policy's own disclosed trade-off ("a hard reload signs
 * the user out") is exactly the live-browser finding this phase's brief
 * reported as broken (finding #1) — the brief explicitly directs persisting
 * across a reload while keeping `localStorage` off the table, so
 * `sessionStorage` is the minimal change that satisfies both. It is cleared
 * when the tab/window closes, never shared across tabs or browser restarts,
 * and is re-validated against the token's own `expiresAtUtc` on every read —
 * an already-expired stored session is discarded, never silently restored.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUserState] = useState<AuthenticatedUser | null>(() => {
    const stored = readStoredSession();
    if (stored) {
      setAuthToken(stored.token);
    }
    return stored;
  });

  const setUser = (nextUser: AuthenticatedUser | null) => {
    setUserState(nextUser);
    if (nextUser) {
      setAuthToken(nextUser.token);
    } else {
      clearAuthToken();
    }

    if (typeof window !== "undefined") {
      if (nextUser) {
        window.sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(nextUser));
      } else {
        window.sessionStorage.removeItem(SESSION_STORAGE_KEY);
      }
    }
  };

  const value = useMemo<AuthContextValue>(
    () => ({ user, isAuthenticated: user !== null, setUser }),
    [user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

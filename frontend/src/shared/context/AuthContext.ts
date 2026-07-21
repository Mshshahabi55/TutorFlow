import { createContext } from "react";

/** The authenticated session established by POST /auth/login (docs/adr/ADR-017-authentication-mechanism-decision.md). */
export interface AuthenticatedUser {
  token: string;
  accountId: string;
  role: string;
  expiresAtUtc: string;
}

export interface AuthContextValue {
  user: AuthenticatedUser | null;
  isAuthenticated: boolean;
  setUser: (user: AuthenticatedUser | null) => void;
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);

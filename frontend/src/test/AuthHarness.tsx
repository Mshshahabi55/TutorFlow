import { useEffect } from "react";
import type { AuthenticatedUser } from "@/shared/context/AuthContext";
import { useAuth } from "@/shared/hooks/useAuth";

/** Simulates a real signed-in session inside a test's AuthProvider — the same technique NavSidebar.test.tsx used first. */
export function AuthHarness({ user }: { user: AuthenticatedUser }) {
  const { setUser } = useAuth();

  useEffect(() => {
    setUser(user);
  }, [setUser, user]);

  return null;
}

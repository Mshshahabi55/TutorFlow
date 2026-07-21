import { useMemo, useState, type ReactNode } from "react";
import { ActorContext, type ActorContextValue, type ActorRole } from "@/shared/context/ActorContext";

const STORAGE_KEY = "tutorflow.devActorRole";

function readStoredRole(): ActorRole | null {
  if (typeof window === "undefined") {
    return null;
  }

  const stored = window.localStorage.getItem(STORAGE_KEY);
  return stored === "Student" ||
    stored === "Tutor" ||
    stored === "ParentGuardian" ||
    stored === "AdminStaff"
    ? stored
    : null;
}

export function ActorProvider({ children }: { children: ReactNode }) {
  const [role, setRoleState] = useState<ActorRole | null>(readStoredRole);

  const setRole = (nextRole: ActorRole | null) => {
    setRoleState(nextRole);
    if (typeof window !== "undefined") {
      if (nextRole) {
        window.localStorage.setItem(STORAGE_KEY, nextRole);
      } else {
        window.localStorage.removeItem(STORAGE_KEY);
      }
    }
  };

  const value = useMemo<ActorContextValue>(
    () => ({ actor: { role, isAuthenticated: false }, setRole }),
    [role],
  );

  return <ActorContext.Provider value={value}>{children}</ActorContext.Provider>;
}

import { useCallback, useState } from "react";

/**
 * RC2: there is still no "my own id" resolution from an authenticated
 * Account (ADR-011 remains frozen) — no backend change is in scope to add
 * one. This is the frontend-only workaround: the first time a user types
 * their own Tutor/Student/Parent-Guardian id into any id-gate (a
 * Dashboard's lookup prompt, a booking wizard step), it's remembered
 * locally, the same `localStorage`-backed pattern already used for
 * `tutorflow.colorMode`/`tutorflow.sidebarCollapsed` — so every other
 * screen that needs that id (nav links, "My Lessons", the booking wizard)
 * can skip asking for it again. Purely a client-side convenience; it
 * grants no capability the id-lookup form didn't already have, and it's
 * cleared by `forget()` if the user ever needs to switch identity (e.g.
 * the dev RoleSwitcher changing role).
 */
export function useRememberedId(kind: "tutor" | "student" | "parentGuardian") {
  const storageKey = `tutorflow.rememberedId.${kind}`;
  const [id, setIdState] = useState<string | undefined>(() => {
    if (typeof window === "undefined") {
      return undefined;
    }
    return window.localStorage.getItem(storageKey) ?? undefined;
  });

  const remember = useCallback(
    (newId: string) => {
      setIdState(newId);
      if (typeof window !== "undefined") {
        window.localStorage.setItem(storageKey, newId);
      }
    },
    [storageKey],
  );

  const forget = useCallback(() => {
    setIdState(undefined);
    if (typeof window !== "undefined") {
      window.localStorage.removeItem(storageKey);
    }
  }, [storageKey]);

  return { id, remember, forget };
}

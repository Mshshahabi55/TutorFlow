import { useCallback, useState } from "react";

/**
 * RC4.3: a real signed-in Account resolves its own Tutor/Student/Parent-
 * Guardian id automatically now (`useOwnId`, `docs/adr/ADR-017-authentication-mechanism-decision.md`)
 * — this hook itself is unchanged and still backs that resolution, but its
 * remaining direct purpose is the dev-only "Acting as" preview (no real
 * Account to resolve against): the first time that preview types an id
 * into any id-gate, it's remembered locally, the same `localStorage`-backed
 * pattern already used for `tutorflow.colorMode`/`tutorflow.sidebarCollapsed`.
 * Purely a client-side convenience; it grants no capability the id-lookup
 * form didn't already have, and it's cleared by `forget()` if the preview
 * ever needs to switch identity.
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

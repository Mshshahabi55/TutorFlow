import { useContext } from "react";
import { ActorContext, type ActorContextValue } from "@/shared/context/ActorContext";

/**
 * The dev-only "Acting as" preview role — real authentication exists now
 * (`docs/adr/ADR-017-authentication-mechanism-decision.md`; see `useAuth`),
 * so this is deliberately not it. Returns the locally-selected preview role
 * and an isAuthenticated flag that is always false — never a real identity.
 * No component may treat this as a signed-in session; `useEffectiveRole`
 * is the seam that lets a real session always win over this preview.
 */
export function useCurrentActor(): ActorContextValue {
  const context = useContext(ActorContext);
  if (!context) {
    throw new Error("useCurrentActor must be used within an ActorProvider.");
  }

  return context;
}

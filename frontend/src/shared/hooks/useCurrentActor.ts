import { useContext } from "react";
import { ActorContext, type ActorContextValue } from "@/shared/context/ActorContext";

/**
 * Authentication placeholder (ADR-011 frozen). Returns the locally-selected
 * "acting as" role and an isAuthenticated flag that is always false — never
 * a real identity. No component may treat this as a signed-in session.
 */
export function useCurrentActor(): ActorContextValue {
  const context = useContext(ActorContext);
  if (!context) {
    throw new Error("useCurrentActor must be used within an ActorProvider.");
  }

  return context;
}

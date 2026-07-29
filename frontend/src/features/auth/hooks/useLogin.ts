import { useMutation, useQueryClient } from "@tanstack/react-query";
import { login } from "@/features/auth/api/authService";
import { useAuth } from "@/shared/hooks/useAuth";
import { useCurrentActor } from "@/shared/hooks/useCurrentActor";
import type { ActorRole } from "@/shared/context/ActorContext";

const KNOWN_ROLES: ActorRole[] = ["Student", "Tutor", "ParentGuardian", "AdminStaff"];

function isKnownRole(role: string): role is ActorRole {
  return (KNOWN_ROLES as string[]).includes(role);
}

export function useLogin() {
  const { setUser } = useAuth();
  const { setRole } = useCurrentActor();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ email, password }: { email: string; password: string }) => login(email, password),
    onSuccess: (result, variables) => {
      // RC4.4: the QueryClient is one long-lived singleton
      // (app/queryClient.ts) shared across every auth state, and every
      // "current user" query (Admin lists, GET /conversations/mine, GET
      // /notifications/mine, ...) shares its cache key across accounts and
      // roles. Browsing role-gated pages via the dev-only "Acting as"
      // preview (no real token) — or simply loading the app before signing
      // in — legitimately 401s those queries, and React Query caches that
      // failure under the same key a real login later reuses; nothing was
      // ever invalidating it, so the cached 401 kept being shown after a
      // genuine, successful login. Clearing here guarantees every query a
      // newly authenticated session touches is fetched fresh, with this
      // session's own token attached, never a stale result cached under a
      // different (or absent) identity.
      queryClient.clear();
      // `email` isn't part of the backend's LoginResultDto — captured here
      // from the form's own input, not invented, purely so the header has
      // something friendlier than a raw Account id to display.
      setUser({ ...result, email: variables.email });
      // Bridges the real, authenticated role into the existing "Acting as
      // (dev only)" nav-preview selector so it reflects reality by default —
      // the manual override remains available for previewing other roles'
      // navigation, which RoleSwitcher's own documented purpose already covers.
      if (isKnownRole(result.role)) {
        setRole(result.role);
      }
    },
  });
}

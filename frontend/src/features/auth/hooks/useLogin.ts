import { useMutation } from "@tanstack/react-query";
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

  return useMutation({
    mutationFn: ({ email, password }: { email: string; password: string }) => login(email, password),
    onSuccess: (result) => {
      setUser(result);
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

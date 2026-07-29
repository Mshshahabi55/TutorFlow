import { useMutation, useQueryClient } from "@tanstack/react-query";
import { logout } from "@/features/auth/api/authService";
import { useAuth } from "@/shared/hooks/useAuth";

export function useLogout() {
  const { user, setUser } = useAuth();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => (user ? logout(user.token) : Promise.resolve()),
    // Clears the local session even if the API call fails (e.g. the token
    // was already expired/revoked) — logout is idempotent from the caller's
    // perspective, mirroring the backend's own LogoutCommandHandler.
    // RC4.4: also clears the QueryClient cache — see useLogin's own comment
    // for why a stale, previously-authenticated result must never survive
    // into whatever comes next (signed out, or a different account signing
    // in on the same device).
    onSettled: () => {
      setUser(null);
      queryClient.clear();
    },
  });
}

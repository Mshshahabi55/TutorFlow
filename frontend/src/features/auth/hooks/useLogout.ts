import { useMutation } from "@tanstack/react-query";
import { logout } from "@/features/auth/api/authService";
import { useAuth } from "@/shared/hooks/useAuth";

export function useLogout() {
  const { user, setUser } = useAuth();

  return useMutation({
    mutationFn: () => (user ? logout(user.token) : Promise.resolve()),
    // Clears the local session even if the API call fails (e.g. the token
    // was already expired/revoked) — logout is idempotent from the caller's
    // perspective, mirroring the backend's own LogoutCommandHandler.
    onSettled: () => setUser(null),
  });
}

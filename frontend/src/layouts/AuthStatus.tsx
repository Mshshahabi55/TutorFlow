import { Link as RouterLink } from "react-router-dom";
import { Button, Stack, Typography } from "@mui/material";
import { useAuth } from "@/shared/hooks/useAuth";
import { useLogout } from "@/features/auth/hooks/useLogout";
import { useNotification } from "@/shared/hooks/useNotification";
import { paths } from "@/routes/paths";

/** The real, authenticated session (docs/adr/ADR-017-authentication-mechanism-decision.md) — distinct from RoleSwitcher's dev-only nav preview. */
export function AuthStatus() {
  const { user, isAuthenticated } = useAuth();
  const logout = useLogout();
  const { notify } = useNotification();

  if (!isAuthenticated || !user) {
    return (
      <Button component={RouterLink} to={paths.auth.login} size="small" variant="outlined">
        Sign in
      </Button>
    );
  }

  function handleLogout() {
    logout.mutate(undefined, {
      onSuccess: () => notify({ message: "Signed out.", severity: "info" }),
    });
  }

  return (
    <Stack direction="row" spacing={1} alignItems="center">
      <Typography variant="body2" color="text.secondary" noWrap>
        {user.role}
      </Typography>
      <Button size="small" onClick={handleLogout} disabled={logout.isPending}>
        {logout.isPending ? "Signing out…" : "Sign out"}
      </Button>
    </Stack>
  );
}

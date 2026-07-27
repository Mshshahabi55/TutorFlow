import { Link as RouterLink } from "react-router-dom";
import { Box, Button, Stack, Typography } from "@mui/material";
import { useAuth } from "@/shared/hooks/useAuth";
import { useLogout } from "@/features/auth/hooks/useLogout";
import { useNotification } from "@/shared/hooks/useNotification";
import { normalizeActorRole } from "@/shared/hooks/useEffectiveRole";
import { ACTOR_ROLE_LABEL } from "@/shared/constants/actorRoleLabels";
import { monoFontFamily } from "@/app/theme";
import { paths } from "@/routes/paths";

/**
 * The real, authenticated session
 * (docs/adr/ADR-017-authentication-mechanism-decision.md) — distinct from
 * RoleSwitcher's dev-only nav preview. Phase 4.9 Task 3: shows the signed-in
 * identity, not just a role label — `LoginResultDto`/`AuthenticatedUser`
 * carries no name or email (ADR-017 leaves personal-data fields beyond
 * login email as its own open question), so the account id is the only
 * other identifying value available; showing a fabricated display name
 * would invent data the backend never returns. The id is hidden below the
 * `sm` breakpoint, where AppBar width is already tight (Phase D2 Task 5).
 */
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

  const roleLabel = ACTOR_ROLE_LABEL[normalizeActorRole(user.role) ?? "Student"];

  return (
    <Stack direction="row" spacing={1} alignItems="center">
      <Box sx={{ lineHeight: 1.1 }}>
        <Typography variant="body2" fontWeight={600} noWrap>
          {roleLabel}
        </Typography>
        <Typography
          variant="caption"
          color="text.secondary"
          fontFamily={monoFontFamily}
          title={user.accountId}
          noWrap
          sx={{ display: { xs: "none", sm: "block" }, maxWidth: 160 }}
        >
          {user.accountId}
        </Typography>
      </Box>
      <Button size="small" onClick={handleLogout} disabled={logout.isPending}>
        {logout.isPending ? "Signing out…" : "Sign out"}
      </Button>
    </Stack>
  );
}

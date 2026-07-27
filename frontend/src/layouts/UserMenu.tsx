import { useState } from "react";
import { Avatar, IconButton, ListItemIcon, Menu, MenuItem, Typography } from "@mui/material";
import LogoutRoundedIcon from "@mui/icons-material/LogoutRounded";
import { useAuth } from "@/shared/hooks/useAuth";
import { useLogout } from "@/features/auth/hooks/useLogout";
import { useNotification } from "@/shared/hooks/useNotification";
import { normalizeActorRole } from "@/shared/hooks/useEffectiveRole";
import { ACTOR_ROLE_LABEL } from "@/shared/constants/actorRoleLabels";
import { monoFontFamily } from "@/app/theme";
import type { ActorRole } from "@/shared/context/ActorContext";

const ROLE_INITIALS: Record<ActorRole, string> = {
  Student: "St",
  Tutor: "Tu",
  ParentGuardian: "PG",
  AdminStaff: "Ad",
};

const ROLE_AVATAR_PALETTE_KEY: Record<ActorRole, "primary" | "success" | "info" | "warning"> = {
  Student: "primary",
  Tutor: "success",
  ParentGuardian: "info",
  AdminStaff: "warning",
};

/**
 * Phase D4: retires `AuthStatus`'s signed-in branch into a proper Avatar +
 * dropdown menu. `LoginResultDto`/`AuthenticatedUser` carries no name or
 * email (ADR-017 leaves personal-data fields beyond login email as its
 * own open question, Phase 4.9's own finding) — the avatar's initials are
 * derived from the already-known role, not a fabricated name or photo.
 * `AppHeader` renders this only when authenticated; the signed-out "Sign
 * in" button stays inline there (too small to warrant its own file).
 */
export function UserMenu() {
  const { user } = useAuth();
  const logout = useLogout();
  const { notify } = useNotification();
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);

  if (!user) {
    return null;
  }

  const role = normalizeActorRole(user.role) ?? "Student";
  const roleLabel = ACTOR_ROLE_LABEL[role];

  function handleClose() {
    setAnchorEl(null);
  }

  function handleLogout() {
    handleClose();
    logout.mutate(undefined, {
      onSuccess: () => notify({ message: "Signed out.", severity: "info" }),
    });
  }

  return (
    <>
      <IconButton
        aria-label={`Account menu (${roleLabel})`}
        size="small"
        onClick={(event) => setAnchorEl(event.currentTarget)}
      >
        <Avatar
          sx={{
            width: 32,
            height: 32,
            fontSize: "0.8125rem",
            bgcolor: `${ROLE_AVATAR_PALETTE_KEY[role]}.main`,
            color: `${ROLE_AVATAR_PALETTE_KEY[role]}.contrastText`,
          }}
        >
          {ROLE_INITIALS[role]}
        </Avatar>
      </IconButton>
      <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={handleClose}>
        <MenuItem disabled divider sx={{ opacity: "1 !important", flexDirection: "column", alignItems: "flex-start" }}>
          <Typography variant="body2" fontWeight={600}>
            {roleLabel}
          </Typography>
          <Typography variant="caption" color="text.secondary" fontFamily={monoFontFamily}>
            {user.accountId}
          </Typography>
        </MenuItem>
        <MenuItem onClick={handleLogout} disabled={logout.isPending}>
          <ListItemIcon>
            <LogoutRoundedIcon fontSize="small" />
          </ListItemIcon>
          {logout.isPending ? "Signing out…" : "Sign out"}
        </MenuItem>
      </Menu>
    </>
  );
}

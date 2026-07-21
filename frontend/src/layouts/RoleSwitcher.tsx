import { IconButton, MenuItem, Stack, TextField, Tooltip } from "@mui/material";
import ClearRoundedIcon from "@mui/icons-material/ClearRounded";
import { useCurrentActor } from "@/shared/hooks/useCurrentActor";
import { useNotification } from "@/shared/hooks/useNotification";
import { useConfirmDialog } from "@/shared/hooks/useConfirmDialog";
import type { ActorRole } from "@/shared/context/ActorContext";

const ROLE_OPTIONS: { value: ActorRole; label: string }[] = [
  { value: "Student", label: "Student" },
  { value: "Tutor", label: "Tutor" },
  { value: "ParentGuardian", label: "Parent/Guardian" },
  { value: "AdminStaff", label: "Admin/Staff" },
];

const ROLE_LABEL: Record<ActorRole, string> = {
  Student: "Student",
  Tutor: "Tutor",
  ParentGuardian: "Parent/Guardian",
  AdminStaff: "Admin/Staff",
};

/**
 * Development aid only — lets the UI be built and reviewed per role before
 * a real authentication mechanism exists (ADR-011). Never treat a selection
 * here as authorization: no permission check anywhere reads this value.
 *
 * Also the Application Shell's real integration point for the Notification
 * and Confirm Dialog infrastructure: changing role notifies, and clearing
 * a selected role asks for confirmation first, since silently discarding it
 * would lose whichever role-specific screen the user was previewing.
 */
export function RoleSwitcher() {
  const { actor, setRole } = useCurrentActor();
  const { notify } = useNotification();
  const { confirm } = useConfirmDialog();

  function applyRole(nextRole: ActorRole | null) {
    setRole(nextRole);
    notify({
      message: nextRole
        ? `Now acting as ${ROLE_LABEL[nextRole]} (dev only — grants no real access).`
        : "Role selection cleared.",
      severity: "info",
    });
  }

  async function handleClear() {
    const confirmed = await confirm({
      title: "Clear the selected role?",
      description: "You'll need to choose a role again to preview role-specific screens.",
      confirmLabel: "Clear role",
    });
    if (confirmed) {
      applyRole(null);
    }
  }

  return (
    <Stack direction="row" spacing={1} alignItems="center">
      <TextField
        select
        size="small"
        label="Acting as (dev only)"
        value={actor.role ?? ""}
        onChange={(event) => applyRole((event.target.value || null) as ActorRole | null)}
        sx={{ minWidth: { xs: 140, sm: 200 }, backgroundColor: "background.paper" }}
      >
        <MenuItem value="">
          <em>None selected</em>
        </MenuItem>
        {ROLE_OPTIONS.map((option) => (
          <MenuItem key={option.value} value={option.value}>
            {option.label}
          </MenuItem>
        ))}
      </TextField>
      {actor.role ? (
        <Tooltip title="Clear role selection">
          <IconButton
            size="small"
            aria-label="Clear role selection"
            onClick={() => void handleClear()}
          >
            <ClearRoundedIcon fontSize="small" />
          </IconButton>
        </Tooltip>
      ) : null}
    </Stack>
  );
}

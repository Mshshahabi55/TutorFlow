import { Link as RouterLink } from "react-router-dom";
import { Alert, AlertTitle, Box, Button } from "@mui/material";
import { paths } from "@/routes/paths";

export interface ForbiddenStateProps {
  /** Plain-language description of who this page is for, e.g. "Admin/Staff". */
  requiredRoleLabel: string;
}

/**
 * Shown by RequireRole (Phase 4.9 Task 4) in place of a privileged route's
 * real content, for a role that route's own backend permission does not
 * grant. This is UX only — the same route hit directly against the API
 * without going through this component would still get the backend's own
 * 403, which remains the real authorization boundary.
 */
export function ForbiddenState({ requiredRoleLabel }: ForbiddenStateProps) {
  return (
    <Box py={2}>
      <Alert
        severity="warning"
        action={
          <Button component={RouterLink} to={paths.home} color="inherit" size="small">
            Go to Dashboard
          </Button>
        }
      >
        <AlertTitle>Not authorized</AlertTitle>
        This page is only available to {requiredRoleLabel}.
      </Alert>
    </Box>
  );
}

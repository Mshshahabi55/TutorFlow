import { Link as RouterLink } from "react-router-dom";
import { Box, Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { fetchHealthStatus } from "@/services/api/healthService";
import { LoadingState } from "@/shared/components/feedback/LoadingState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { PageHeader } from "@/shared/components/PageHeader";
import { useCurrentActor } from "@/shared/hooks/useCurrentActor";
import type { ActorRole, CurrentActor } from "@/shared/context/ActorContext";
import { StudentDashboard } from "@/routes/dashboard/StudentDashboard";
import { TutorDashboard } from "@/routes/dashboard/TutorDashboard";
import { ParentDashboard } from "@/routes/dashboard/ParentDashboard";
import { ROLE_QUICK_ACTIONS } from "@/routes/dashboardRoleConfig";

const ROLE_SUMMARY: Record<ActorRole, string> = {
  Student:
    "Search for a Tutor by subject, language, location, or availability, then book a session. Use the navigation to get started.",
  Tutor:
    "Set your hourly rate, subjects, and availability, and manage your booked sessions from the navigation.",
  ParentGuardian:
    "Link to a Student by inviting a Relationship, then book and manage sessions on their behalf from the navigation.",
  AdminStaff:
    "Review pending Tutor applications and oversee every session on the platform from the navigation. Administrative conflict resolution is not yet available.",
};

/**
 * The application's landing page. Student, Tutor, and ParentGuardian each
 * get their own workspace-style home (`StudentDashboard` Phase 3 Step 1,
 * `TutorDashboard` Phase 3 Step 6, `ParentDashboard` Phase 3 Step 7) —
 * AdminStaff keeps the original generic dashboard (`GenericDashboard`)
 * unchanged, since no phase's scope covers the Admin workspace.
 */
export function DashboardPage() {
  const { actor } = useCurrentActor();

  if (actor.role === "Student") {
    return <StudentDashboard />;
  }

  if (actor.role === "Tutor") {
    return <TutorDashboard />;
  }

  if (actor.role === "ParentGuardian") {
    return <ParentDashboard />;
  }

  return <GenericDashboard actor={actor} />;
}

/**
 * Exercises the real GET /health capability end-to-end (QueryClient, Axios
 * client, loading/error states) and, once a role is known, surfaces that
 * role's most relevant next steps as direct links — every destination is
 * already live, reachable from the navigation too.
 */
function GenericDashboard({ actor }: { actor: CurrentActor }) {
  const healthQuery = useQuery({
    queryKey: ["health"],
    queryFn: fetchHealthStatus,
  });

  return (
    <Stack spacing={3}>
      <PageHeader
        title="Dashboard"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            {actor.role
              ? ROLE_SUMMARY[actor.role]
              : "Select a role from Acting as (dev only) above to preview what this dashboard shows for that role."}
          </Typography>
        }
      />

      {actor.role ? (
        <Box>
          <Typography variant="subtitle1" fontWeight={600} gutterBottom>
            Quick actions
          </Typography>
          <Stack direction="row" flexWrap="wrap" gap={1.5}>
            {ROLE_QUICK_ACTIONS[actor.role].map((action) => (
              <Button
                key={action.to}
                component={RouterLink}
                to={action.to}
                variant="outlined"
                startIcon={action.icon}
              >
                {action.label}
              </Button>
            ))}
          </Stack>
        </Box>
      ) : null}

      <Card variant="outlined" sx={{ maxWidth: 480 }}>
        <CardContent>
          <Typography variant="subtitle1" fontWeight={600} gutterBottom>
            Backend connectivity
          </Typography>

          {healthQuery.isPending ? <LoadingState label="Checking backend health…" /> : null}
          {healthQuery.isError ? (
            <ErrorState error={healthQuery.error} onRetry={() => void healthQuery.refetch()} />
          ) : null}
          {healthQuery.isSuccess ? (
            <Stack direction="row" alignItems="center" spacing={1.5}>
              <StatusPill
                label={healthQuery.data}
                tone={healthQuery.data === "Healthy" ? "success" : "critical"}
              />
              <Typography variant="body2" color="text.secondary">
                GET /health
              </Typography>
            </Stack>
          ) : null}
        </CardContent>
      </Card>
    </Stack>
  );
}

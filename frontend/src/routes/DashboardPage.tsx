import { Card, CardContent, Stack, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { fetchHealthStatus } from "@/services/api/healthService";
import { LoadingState } from "@/shared/components/feedback/LoadingState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { PageHeader } from "@/shared/components/PageHeader";
import { useCurrentActor } from "@/shared/hooks/useCurrentActor";
import type { ActorRole } from "@/shared/context/ActorContext";

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

const AVAILABLE_MODULES = [
  "Identity & Relationship",
  "Scheduling & Booking",
  "Tutor Discovery",
  "Administration & Oversight",
];

/**
 * The application's landing page. Exercises the real GET /health capability
 * end-to-end (QueryClient, Axios client, loading/error states) and surfaces,
 * as static text keyed to the locally-selected dev role, what each role can
 * already do — every module listed here is live; see the navigation.
 */
export function DashboardPage() {
  const { actor } = useCurrentActor();
  const healthQuery = useQuery({
    queryKey: ["health"],
    queryFn: fetchHealthStatus,
  });

  return (
    <Stack spacing={3} maxWidth={720}>
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

      <Card variant="outlined">
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

      <Card variant="outlined">
        <CardContent>
          <Typography variant="subtitle1" fontWeight={600} gutterBottom>
            Available modules
          </Typography>
          <Typography variant="body2" color="text.secondary" gutterBottom>
            Every module below is live — open it from the navigation.
          </Typography>
          <Stack direction="row" flexWrap="wrap" gap={1}>
            {AVAILABLE_MODULES.map((module) => (
              <StatusPill key={module} label={module} tone="success" />
            ))}
          </Stack>
        </CardContent>
      </Card>
    </Stack>
  );
}

import type { ReactNode } from "react";
import { Link as RouterLink } from "react-router-dom";
import { Box, Button, Card, CardContent, Stack, Typography } from "@mui/material";
import SearchRoundedIcon from "@mui/icons-material/SearchRounded";
import EventRoundedIcon from "@mui/icons-material/EventRounded";
import CalendarMonthRoundedIcon from "@mui/icons-material/CalendarMonthRounded";
import EventAvailableRoundedIcon from "@mui/icons-material/EventAvailableRounded";
import LinkRoundedIcon from "@mui/icons-material/LinkRounded";
import FactCheckRoundedIcon from "@mui/icons-material/FactCheckRounded";
import AdminPanelSettingsRoundedIcon from "@mui/icons-material/AdminPanelSettingsRounded";
import ListAltRoundedIcon from "@mui/icons-material/ListAltRounded";
import { useQuery } from "@tanstack/react-query";
import { fetchHealthStatus } from "@/services/api/healthService";
import { LoadingState } from "@/shared/components/feedback/LoadingState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { PageHeader } from "@/shared/components/PageHeader";
import { useCurrentActor } from "@/shared/hooks/useCurrentActor";
import type { ActorRole } from "@/shared/context/ActorContext";
import { paths } from "@/routes/paths";

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

interface QuickAction {
  label: string;
  to: string;
  icon: ReactNode;
}

/**
 * Phase D2 Task 4: replaces the old static "Available modules" list — four
 * inert, non-interactive pills naming every bounded context, with no link
 * and no distinction between them (the Task 1 audit's clutter finding).
 * Each role instead gets its own short, actually-navigable set of next
 * steps, reusing the exact routes NavSidebar already exposes for that role
 * — no new page, no new capability, just the same destinations surfaced
 * one click sooner from the page a signed-in user lands on first.
 */
const ROLE_QUICK_ACTIONS: Record<ActorRole, QuickAction[]> = {
  Student: [
    { label: "Search Tutors", to: paths.discovery.tutorSearch, icon: <SearchRoundedIcon /> },
    { label: "Book a session", to: paths.scheduling.bookSession, icon: <EventRoundedIcon /> },
    {
      label: "My sessions",
      to: paths.scheduling.studentScheduleBase,
      icon: <CalendarMonthRoundedIcon />,
    },
  ],
  Tutor: [
    {
      label: "Declare availability",
      to: paths.scheduling.declareAvailability,
      icon: <EventAvailableRoundedIcon />,
    },
    {
      label: "My sessions",
      to: paths.scheduling.tutorScheduleBase,
      icon: <CalendarMonthRoundedIcon />,
    },
  ],
  ParentGuardian: [
    { label: "Relationships", to: paths.identity.relationships, icon: <LinkRoundedIcon /> },
    { label: "Book a session", to: paths.scheduling.bookSession, icon: <EventRoundedIcon /> },
    {
      label: "Student sessions",
      to: paths.scheduling.studentScheduleBase,
      icon: <CalendarMonthRoundedIcon />,
    },
  ],
  AdminStaff: [
    {
      label: "Pending Tutor approvals",
      to: paths.identity.tutorPending,
      icon: <FactCheckRoundedIcon />,
    },
    {
      label: "Admin dashboard",
      to: paths.oversight.adminDashboard,
      icon: <AdminPanelSettingsRoundedIcon />,
    },
    { label: "All sessions", to: paths.oversight.globalSessions, icon: <ListAltRoundedIcon /> },
  ],
};

/**
 * The application's landing page. Exercises the real GET /health capability
 * end-to-end (QueryClient, Axios client, loading/error states) and, once a
 * role is known, surfaces that role's most relevant next steps as direct
 * links — every destination is already live, reachable from the navigation
 * too.
 */
export function DashboardPage() {
  const { actor } = useCurrentActor();
  const healthQuery = useQuery({
    queryKey: ["health"],
    queryFn: fetchHealthStatus,
  });

  return (
    <Stack spacing={4}>
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

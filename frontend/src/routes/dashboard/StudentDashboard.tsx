import { Link as RouterLink } from "react-router-dom";
import { Box, Button, Stack, Typography } from "@mui/material";
import SearchRoundedIcon from "@mui/icons-material/SearchRounded";
import EventRoundedIcon from "@mui/icons-material/EventRounded";
import { PageHeader } from "@/shared/components/PageHeader";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { SectionCard } from "@/shared/components/SectionCard";
import { RecommendedTutors } from "@/routes/dashboard/RecommendedTutors";
import { ROLE_QUICK_ACTIONS } from "@/routes/dashboardRoleConfig";
import { paths } from "@/routes/paths";

/**
 * The Student's landing experience — a marketplace home instead of the
 * generic role-summary dashboard every other role still sees (unchanged in
 * `DashboardPage`). No new data source is introduced: Recommended Tutors
 * reuses Discovery's existing search capability; Upcoming Sessions and
 * Continue Learning have no capability to read yet (there is no "my own
 * Student id" resolved from an authenticated Account anywhere in this app
 * today — `StudentSessionListPage` itself still requires a manually entered
 * Student id), so they render an honest empty state rather than inventing
 * one.
 */
export function StudentDashboard() {
  return (
    <Stack spacing={4}>
      <PageHeader
        title="Welcome back"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Find your next tutor, pick up where you left off, or check what&rsquo;s coming up.
          </Typography>
        }
      />

      <Stack direction={{ xs: "column", md: "row" }} spacing={3} alignItems="stretch">
        <Box flex={1}>
          <SectionCard title="Upcoming Sessions">
            <EmptyState
              title="No upcoming sessions yet"
              description="Once you book a session with a tutor, it will appear here."
              action={
                <Button
                  component={RouterLink}
                  to={paths.scheduling.bookSession}
                  variant="contained"
                  size="small"
                  startIcon={<EventRoundedIcon />}
                >
                  Book a session
                </Button>
              }
            />
          </SectionCard>
        </Box>
        <Box flex={1}>
          <SectionCard title="Continue Learning">
            <EmptyState
              title="Nothing in progress yet"
              description="After your first completed session, you'll be able to pick up right where you left off."
              action={
                <Button
                  component={RouterLink}
                  to={paths.discovery.tutorSearch}
                  variant="outlined"
                  size="small"
                  startIcon={<SearchRoundedIcon />}
                >
                  Find a tutor
                </Button>
              }
            />
          </SectionCard>
        </Box>
      </Stack>

      <SectionCard
        title="Recommended Tutors"
        action={
          <Button component={RouterLink} to={paths.discovery.tutorSearch} size="small">
            Browse all tutors
          </Button>
        }
      >
        <RecommendedTutors />
      </SectionCard>

      <Stack direction={{ xs: "column", md: "row" }} spacing={3} alignItems="stretch">
        <Box flex={1}>
          <SectionCard title="Recent Activity">
            <EmptyState
              title="No recent activity yet"
              description="Your bookings, completed sessions, and updates will show up here."
            />
          </SectionCard>
        </Box>
        <Box flex={1}>
          <SectionCard title="Quick actions">
            <Stack direction="row" flexWrap="wrap" gap={1.5}>
              {ROLE_QUICK_ACTIONS.Student.map((action) => (
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
          </SectionCard>
        </Box>
      </Stack>
    </Stack>
  );
}

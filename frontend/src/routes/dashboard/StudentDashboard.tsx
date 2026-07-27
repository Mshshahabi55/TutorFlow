import { Link as RouterLink, useNavigate } from "react-router-dom";
import { Box, Button, Stack, Typography } from "@mui/material";
import SearchRoundedIcon from "@mui/icons-material/SearchRounded";
import EventRoundedIcon from "@mui/icons-material/EventRounded";
import { useStudentSchedule } from "@/features/scheduling/hooks/useSessionQueries";
import { SessionCard } from "@/features/scheduling/components/SessionCard";
import { SessionCardSkeleton } from "@/features/scheduling/components/SessionCardSkeleton";
import { byScheduledTimeAscending, byScheduledTimeDescending } from "@/features/scheduling/utils/sessionSort";
import { PageHeader } from "@/shared/components/PageHeader";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { SectionCard } from "@/shared/components/SectionCard";
import { useRememberedId } from "@/shared/hooks/useRememberedId";
import { RecommendedTutors } from "@/routes/dashboard/RecommendedTutors";
import { ROLE_QUICK_ACTIONS } from "@/routes/dashboardRoleConfig";
import { SessionStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";
import { paths } from "@/routes/paths";

const RECENT_ACTIVITY_LIMIT = 3;

/**
 * Once a Student id is known (`useRememberedId` — set the first time the
 * Student uses My Lessons or the booking wizard), reuses `useStudentSchedule`
 * (the same hook `StudentSessionListPage` already uses) to show the next
 * upcoming Session and recent activity for real, instead of the permanent
 * empty state a brand new visitor with no remembered id sees.
 */
function UpcomingSessionContent({ studentId }: { studentId: string }) {
  const navigate = useNavigate();
  const scheduleQuery = useStudentSchedule(studentId);

  if (scheduleQuery.isPending) {
    return <SessionCardSkeleton />;
  }

  if (scheduleQuery.isError) {
    return <ErrorState error={scheduleQuery.error} onRetry={() => void scheduleQuery.refetch()} />;
  }

  const nextSession = scheduleQuery.data
    .filter((session) => session.status === SessionStatus.Scheduled)
    .sort(byScheduledTimeAscending)[0];

  if (!nextSession) {
    return (
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
            Book Your Lesson
          </Button>
        }
      />
    );
  }

  return (
    <SessionCard
      session={nextSession}
      onOpen={(session) => void navigate(paths.scheduling.sessionDetail(session.sessionId))}
    />
  );
}

function RecentActivityContent({ studentId }: { studentId: string }) {
  const navigate = useNavigate();
  const scheduleQuery = useStudentSchedule(studentId);

  if (scheduleQuery.isPending) {
    return <SessionCardSkeleton />;
  }

  if (scheduleQuery.isError) {
    return <ErrorState error={scheduleQuery.error} onRetry={() => void scheduleQuery.refetch()} />;
  }

  const recentActivity = scheduleQuery.data
    .filter((session: SessionDto) => session.status !== SessionStatus.Scheduled)
    .sort(byScheduledTimeDescending)
    .slice(0, RECENT_ACTIVITY_LIMIT);

  if (recentActivity.length === 0) {
    return (
      <EmptyState
        title="No recent activity yet"
        description="Your bookings, completed sessions, and updates will show up here."
        action={
          <Button
            component={RouterLink}
            to={paths.discovery.tutorSearch}
            variant="outlined"
            size="small"
            startIcon={<SearchRoundedIcon />}
          >
            Find Tutors
          </Button>
        }
      />
    );
  }

  return (
    <Stack spacing={2}>
      {recentActivity.map((session) => (
        <SessionCard
          key={session.sessionId}
          session={session}
          onOpen={(selected) => void navigate(paths.scheduling.sessionDetail(selected.sessionId))}
        />
      ))}
    </Stack>
  );
}

/**
 * The Student's landing experience — a marketplace home instead of the
 * generic role-summary dashboard every other role still sees (unchanged in
 * `DashboardPage`). Recommended Tutors reuses Discovery's existing search
 * capability; Upcoming Sessions and Recent Activity show real data once a
 * Student id is known (`useRememberedId`), and an honest empty state
 * otherwise — never fabricated. Continue Learning has no capability to
 * read at all (no "in-progress lesson" concept exists anywhere in this
 * API), so it always renders its own honest empty state.
 */
export function StudentDashboard() {
  const { id: studentId } = useRememberedId("student");

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
          <SectionCard
            title="Upcoming Sessions"
            action={
              studentId ? (
                <Button component={RouterLink} to={paths.scheduling.studentScheduleBase} size="small">
                  View all
                </Button>
              ) : undefined
            }
          >
            {studentId ? (
              <UpcomingSessionContent studentId={studentId} />
            ) : (
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
                    Book Your Lesson
                  </Button>
                }
              />
            )}
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
                  Find Tutors
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
            {studentId ? (
              <RecentActivityContent studentId={studentId} />
            ) : (
              <EmptyState
                title="No recent activity yet"
                description="Your bookings, completed sessions, and updates will show up here."
              />
            )}
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

import { Link as RouterLink, useNavigate } from "react-router-dom";
import { Box, Button, Stack, Typography } from "@mui/material";
import SearchRoundedIcon from "@mui/icons-material/SearchRounded";
import EventRoundedIcon from "@mui/icons-material/EventRounded";
import { useStudentSchedule } from "@/features/scheduling/hooks/useSessionQueries";
import { SessionCard } from "@/features/scheduling/components/SessionCard";
import { SessionStatusBreakdownChart } from "@/features/scheduling/components/SessionStatusBreakdownChart";
import { SessionCardSkeleton } from "@/features/scheduling/components/SessionCardSkeleton";
import { byScheduledTimeAscending, byScheduledTimeDescending } from "@/features/scheduling/utils/sessionSort";
import { PageHeader } from "@/shared/components/PageHeader";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { IdentityLookupErrorState } from "@/shared/components/feedback/IdentityLookupErrorState";
import { SectionCard } from "@/shared/components/SectionCard";
import { useOwnId } from "@/shared/hooks/useOwnId";
import { RecentConversationsSection } from "@/features/communication/components/RecentConversationsSection";
import { RecommendedTutors } from "@/routes/dashboard/RecommendedTutors";
import { ROLE_QUICK_ACTIONS } from "@/routes/dashboardRoleConfig";
import { SessionStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";
import { paths } from "@/routes/paths";

const RECENT_ACTIVITY_LIMIT = 3;

/**
 * Once a Student id is known (`useOwnId` — a real signed-in Student's own
 * id, resolved automatically; falls back to the dev-only "Acting as"
 * preview's remembered id otherwise), reuses `useStudentSchedule`
 * (the same hook `StudentSessionListPage` already uses) to show the next
 * upcoming Session and recent activity for real, instead of the permanent
 * empty state a brand new visitor with no remembered id sees.
 */
function UpcomingSessionContent({
  studentId,
  onChooseAgain,
}: {
  studentId: string;
  onChooseAgain: () => void;
}) {
  const navigate = useNavigate();
  const scheduleQuery = useStudentSchedule(studentId);

  if (scheduleQuery.isPending) {
    return <SessionCardSkeleton />;
  }

  if (scheduleQuery.isError) {
    return (
      <IdentityLookupErrorState
        error={scheduleQuery.error}
        onRetry={() => void scheduleQuery.refetch()}
        onChooseAgain={onChooseAgain}
      />
    );
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

function RecentActivityContent({
  studentId,
  onChooseAgain,
}: {
  studentId: string;
  onChooseAgain: () => void;
}) {
  const navigate = useNavigate();
  const scheduleQuery = useStudentSchedule(studentId);

  if (scheduleQuery.isPending) {
    return <SessionCardSkeleton />;
  }

  if (scheduleQuery.isError) {
    return (
      <IdentityLookupErrorState
        error={scheduleQuery.error}
        onRetry={() => void scheduleQuery.refetch()}
        onChooseAgain={onChooseAgain}
      />
    );
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
    <Stack spacing={3}>
      {/* From the Student's full schedule, not just the slice shown below
          — an honest breakdown, not one skewed by RECENT_ACTIVITY_LIMIT. */}
      <SessionStatusBreakdownChart sessions={scheduleQuery.data} />
      <Stack spacing={2}>
        {recentActivity.map((session) => (
          <SessionCard
            key={session.sessionId}
            session={session}
            onOpen={(selected) => void navigate(paths.scheduling.sessionDetail(selected.sessionId))}
          />
        ))}
      </Stack>
    </Stack>
  );
}

/**
 * The Student's landing experience — a marketplace home instead of the
 * generic role-summary dashboard every other role still sees (unchanged in
 * `DashboardPage`). Recommended Tutors reuses Discovery's existing search
 * capability; Upcoming Sessions and Recent Activity show real data once a
 * Student id is known (`useOwnId`), and an honest empty state
 * otherwise — never fabricated. Continue Learning has no capability to
 * read at all (no Learning Plan/Enrollment concept exists anywhere in this
 * API yet — `docs/adr/ADR-021...`, Proposed, not Accepted), so it always
 * renders its own honest empty state.
 */
export function StudentDashboard() {
  const { id: studentId, forget } = useOwnId("student");

  return (
    <Stack spacing={3}>
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
              <UpcomingSessionContent studentId={studentId} onChooseAgain={forget} />
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
          {/*
           * RC5.0: "Continue Learning" — no Learning Plan/Enrollment capability
           * exists in this API version yet (docs/adr/ADR-021..., Proposed, not
           * Accepted), so there is no real "Current Plan" any Student can ever
           * have today. This stays an honest empty state (never a fabricated
           * Current Plan/Progress/Renew) using the exact wording this phase
           * specifies, rather than pretending an enrollment exists.
           */}
          <SectionCard title="Continue Learning">
            <EmptyState
              title="You haven't enrolled in a learning plan yet"
              description="Once Learning Plans are available, your current plan, progress, and remaining lessons will show up here."
              action={
                <Button
                  component={RouterLink}
                  to={paths.discovery.tutorSearch}
                  variant="outlined"
                  size="small"
                  startIcon={<SearchRoundedIcon />}
                >
                  View Learning Plans
                </Button>
              }
            />
          </SectionCard>
        </Box>
      </Stack>

      <RecentConversationsSection />

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
              <RecentActivityContent studentId={studentId} onChooseAgain={forget} />
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

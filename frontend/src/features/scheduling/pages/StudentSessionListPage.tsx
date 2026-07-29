import { useEffect, type ReactNode } from "react";
import { Link as RouterLink, Navigate, useNavigate, useParams } from "react-router-dom";
import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import SearchRoundedIcon from "@mui/icons-material/SearchRounded";
import EventRoundedIcon from "@mui/icons-material/EventRounded";
import { useStudentSchedule } from "@/features/scheduling/hooks/useSessionQueries";
import { NextSessionCard } from "@/features/scheduling/components/NextSessionCard";
import { SessionCard } from "@/features/scheduling/components/SessionCard";
import { SessionCardSkeleton } from "@/features/scheduling/components/SessionCardSkeleton";
import { SectionCard } from "@/shared/components/SectionCard";
import { IdLookupForm } from "@/shared/components/forms/IdLookupForm";
import { useOwnId } from "@/shared/hooks/useOwnId";
import { PageHeader } from "@/shared/components/PageHeader";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { IdentityLookupErrorState } from "@/shared/components/feedback/IdentityLookupErrorState";
import { SessionStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";
import { paths } from "@/routes/paths";

function byScheduledTimeAscending(a: SessionDto, b: SessionDto): number {
  return Date.parse(a.scheduledTimeUtc) - Date.parse(b.scheduledTimeUtc);
}

function byScheduledTimeDescending(a: SessionDto, b: SessionDto): number {
  return Date.parse(b.scheduledTimeUtc) - Date.parse(a.scheduledTimeUtc);
}

interface SessionsWorkspaceProps {
  sessions: SessionDto[];
  onOpen: (session: SessionDto) => void;
}

/**
 * Groups the same `useStudentSchedule` result the page already fetched —
 * by `status`, the domain's own authoritative signal for "upcoming" vs
 * "history" (Scheduled vs Completed/Cancelled/No-Show) — into the Next
 * Lesson highlight, the rest of Upcoming Sessions, and a History section
 * split into Completed vs Cancelled/No-Show for visual distinction. No
 * additional query, no fabricated grouping field.
 */
function SessionsWorkspace({ sessions, onOpen }: SessionsWorkspaceProps) {
  if (sessions.length === 0) {
    return (
      <EmptyState
        title="No lessons yet"
        description="You haven't booked your first lesson. Find a tutor to get started."
        action={
          <Button
            component={RouterLink}
            to={paths.discovery.tutorSearch}
            variant="contained"
            startIcon={<SearchRoundedIcon />}
          >
            Find Tutors
          </Button>
        }
      />
    );
  }

  const upcoming = sessions
    .filter((session) => session.status === SessionStatus.Scheduled)
    .sort(byScheduledTimeAscending);
  const [nextSession, ...restUpcoming] = upcoming;
  const completed = sessions
    .filter((session) => session.status === SessionStatus.Completed)
    .sort(byScheduledTimeDescending);
  const cancelledOrNoShow = sessions
    .filter(
      (session) => session.status === SessionStatus.Cancelled || session.status === SessionStatus.NoShow,
    )
    .sort(byScheduledTimeDescending);

  return (
    <Stack spacing={3}>
      {nextSession ? <NextSessionCard session={nextSession} onOpen={onOpen} /> : null}

      {restUpcoming.length > 0 ? (
        <SectionCard title="Upcoming Sessions">
          <Stack spacing={2}>
            {restUpcoming.map((session) => (
              <SessionCard key={session.sessionId} session={session} onOpen={onOpen} />
            ))}
          </Stack>
        </SectionCard>
      ) : !nextSession ? (
        <SectionCard title="Upcoming Sessions">
          <Typography variant="body2" color="text.secondary">
            No upcoming sessions right now.
          </Typography>
        </SectionCard>
      ) : null}

      {completed.length > 0 || cancelledOrNoShow.length > 0 ? (
        <SectionCard title="History">
          <Stack spacing={3}>
            {completed.length > 0 ? (
              <Stack spacing={2}>
                <Typography variant="subtitle2" component="h3" color="text.secondary" fontWeight={600}>
                  Completed
                </Typography>
                <Stack spacing={2}>
                  {completed.map((session) => (
                    <SessionCard key={session.sessionId} session={session} onOpen={onOpen} />
                  ))}
                </Stack>
              </Stack>
            ) : null}
            {cancelledOrNoShow.length > 0 ? (
              <Stack spacing={2}>
                <Typography variant="subtitle2" component="h3" color="text.secondary" fontWeight={600}>
                  Cancelled &amp; No-Show
                </Typography>
                <Stack spacing={2}>
                  {cancelledOrNoShow.map((session) => (
                    <SessionCard key={session.sessionId} session={session} onOpen={onOpen} />
                  ))}
                </Stack>
              </Stack>
            ) : null}
          </Stack>
        </SectionCard>
      ) : null}

      <Button
        component={RouterLink}
        to={paths.scheduling.bookSession}
        variant="contained"
        size="large"
        startIcon={<EventRoundedIcon />}
        sx={{ alignSelf: { xs: "stretch", sm: "flex-start" } }}
      >
        Book another lesson
      </Button>
    </Stack>
  );
}

/**
 * GET /students/{id}/schedule — every Session for the Student, unpaginated
 * (matches the endpoint's own shape). RC4.3: a real signed-in Student's own
 * id resolves automatically (`useOwnId`) — the id-entry form below is
 * reachable only by a Parent/Guardian or Admin/Staff viewer looking up a
 * specific Student's schedule by id (a legitimate lookup; the route allows
 * `Student | ParentGuardian | AdminStaff`) or the dev-only "Acting as"
 * preview, since a real Student session never reaches it.
 */
export function StudentSessionListPage() {
  const { studentId: routeStudentId } = useParams<{ studentId: string }>();
  const navigate = useNavigate();
  const { id: ownStudentId, remember, forget } = useOwnId("student");

  useEffect(() => {
    if (routeStudentId) {
      remember(routeStudentId);
    }
  }, [routeStudentId, remember]);

  const studentId = routeStudentId ?? ownStudentId;

  const header = (
    <PageHeader
      title="My Lessons"
      subtitle={
        <Typography variant="body1" color="text.secondary">
          Everything you&rsquo;ve booked, grouped by what&rsquo;s next and what&rsquo;s already
          happened.
        </Typography>
      }
    />
  );

  if (!routeStudentId && ownStudentId) {
    return <Navigate to={paths.scheduling.studentSchedule(ownStudentId)} replace />;
  }

  if (!studentId) {
    return (
      <Stack spacing={3}>
        {header}
        <Card variant="outlined">
          <CardContent>
            <Stack spacing={2} alignItems="flex-start">
              <Typography variant="h5" component="h2" fontWeight={600}>
                Let&rsquo;s find your lessons
              </Typography>
              <Typography variant="body1" color="text.secondary">
                Enter a Student id to look up their lessons.
              </Typography>
              <IdLookupForm
                label="Student id"
                onSubmit={(id) => {
                  remember(id);
                  void navigate(paths.scheduling.studentSchedule(id));
                }}
              />
            </Stack>
          </CardContent>
        </Card>
      </Stack>
    );
  }

  function chooseStudentAgain() {
    forget();
    void navigate(paths.scheduling.studentScheduleBase, { replace: true });
  }

  return (
    <StudentSessionsContent studentId={studentId} header={header} onChooseAgain={chooseStudentAgain} />
  );
}

function StudentSessionsContent({
  studentId,
  header,
  onChooseAgain,
}: {
  studentId: string;
  header: ReactNode;
  onChooseAgain: () => void;
}) {
  const navigate = useNavigate();
  const scheduleQuery = useStudentSchedule(studentId);

  function openSession(session: SessionDto) {
    void navigate(paths.scheduling.sessionDetail(session.sessionId));
  }

  return (
    <Stack spacing={3}>
      {header}

      {scheduleQuery.isPending ? (
        <Stack spacing={2}>
          {Array.from({ length: 3 }, (_, index) => (
            <SessionCardSkeleton key={index} />
          ))}
        </Stack>
      ) : scheduleQuery.isError ? (
        <IdentityLookupErrorState
          error={scheduleQuery.error}
          onRetry={() => void scheduleQuery.refetch()}
          onChooseAgain={onChooseAgain}
        />
      ) : (
        <SessionsWorkspace sessions={scheduleQuery.data} onOpen={openSession} />
      )}
    </Stack>
  );
}

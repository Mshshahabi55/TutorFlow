import { Link as RouterLink, useNavigate, useParams } from "react-router-dom";
import { Button, Stack, Typography } from "@mui/material";
import SearchRoundedIcon from "@mui/icons-material/SearchRounded";
import { useStudentSchedule } from "@/features/scheduling/hooks/useSessionQueries";
import { NextSessionCard } from "@/features/scheduling/components/NextSessionCard";
import { SessionCard } from "@/features/scheduling/components/SessionCard";
import { SessionCardSkeleton } from "@/features/scheduling/components/SessionCardSkeleton";
import { SectionCard } from "@/shared/components/SectionCard";
import { IdLookupForm } from "@/shared/components/forms/IdLookupForm";
import { PageHeader } from "@/shared/components/PageHeader";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
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
        title="No sessions yet"
        description="Once you book a session with a tutor, it will appear here."
        action={
          <Button
            component={RouterLink}
            to={paths.discovery.tutorSearch}
            variant="contained"
            startIcon={<SearchRoundedIcon />}
          >
            Find a tutor
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
    </Stack>
  );
}

/** GET /students/{id}/schedule — every Session for the Student, unpaginated (matches the endpoint's own shape). Same query, route, and data as before (Phase 3 Step 5 is presentation-only). */
export function StudentSessionListPage() {
  const { studentId } = useParams<{ studentId: string }>();
  const navigate = useNavigate();
  const scheduleQuery = useStudentSchedule(studentId);

  function openSession(session: SessionDto) {
    void navigate(paths.scheduling.sessionDetail(session.sessionId));
  }

  return (
    <Stack spacing={3}>
      <PageHeader
        title="My Sessions"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Everything you&rsquo;ve booked, grouped by what&rsquo;s next and what&rsquo;s already
            happened.
          </Typography>
        }
      />

      {!studentId ? (
        <IdLookupForm
          label="Student id"
          onSubmit={(id) => {
            void navigate(paths.scheduling.studentSchedule(id));
          }}
        />
      ) : scheduleQuery.isPending ? (
        <Stack spacing={2}>
          {Array.from({ length: 3 }, (_, index) => (
            <SessionCardSkeleton key={index} />
          ))}
        </Stack>
      ) : scheduleQuery.isError ? (
        <ErrorState error={scheduleQuery.error} onRetry={() => void scheduleQuery.refetch()} />
      ) : (
        <SessionsWorkspace sessions={scheduleQuery.data} onOpen={openSession} />
      )}
    </Stack>
  );
}

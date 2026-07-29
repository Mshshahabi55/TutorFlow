import { useEffect, type ReactNode } from "react";
import { Link as RouterLink, Navigate, useNavigate, useParams } from "react-router-dom";
import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import EventAvailableRoundedIcon from "@mui/icons-material/EventAvailableRounded";
import { useTutorSchedule } from "@/features/scheduling/hooks/useSessionQueries";
import { TutorSessionCard } from "@/features/scheduling/components/TutorSessionCard";
import { SessionCardSkeleton } from "@/features/scheduling/components/SessionCardSkeleton";
import { SectionCard } from "@/shared/components/SectionCard";
import { byScheduledTimeAscending, byScheduledTimeDescending } from "@/features/scheduling/utils/sessionSort";
import { IdLookupForm } from "@/shared/components/forms/IdLookupForm";
import { useOwnId } from "@/shared/hooks/useOwnId";
import { PageHeader } from "@/shared/components/PageHeader";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { IdentityLookupErrorState } from "@/shared/components/feedback/IdentityLookupErrorState";
import { SessionStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";
import { paths } from "@/routes/paths";

interface TutorSessionsWorkspaceProps {
  sessions: SessionDto[];
  onOpen: (session: SessionDto) => void;
}

/**
 * Groups the same `useTutorSchedule` result the page already fetched by
 * `status` — Upcoming (Scheduled), Completed, and Cancelled (grouped with
 * No-Show, the same "did not happen as scheduled" pairing the Student
 * Workspace's `StudentSessionListPage` already uses) — no additional
 * query, no fabricated grouping field.
 */
function TutorSessionsWorkspace({ sessions, onOpen }: TutorSessionsWorkspaceProps) {
  if (sessions.length === 0) {
    return (
      <EmptyState
        title="No lessons yet"
        description="Lessons booked with you will appear here once you've added some teaching time."
        action={
          <Button
            component={RouterLink}
            to={paths.scheduling.declareAvailability}
            variant="contained"
            startIcon={<EventAvailableRoundedIcon />}
          >
            Manage Your Schedule
          </Button>
        }
      />
    );
  }

  const upcoming = sessions
    .filter((session) => session.status === SessionStatus.Scheduled)
    .sort(byScheduledTimeAscending);
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
      <SectionCard title="Upcoming Lessons">
        {upcoming.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            No upcoming lessons right now.
          </Typography>
        ) : (
          <Stack spacing={2}>
            {upcoming.map((session) => (
              <TutorSessionCard key={session.sessionId} session={session} onOpen={onOpen} />
            ))}
          </Stack>
        )}
      </SectionCard>

      {completed.length > 0 ? (
        <SectionCard title="Completed">
          <Stack spacing={2}>
            {completed.map((session) => (
              <TutorSessionCard key={session.sessionId} session={session} onOpen={onOpen} />
            ))}
          </Stack>
        </SectionCard>
      ) : null}

      {cancelledOrNoShow.length > 0 ? (
        <SectionCard title="Cancelled & No-Show">
          <Stack spacing={2}>
            {cancelledOrNoShow.map((session) => (
              <TutorSessionCard key={session.sessionId} session={session} onOpen={onOpen} />
            ))}
          </Stack>
        </SectionCard>
      ) : null}
    </Stack>
  );
}

/**
 * GET /tutors/{id}/schedule — every Session for the Tutor, unpaginated
 * (matches the endpoint's own shape). RC4.3: a real signed-in Tutor's own
 * id resolves automatically (`useOwnId`) — the id-entry form below is
 * reachable only by an Admin/Staff viewer looking up another Tutor's
 * schedule by id (a legitimate lookup, not a "my own id" prompt; the
 * route itself allows `Tutor | AdminStaff`) or the dev-only "Acting as"
 * preview, since a real Tutor session never reaches it.
 */
export function TutorSessionListPage() {
  const { tutorId: routeTutorId } = useParams<{ tutorId: string }>();
  const navigate = useNavigate();
  const { id: ownTutorId, remember, forget } = useOwnId("tutor");

  useEffect(() => {
    if (routeTutorId) {
      remember(routeTutorId);
    }
  }, [routeTutorId, remember]);

  const tutorId = routeTutorId ?? ownTutorId;
  const header = <PageHeader title="My Lessons" />;

  if (!routeTutorId && ownTutorId) {
    return <Navigate to={paths.scheduling.tutorSchedule(ownTutorId)} replace />;
  }

  if (!tutorId) {
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
                Enter a Tutor id to look up their lessons.
              </Typography>
              <IdLookupForm
                label="Tutor id"
                onSubmit={(id) => {
                  remember(id);
                  void navigate(paths.scheduling.tutorSchedule(id));
                }}
              />
            </Stack>
          </CardContent>
        </Card>
      </Stack>
    );
  }

  function chooseTutorAgain() {
    forget();
    void navigate(paths.scheduling.tutorScheduleBase, { replace: true });
  }

  return (
    <TutorSessionsContent tutorId={tutorId} header={header} onChooseAgain={chooseTutorAgain} />
  );
}

function TutorSessionsContent({
  tutorId,
  header,
  onChooseAgain,
}: {
  tutorId: string;
  header: ReactNode;
  onChooseAgain: () => void;
}) {
  const navigate = useNavigate();
  const scheduleQuery = useTutorSchedule(tutorId);

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
        <TutorSessionsWorkspace sessions={scheduleQuery.data} onOpen={openSession} />
      )}
    </Stack>
  );
}

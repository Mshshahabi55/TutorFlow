import { useEffect, type ReactNode } from "react";
import { Link as RouterLink, Navigate, useNavigate, useParams } from "react-router-dom";
import { Button, Stack, Typography } from "@mui/material";
import EventAvailableRoundedIcon from "@mui/icons-material/EventAvailableRounded";
import { useTutorSchedule } from "@/features/scheduling/hooks/useSessionQueries";
import { TutorSessionCard } from "@/features/scheduling/components/TutorSessionCard";
import { SessionCardSkeleton } from "@/features/scheduling/components/SessionCardSkeleton";
import { SectionCard } from "@/shared/components/SectionCard";
import { byScheduledTimeAscending, byScheduledTimeDescending } from "@/features/scheduling/utils/sessionSort";
import { IdentityGate } from "@/shared/components/IdentityGate";
import { useRememberedId } from "@/shared/hooks/useRememberedId";
import { PageHeader } from "@/shared/components/PageHeader";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
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

/** GET /tutors/{id}/schedule — every Session for the Tutor, unpaginated (matches the endpoint's own shape). Same query and data as before; RC2 replaces the raw Tutor-id form with the shared `IdentityGate` (ask once, remember on this device). */
export function TutorSessionListPage() {
  const { tutorId: routeTutorId } = useParams<{ tutorId: string }>();
  const { id: rememberedTutorId, remember } = useRememberedId("tutor");

  useEffect(() => {
    if (routeTutorId) {
      remember(routeTutorId);
    }
  }, [routeTutorId, remember]);

  const tutorId = routeTutorId ?? rememberedTutorId;
  const header = <PageHeader title="My Lessons" />;

  if (!routeTutorId && rememberedTutorId) {
    return <Navigate to={paths.scheduling.tutorSchedule(rememberedTutorId)} replace />;
  }

  if (!tutorId) {
    return (
      <Stack spacing={3}>
        {header}
        <IdentityGate
          kind="tutor"
          fieldLabel="Tutor id"
          title="Let's find your lessons"
          description="Enter your tutor id once — we'll remember it on this device so you won't need to again."
        >
          {(id) => <Navigate to={paths.scheduling.tutorSchedule(id)} replace />}
        </IdentityGate>
      </Stack>
    );
  }

  return <TutorSessionsContent tutorId={tutorId} header={header} />;
}

function TutorSessionsContent({ tutorId, header }: { tutorId: string; header: ReactNode }) {
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
        <ErrorState error={scheduleQuery.error} onRetry={() => void scheduleQuery.refetch()} />
      ) : (
        <TutorSessionsWorkspace sessions={scheduleQuery.data} onOpen={openSession} />
      )}
    </Stack>
  );
}

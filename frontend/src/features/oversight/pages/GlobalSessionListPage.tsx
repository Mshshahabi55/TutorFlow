import { useNavigate } from "react-router-dom";
import { Stack, TablePagination, Typography } from "@mui/material";
import { useAllSessions } from "@/features/oversight/hooks/useAllSessions";
import { AdminSessionCard } from "@/features/oversight/components/AdminSessionCard";
import { AdminSessionCardSkeleton } from "@/features/oversight/components/AdminSessionCardSkeleton";
import { SectionCard } from "@/shared/components/SectionCard";
import { PageHeader } from "@/shared/components/PageHeader";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { usePagination } from "@/shared/hooks/usePagination";
import { SessionStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";
import { paths } from "@/routes/paths";

const SKELETON_COUNT = 4;

interface SessionGroupsProps {
  sessions: SessionDto[];
  onOpen: (session: SessionDto) => void;
}

/** Groups the current page's Sessions by status — the same Upcoming/Completed/Cancelled & No-Show split the Student and Tutor Workspaces already use for their own schedules, scoped here to whichever page of the platform-wide list is currently loaded. */
function SessionGroups({ sessions, onOpen }: SessionGroupsProps) {
  const upcoming = sessions.filter((session) => session.status === SessionStatus.Scheduled);
  const completed = sessions.filter((session) => session.status === SessionStatus.Completed);
  const cancelledOrNoShow = sessions.filter(
    (session) => session.status === SessionStatus.Cancelled || session.status === SessionStatus.NoShow,
  );

  return (
    <Stack spacing={3}>
      {upcoming.length > 0 ? (
        <SectionCard title="Upcoming">
          <Stack spacing={2}>
            {upcoming.map((session) => (
              <AdminSessionCard key={session.sessionId} session={session} onOpen={onOpen} />
            ))}
          </Stack>
        </SectionCard>
      ) : null}

      {completed.length > 0 ? (
        <SectionCard title="Completed">
          <Stack spacing={2}>
            {completed.map((session) => (
              <AdminSessionCard key={session.sessionId} session={session} onOpen={onOpen} />
            ))}
          </Stack>
        </SectionCard>
      ) : null}

      {cancelledOrNoShow.length > 0 ? (
        <SectionCard title="Cancelled & No-Show">
          <Stack spacing={2}>
            {cancelledOrNoShow.map((session) => (
              <AdminSessionCard key={session.sessionId} session={session} onOpen={onOpen} />
            ))}
          </Stack>
        </SectionCard>
      ) : null}
    </Stack>
  );
}

/**
 * GET /sessions — every Session across the platform, paginated (ADM-3:
 * "view all schedules"). Same query, route, and pagination as before
 * (Phase 3 Step 8 is presentation-only); grouping and cards are scoped to
 * whichever page is currently loaded, not the whole platform at once.
 */
export function GlobalSessionListPage() {
  const navigate = useNavigate();
  const { page, pageSize, setPage, setPageSize } = usePagination();
  const sessionsQuery = useAllSessions(page, pageSize);

  function openSession(session: SessionDto) {
    void navigate(paths.scheduling.sessionDetail(session.sessionId));
  }

  return (
    <Stack spacing={3}>
      <PageHeader
        title="All sessions"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Every Session booked across the platform, grouped by status. Select one to inspect it.
          </Typography>
        }
      />

      {sessionsQuery.isPending ? (
        <Stack spacing={2}>
          {Array.from({ length: SKELETON_COUNT }, (_, index) => (
            <AdminSessionCardSkeleton key={index} />
          ))}
        </Stack>
      ) : sessionsQuery.isError ? (
        <ErrorState error={sessionsQuery.error} onRetry={() => void sessionsQuery.refetch()} />
      ) : sessionsQuery.data.items.length === 0 ? (
        <EmptyState
          title="No sessions have been booked yet"
          description="Sessions will appear here as they're booked across the platform."
        />
      ) : (
        <>
          <SessionGroups sessions={sessionsQuery.data.items} onOpen={openSession} />
          <TablePagination
            component="div"
            count={sessionsQuery.data.totalCount}
            page={page - 1}
            rowsPerPage={pageSize}
            rowsPerPageOptions={[10, 20, 50]}
            onPageChange={(_event, newPage) => setPage(newPage + 1)}
            onRowsPerPageChange={(event) => {
              setPageSize(Number(event.target.value));
              setPage(1);
            }}
          />
        </>
      )}
    </Stack>
  );
}

import { Stack, Typography } from "@mui/material";
import { useTutorSchedule } from "@/features/scheduling/hooks/useSessionQueries";
import { StudentRosterCard, type StudentRosterEntry } from "@/features/scheduling/components/StudentRosterCard";
import { SessionCardSkeleton } from "@/features/scheduling/components/SessionCardSkeleton";
import { IdentityGate } from "@/shared/components/IdentityGate";
import { PageHeader } from "@/shared/components/PageHeader";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { SessionStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";

function deriveRoster(sessions: SessionDto[]): StudentRosterEntry[] {
  const byStudent = new Map<string, StudentRosterEntry>();

  for (const session of sessions) {
    const entry = byStudent.get(session.studentId) ?? {
      studentId: session.studentId,
      upcomingSessions: 0,
      totalSessions: 0,
    };
    entry.totalSessions += 1;
    if (session.status === SessionStatus.Scheduled) {
      entry.upcomingSessions += 1;
    }
    byStudent.set(session.studentId, entry);
  }

  return Array.from(byStudent.values()).sort((a, b) => b.upcomingSessions - a.upcomingSessions);
}

/**
 * Reuses `useTutorSchedule` (the same hook `TutorSessionListPage` and the
 * Tutor Dashboard already use) to derive the Tutor's unique Students —
 * client-side grouping over data already fetched, not a new endpoint. There
 * is no "list of my Students" capability in the API, so this is the honest
 * version of one: exactly the Students that show up in the Tutor's own
 * schedule, with session counts, nothing fabricated.
 */
function MyStudentsRoster({ tutorId }: { tutorId: string }) {
  const scheduleQuery = useTutorSchedule(tutorId);

  if (scheduleQuery.isPending) {
    return (
      <Stack spacing={2}>
        <SessionCardSkeleton />
        <SessionCardSkeleton />
      </Stack>
    );
  }

  if (scheduleQuery.isError) {
    return <ErrorState error={scheduleQuery.error} onRetry={() => void scheduleQuery.refetch()} />;
  }

  const roster = deriveRoster(scheduleQuery.data);

  if (roster.length === 0) {
    return (
      <EmptyState
        title="No students yet"
        description="Once a Student books a lesson with you, they'll appear here."
      />
    );
  }

  return (
    <Stack spacing={2}>
      {roster.map((entry) => (
        <StudentRosterCard key={entry.studentId} entry={entry} />
      ))}
    </Stack>
  );
}

/** "My Students" — the Tutor Workspace's roster view, reusing the same schedule data `TutorSessionListPage` fetches. */
export function TutorStudentsPage() {
  return (
    <Stack spacing={3}>
      <PageHeader
        title="My Students"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Everyone you&rsquo;ve taught or have a lesson scheduled with.
          </Typography>
        }
      />

      <IdentityGate
        kind="tutor"
        fieldLabel="Tutor id"
        title="Let's find your students"
        description="Enter your tutor id once — we'll remember it on this device so you won't need to again."
      >
        {(tutorId) => <MyStudentsRoster tutorId={tutorId} />}
      </IdentityGate>
    </Stack>
  );
}

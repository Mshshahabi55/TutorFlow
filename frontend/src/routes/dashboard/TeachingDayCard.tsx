import { Stack, Typography } from "@mui/material";
import { TutorSessionCard } from "@/features/scheduling/components/TutorSessionCard";
import { DashboardSectionCard } from "@/routes/dashboard/DashboardSectionCard";
import type { SessionDto } from "@/services/api/dtos";

export interface TeachingDayCardProps {
  /** Already filtered to today (Tehran calendar day) by the caller — this component only presents. */
  todaySessions: SessionDto[];
  onOpen: (session: SessionDto) => void;
}

/** The Tutor Dashboard's "what does today look like" highlight — derived from the same `useTutorSchedule` result the page already fetched, not a new query. */
export function TeachingDayCard({ todaySessions, onOpen }: TeachingDayCardProps) {
  return (
    <DashboardSectionCard title="Today's Sessions">
      {todaySessions.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          No sessions scheduled for today.
        </Typography>
      ) : (
        <Stack spacing={2}>
          {todaySessions.map((session) => (
            <TutorSessionCard key={session.sessionId} session={session} onOpen={onOpen} />
          ))}
        </Stack>
      )}
    </DashboardSectionCard>
  );
}

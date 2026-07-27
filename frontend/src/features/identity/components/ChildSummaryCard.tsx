import { Link as RouterLink } from "react-router-dom";
import { Avatar, Box, Button, Card, CardContent, Skeleton, Stack, Typography } from "@mui/material";
import PersonRoundedIcon from "@mui/icons-material/PersonRounded";
import CalendarMonthRoundedIcon from "@mui/icons-material/CalendarMonthRounded";
import EventRoundedIcon from "@mui/icons-material/EventRounded";
import { useStudentSchedule } from "@/features/scheduling/hooks/useSessionQueries";
import { useTutor } from "@/features/identity/hooks/useTutorQueries";
import { byScheduledTimeAscending, byScheduledTimeDescending } from "@/features/scheduling/utils/sessionSort";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { monoFontFamily } from "@/app/theme";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { RelationshipStatus, SessionStatus } from "@/services/api/dtos";
import type { RelationshipDto } from "@/services/api/dtos";
import { paths } from "@/routes/paths";

export interface ChildSummaryCardProps {
  relationship: RelationshipDto;
}

/**
 * A Parent/Guardian-specific card: presents one of their Student
 * Relationships as "a child." Neither `RelationshipDto` nor `StudentDto`
 * has a name field, so `studentId` is shown as-is — same honesty
 * convention as every other entity without a name field in this app.
 *
 * Only a Confirmed Relationship reuses `useStudentSchedule` (the same
 * hook `StudentSessionListPage` already uses) for Next/Recent lesson and
 * "Current tutor" (`useTutor`, resolving that lesson's own `tutorId` — the
 * Tutor's `subject` stands in for a name, same convention as everywhere
 * else `TutorDto` is shown). An Invited-but-not-yet-confirmed Relationship
 * isn't an authorized family link yet, so it only shows its own status,
 * never session data that isn't really the parent's to view.
 */
export function ChildSummaryCard({ relationship }: ChildSummaryCardProps) {
  const isConfirmed = relationship.status === RelationshipStatus.Confirmed;
  const scheduleQuery = useStudentSchedule(isConfirmed ? relationship.studentId : undefined);

  const sessions = scheduleQuery.data ?? [];
  const nextSession = sessions
    .filter((session) => session.status === SessionStatus.Scheduled)
    .sort(byScheduledTimeAscending)[0];
  const recentSession = sessions
    .filter((session) => session.status !== SessionStatus.Scheduled)
    .sort(byScheduledTimeDescending)[0];
  const currentTutorId = nextSession?.tutorId ?? recentSession?.tutorId;
  const tutorQuery = useTutor(currentTutorId);

  return (
    <Card variant="outlined">
      <CardContent>
        <Stack direction="row" spacing={2} alignItems="center">
          <Avatar sx={{ width: 48, height: 48, bgcolor: "action.selected" }}>
            <PersonRoundedIcon color="disabled" aria-hidden="true" />
          </Avatar>
          <Box flex={1} minWidth={0}>
            <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
              <Typography
                variant="subtitle1"
                fontWeight={600}
                fontFamily={monoFontFamily}
                sx={{ wordBreak: "break-all" }}
              >
                {relationship.studentId}
              </Typography>
              <StatusPill
                label={isConfirmed ? "Confirmed" : "Invited"}
                tone={isConfirmed ? "success" : "warning"}
              />
            </Stack>
          </Box>
        </Stack>

        {isConfirmed ? (
          <Stack spacing={1.5} mt={2}>
            {scheduleQuery.isPending ? (
              <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
                <Skeleton variant="text" width="45%" />
                <Skeleton variant="text" width="45%" />
              </Stack>
            ) : (
              <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
                <Box flex={1}>
                  <Typography variant="caption" color="text.secondary">
                    Next lesson
                  </Typography>
                  <Typography variant="body2">
                    {nextSession ? toTehranDisplay(nextSession.scheduledTimeUtc) : "None scheduled"}
                  </Typography>
                </Box>
                <Box flex={1}>
                  <Typography variant="caption" color="text.secondary">
                    Recent lesson
                  </Typography>
                  <Typography variant="body2">
                    {recentSession ? toTehranDisplay(recentSession.scheduledTimeUtc) : "No lessons yet"}
                  </Typography>
                </Box>
              </Stack>
            )}

            <Box>
              <Typography variant="caption" color="text.secondary">
                Current tutor
              </Typography>
              {scheduleQuery.isPending || (Boolean(currentTutorId) && tutorQuery.isPending) ? (
                <Skeleton variant="text" width="40%" />
              ) : (
                <Typography variant="body2">
                  {tutorQuery.data?.subject ?? (currentTutorId ? "Tutor" : "No tutor yet")}
                </Typography>
              )}
            </Box>

            <Typography variant="caption" color="text.secondary" fontStyle="italic">
              Progress tracking coming soon
            </Typography>

            <Stack direction="row" spacing={1} flexWrap="wrap">
              <Button
                component={RouterLink}
                to={paths.scheduling.studentSchedule(relationship.studentId)}
                size="small"
                variant="outlined"
                startIcon={<CalendarMonthRoundedIcon />}
              >
                View Lessons
              </Button>
              <Button
                component={RouterLink}
                to={
                  currentTutorId
                    ? `${paths.scheduling.bookSession}?tutorId=${currentTutorId}`
                    : paths.scheduling.bookSession
                }
                size="small"
                variant="contained"
                startIcon={<EventRoundedIcon />}
              >
                Book Lesson
              </Button>
            </Stack>
          </Stack>
        ) : (
          <Typography variant="body2" color="text.secondary" mt={2}>
            Waiting for this Relationship to be confirmed.
          </Typography>
        )}
      </CardContent>
    </Card>
  );
}

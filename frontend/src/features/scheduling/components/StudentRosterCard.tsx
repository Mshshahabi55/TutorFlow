import { useState } from "react";
import { Link as RouterLink, useNavigate } from "react-router-dom";
import { Avatar, Box, Button, Card, CardContent, Stack, Tooltip, Typography } from "@mui/material";
import PersonRoundedIcon from "@mui/icons-material/PersonRounded";
import { TutorSessionCard } from "@/features/scheduling/components/TutorSessionCard";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { monoFontFamily } from "@/app/theme";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { paths } from "@/routes/paths";
import type { StudentRosterEntry } from "@/features/scheduling/utils/studentRoster";
import type { SessionDto } from "@/services/api/dtos";

export interface StudentRosterCardProps {
  entry: StudentRosterEntry;
}

/**
 * A Tutor-specific relationship card for "My Students" and the Dashboard's
 * "Students Requiring Attention" — mirrors `ChildSummaryCard`'s layout
 * (avatar placeholder, mono id, a status pill) since both are "here is one
 * person, honestly, with no name field to show" cards.
 *
 * "View Profile" and "Book Follow-up" (as named in the marketplace-parity
 * brief) aren't real destinations for a Tutor: `StudentDetailPage` is
 * Student/ParentGuardian/AdminStaff-only, and the booking wizard is
 * Student/ParentGuardian-only (`router.tsx`) — a Tutor has no authorized
 * route to either. Rather than link to a page that would 403, this card's
 * actions are the honest equivalents: "View History" expands this
 * Student's own lessons (already-fetched data, no new request), and "Add
 * Availability" is the real lever a Tutor has to make a follow-up lesson
 * possible. "Message" stays a disabled placeholder, same as everywhere
 * else in this app — there is no messaging capability yet.
 */
export function StudentRosterCard({ entry }: StudentRosterCardProps) {
  const navigate = useNavigate();
  const [showHistory, setShowHistory] = useState(false);

  function openSession(session: SessionDto) {
    void navigate(paths.scheduling.sessionDetail(session.sessionId));
  }

  return (
    <Card variant="outlined">
      <CardContent>
        <Stack spacing={2}>
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
                  {entry.studentId}
                </Typography>
                {entry.upcomingSessions > 0 ? (
                  <StatusPill label={`${entry.upcomingSessions} upcoming`} tone="success" />
                ) : null}
              </Stack>
              <Typography variant="body2" color="text.secondary" mt={0.5}>
                {entry.totalSessions} {entry.totalSessions === 1 ? "lesson" : "lessons"} together
              </Typography>
            </Box>
          </Stack>

          <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
            <Box flex={1}>
              <Typography variant="caption" color="text.secondary">
                Next lesson
              </Typography>
              <Typography variant="body2">
                {entry.nextSession ? toTehranDisplay(entry.nextSession.scheduledTimeUtc) : "None scheduled"}
              </Typography>
            </Box>
            <Box flex={1}>
              <Typography variant="caption" color="text.secondary">
                Last lesson
              </Typography>
              <Typography variant="body2">
                {entry.mostRecentPastSession
                  ? toTehranDisplay(entry.mostRecentPastSession.scheduledTimeUtc)
                  : "No lessons yet"}
              </Typography>
            </Box>
          </Stack>

          <Typography variant="caption" color="text.secondary" fontStyle="italic">
            Progress tracking coming soon
          </Typography>

          <Stack direction="row" spacing={1} flexWrap="wrap">
            <Button size="small" variant="outlined" onClick={() => setShowHistory((value) => !value)}>
              {showHistory ? "Hide History" : "View History"}
            </Button>
            <Button
              size="small"
              variant="outlined"
              component={RouterLink}
              to={paths.scheduling.declareAvailability}
            >
              Add Availability
            </Button>
            <Tooltip title="Messaging is coming soon">
              <span>
                <Button size="small" variant="outlined" disabled>
                  Message
                </Button>
              </span>
            </Tooltip>
          </Stack>

          {showHistory ? (
            <Stack spacing={1.5} pt={1} borderTop="1px solid" borderColor="divider">
              {entry.sessions.map((session) => (
                <TutorSessionCard key={session.sessionId} session={session} onOpen={openSession} />
              ))}
            </Stack>
          ) : null}
        </Stack>
      </CardContent>
    </Card>
  );
}

import { Link as RouterLink } from "react-router-dom";
import { Button, Card, CardContent, Chip, Stack, Tooltip, Typography } from "@mui/material";
import EventAvailableRoundedIcon from "@mui/icons-material/EventAvailableRounded";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { timeSpanToMinutes } from "@/shared/utils/duration";
import { paths } from "@/routes/paths";
import type { SessionDto } from "@/services/api/dtos";

/**
 * A plain elapsed-time countdown against the current instant — no Tehran
 * offset math involved (a duration is the same length regardless of
 * timezone), so this stays local to the component rather than living in
 * `shared/time/tehranTime.ts`, which owns only UTC<->Tehran conversions.
 */
function formatCountdown(scheduledTimeUtc: string, now: number): string {
  const diffMinutes = Math.round((Date.parse(scheduledTimeUtc) - now) / 60_000);

  if (diffMinutes <= 0) {
    return "Starting now";
  }

  const hours = Math.floor(diffMinutes / 60);
  const minutes = diffMinutes % 60;

  if (hours === 0) {
    return `in ${minutes} min`;
  }
  if (minutes === 0) {
    return `in ${hours}h`;
  }
  return `in ${hours}h ${minutes}min`;
}

export interface NextLessonHeroCardProps {
  session: SessionDto;
  /** The Tutor's own subject (`TutorDto.subject`) — a Session has no subject of its own. */
  subject: string | null;
  now?: number;
}

/**
 * The Tutor Dashboard's hero — "who's next, and how soon." `SessionDto`
 * has no Student name, so `studentId` is shown as-is, same honesty
 * convention as everywhere else in this app. The Join/Prepare action is a
 * placeholder: there is no video-call or lesson-prep capability in this
 * API, so it's disabled with an explanatory tooltip rather than a fake
 * live button — "Open Lesson" (the real, working action) stays primary.
 */
export function NextLessonHeroCard({ session, subject, now = Date.now() }: NextLessonHeroCardProps) {
  return (
    <Card variant="outlined" sx={{ borderColor: "primary.main", borderWidth: 2 }}>
      <CardContent>
        <Stack spacing={2}>
          <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
            <Typography variant="overline" color="text.secondary">
              Your Next Lesson
            </Typography>
            <Chip
              label={formatCountdown(session.scheduledTimeUtc, now)}
              color="primary"
              size="small"
            />
          </Stack>

          <Stack direction={{ xs: "column", sm: "row" }} spacing={1} alignItems={{ sm: "center" }}>
            <Typography variant="h5" component="p" fontWeight={600}>
              {subject ?? "Lesson"}
            </Typography>
            <Typography variant="body1" color="text.secondary">
              with Student: {session.studentId}
            </Typography>
          </Stack>

          <Typography variant="body2" color="text.secondary">
            {toTehranDisplay(session.scheduledTimeUtc)} · {timeSpanToMinutes(session.duration)} min
          </Typography>

          <Stack direction="row" spacing={1.5} flexWrap="wrap">
            <Button
              component={RouterLink}
              to={paths.scheduling.sessionDetail(session.sessionId)}
              variant="contained"
              startIcon={<EventAvailableRoundedIcon />}
            >
              Open Lesson
            </Button>
            <Tooltip title="Video call integration is coming soon">
              <span>
                <Button variant="outlined" disabled>
                  Join / Prepare
                </Button>
              </span>
            </Tooltip>
          </Stack>
        </Stack>
      </CardContent>
    </Card>
  );
}

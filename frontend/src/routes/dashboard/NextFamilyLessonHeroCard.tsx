import { Link as RouterLink } from "react-router-dom";
import { Button, Card, CardContent, Chip, Stack, Typography } from "@mui/material";
import EventAvailableRoundedIcon from "@mui/icons-material/EventAvailableRounded";
import { useTutor } from "@/features/identity/hooks/useTutorQueries";
import { formatCountdown } from "@/features/scheduling/utils/countdown";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { timeSpanToMinutes } from "@/shared/utils/duration";
import { paths } from "@/routes/paths";
import type { SessionDto } from "@/services/api/dtos";

export interface NextFamilyLessonHeroCardProps {
  session: SessionDto;
  now?: number;
}

/**
 * The Parent Dashboard's hero — "which child, with whom, and how soon."
 * Unlike the Tutor's own `NextLessonHeroCard` (where the Tutor is the
 * constant and only the Student varies), a Parent's family spans several
 * children, so both `studentId` ("Child") and the Tutor need to be shown
 * — the Tutor's own `subject` stands in for a name, same convention used
 * everywhere `TutorDto` is displayed without a name field.
 */
export function NextFamilyLessonHeroCard({ session, now = Date.now() }: NextFamilyLessonHeroCardProps) {
  const tutorQuery = useTutor(session.tutorId);

  return (
    <Card variant="outlined" sx={{ borderColor: "primary.main", borderWidth: 2 }}>
      <CardContent>
        <Stack spacing={2}>
          <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
            <Typography variant="overline" color="text.secondary">
              Next Lesson
            </Typography>
            <Chip label={formatCountdown(session.scheduledTimeUtc, now)} color="primary" size="small" />
          </Stack>

          <Stack direction={{ xs: "column", sm: "row" }} spacing={1} alignItems={{ sm: "center" }}>
            <Typography variant="h5" component="p" fontWeight={600}>
              {tutorQuery.data?.subject ?? "Lesson"}
            </Typography>
            <Typography variant="body1" color="text.secondary">
              with Child: {session.studentId}
            </Typography>
          </Stack>

          <Typography variant="body2" color="text.secondary">
            {toTehranDisplay(session.scheduledTimeUtc)} · {timeSpanToMinutes(session.duration)} min
          </Typography>

          <Button
            component={RouterLink}
            to={paths.scheduling.sessionDetail(session.sessionId)}
            variant="contained"
            startIcon={<EventAvailableRoundedIcon />}
            sx={{ alignSelf: { xs: "stretch", sm: "flex-start" } }}
          >
            View Lesson
          </Button>
        </Stack>
      </CardContent>
    </Card>
  );
}

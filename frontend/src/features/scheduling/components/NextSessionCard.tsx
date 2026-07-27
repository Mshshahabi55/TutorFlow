import { Card, CardActionArea, CardActions, CardContent, Stack, Typography } from "@mui/material";
import { SessionActions } from "@/features/scheduling/components/SessionActions";
import { SessionStatusBadge } from "@/features/scheduling/components/SessionStatusBadge";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { timeSpanToMinutes } from "@/shared/utils/duration";
import { monoFontFamily } from "@/app/theme";
import { DeliveryMode } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";

export interface NextSessionCardProps {
  session: SessionDto;
  onOpen: (session: SessionDto) => void;
}

/**
 * Highlights the Student's next Scheduled Session (the earliest-starting
 * one — derived by sorting the same `useStudentSchedule` result already
 * fetched for the page, not a new query). Only rendered when at least one
 * Scheduled Session exists; there is no separate "next lesson" endpoint or
 * field to fabricate this from otherwise. Same clickable-content /
 * separate-actions split as `SessionCard`, so `SessionActions` is never
 * nested inside another interactive element.
 */
export function NextSessionCard({ session, onOpen }: NextSessionCardProps) {
  return (
    <Card variant="outlined" sx={{ borderColor: "primary.main", borderWidth: 2 }}>
      <CardActionArea onClick={() => onOpen(session)}>
        <CardContent>
          <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={1}>
            <Typography variant="h5" component="h2" fontWeight={600} color="primary.main">
              Next Lesson
            </Typography>
            <SessionStatusBadge status={session.status} />
          </Stack>
          <Typography variant="subtitle1" fontWeight={600} mt={1}>
            {toTehranDisplay(session.scheduledTimeUtc)}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {timeSpanToMinutes(session.duration)} minutes ·{" "}
            {session.deliveryMode === DeliveryMode.Online ? "Online" : "In-Person"}
          </Typography>
          <Typography
            variant="caption"
            color="text.secondary"
            fontFamily={monoFontFamily}
            sx={{ wordBreak: "break-all", display: "block" }}
          >
            Tutor: {session.tutorId}
          </Typography>
        </CardContent>
      </CardActionArea>
      <CardActions sx={{ px: 2, pb: 2, pt: 0 }}>
        <SessionActions session={session} />
      </CardActions>
    </Card>
  );
}

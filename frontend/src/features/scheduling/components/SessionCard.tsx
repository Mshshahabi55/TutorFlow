import { Card, CardActionArea, CardActions, CardContent, Stack, Typography } from "@mui/material";
import { SessionActions } from "@/features/scheduling/components/SessionActions";
import { SessionStatusBadge } from "@/features/scheduling/components/SessionStatusBadge";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { timeSpanToMinutes } from "@/shared/utils/duration";
import { monoFontFamily } from "@/app/theme";
import { DeliveryMode } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";

export interface SessionCardProps {
  session: SessionDto;
  onOpen: (session: SessionDto) => void;
}

/**
 * One Session, as a card instead of a table row. The clickable region
 * (`CardActionArea`, keyboard-operable by default) covers only the
 * informational content; `SessionActions` sits in a separate `CardActions`
 * region below it, so the Complete/No-Show/Cancel buttons are never
 * nested inside another interactive element — no `stopPropagation` needed,
 * unlike the old `DataTable` row's own workaround for the same problem.
 * `tutorId` is shown as-is (no Tutor name/subject fetched per row) —
 * fetching one Tutor per Session in a list would be a new per-row query
 * this phase must not introduce.
 */
export function SessionCard({ session, onOpen }: SessionCardProps) {
  return (
    <Card variant="outlined">
      <CardActionArea onClick={() => onOpen(session)}>
        <CardContent>
          <Stack spacing={1}>
            <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={1}>
              <Typography variant="subtitle1" fontWeight={600}>
                {toTehranDisplay(session.scheduledTimeUtc)}
              </Typography>
              <SessionStatusBadge status={session.status} />
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {timeSpanToMinutes(session.duration)} min ·{" "}
              {session.deliveryMode === DeliveryMode.Online ? "Online" : "In-Person"}
            </Typography>
            <Typography
              variant="caption"
              color="text.secondary"
              fontFamily={monoFontFamily}
              sx={{ wordBreak: "break-all" }}
            >
              Tutor: {session.tutorId}
            </Typography>
          </Stack>
        </CardContent>
      </CardActionArea>
      <CardActions sx={{ px: 2, pb: 2, pt: 0 }}>
        <SessionActions session={session} />
      </CardActions>
    </Card>
  );
}

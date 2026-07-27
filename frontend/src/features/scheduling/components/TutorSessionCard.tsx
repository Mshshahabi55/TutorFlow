import { Button, Card, CardActionArea, CardActions, CardContent, Stack, Typography } from "@mui/material";
import { SessionActions } from "@/features/scheduling/components/SessionActions";
import { SessionStatusBadge } from "@/features/scheduling/components/SessionStatusBadge";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { timeSpanToMinutes } from "@/shared/utils/duration";
import { monoFontFamily } from "@/app/theme";
import { DeliveryMode } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";

export interface TutorSessionCardProps {
  session: SessionDto;
  onOpen: (session: SessionDto) => void;
}

/**
 * The Tutor Workspace's own session card — same shape and clickable-
 * content/separate-actions split as the Student Workspace's `SessionCard`
 * (Phase 3 Step 5), but surfacing `studentId` instead of `tutorId`: from a
 * Tutor's own schedule, the *Tutor* is the constant (themselves) and the
 * *Student* is the variable worth showing. That's the one piece of
 * information whose responsibility genuinely differs between the two
 * workspaces, so it gets its own small component instead of reusing
 * `SessionCard` with a prop to swap the label.
 */
export function TutorSessionCard({ session, onOpen }: TutorSessionCardProps) {
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
              Student: {session.studentId}
            </Typography>
          </Stack>
        </CardContent>
      </CardActionArea>
      <CardActions sx={{ px: 2, pb: 2, pt: 0, flexWrap: "wrap", gap: 1 }}>
        <Button size="small" variant="contained" onClick={() => onOpen(session)}>
          Open Lesson
        </Button>
        <SessionActions session={session} />
      </CardActions>
    </Card>
  );
}

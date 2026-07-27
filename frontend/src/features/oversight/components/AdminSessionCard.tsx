import { Card, CardActionArea, CardActions, CardContent, Stack, Typography } from "@mui/material";
import { SessionActions } from "@/features/scheduling/components/SessionActions";
import { SessionStatusBadge } from "@/features/scheduling/components/SessionStatusBadge";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { timeSpanToMinutes } from "@/shared/utils/duration";
import { monoFontFamily } from "@/app/theme";
import { DeliveryMode } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";

export interface AdminSessionCardProps {
  session: SessionDto;
  onOpen: (session: SessionDto) => void;
}

/**
 * Marketplace Oversight's own session card — same clickable-content /
 * separate-actions split as the Student Workspace's `SessionCard` and the
 * Tutor Workspace's `TutorSessionCard`, but showing both `tutorId` and
 * `studentId`: an Admin inspecting every Session across the platform
 * needs both parties, unlike either single-sided workspace view. That
 * genuinely admin-specific responsibility is why this is its own
 * component rather than a third prop bolted onto one of the existing two.
 */
export function AdminSessionCard({ session, onOpen }: AdminSessionCardProps) {
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
      <CardActions sx={{ px: 2, pb: 2, pt: 0 }}>
        <SessionActions session={session} />
      </CardActions>
    </Card>
  );
}

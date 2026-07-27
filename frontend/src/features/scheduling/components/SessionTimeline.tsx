import { Box, Stack, Typography } from "@mui/material";
import ScheduleRoundedIcon from "@mui/icons-material/ScheduleRounded";
import EventAvailableRoundedIcon from "@mui/icons-material/EventAvailableRounded";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import type { SessionDto } from "@/services/api/dtos";

/**
 * A visual start -> end span for the Session's own `scheduledTimeUtc` /
 * `endTimeUtc` — the only two points in time `SessionDto` actually has.
 * There is no "booked at" / "confirmed at" timestamp on this DTO, so this
 * is a time-span visualization, not a fabricated multi-event history log.
 */
export function SessionTimeline({ session }: { session: SessionDto }) {
  return (
    <Stack direction="row" spacing={2} alignItems="center">
      <Stack alignItems="center" spacing={0.5} flexShrink={0}>
        <ScheduleRoundedIcon color="primary" aria-hidden="true" />
        <Typography variant="caption" color="text.secondary">
          Start
        </Typography>
        <Typography variant="body2" fontWeight={600} textAlign="center">
          {toTehranDisplay(session.scheduledTimeUtc)}
        </Typography>
      </Stack>
      <Box flex={1} minWidth={32} sx={{ height: 2, bgcolor: "divider", borderRadius: 1 }} aria-hidden="true" />
      <Stack alignItems="center" spacing={0.5} flexShrink={0}>
        <EventAvailableRoundedIcon color="action" aria-hidden="true" />
        <Typography variant="caption" color="text.secondary">
          End
        </Typography>
        <Typography variant="body2" fontWeight={600} textAlign="center">
          {toTehranDisplay(session.endTimeUtc)}
        </Typography>
      </Stack>
    </Stack>
  );
}

import { Link as RouterLink } from "react-router-dom";
import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { timeSpanToMinutes } from "@/shared/utils/duration";
import { DeliveryMode } from "@/services/api/dtos";
import type { AvailabilitySlotDto } from "@/services/api/dtos";
import { paths } from "@/routes/paths";

export interface AvailabilitySummaryCardProps {
  slot: AvailabilitySlotDto;
}

/**
 * A read-only status card for one of the Tutor's own Availability Slots
 * (`GET /tutors/{id}/availability-slots`, the same capability
 * `AvailabilityCard`/`RescheduleSessionForm` already reuse) — deliberately
 * not the Booking flow's selectable `AvailabilityCard`: this one has no
 * "select" concept at all, just an Open/Booked status and a link to the
 * slot's own existing detail page, so it gets its own small component
 * rather than a duplicate.
 */
export function AvailabilitySummaryCard({ slot }: AvailabilitySummaryCardProps) {
  return (
    <Card variant="outlined">
      <CardContent>
        <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={1}>
          <Typography variant="subtitle1" fontWeight={600}>
            {toTehranDisplay(slot.startTimeUtc)}
          </Typography>
          <StatusPill label={slot.isConsumed ? "Booked" : "Open"} tone={slot.isConsumed ? "neutral" : "success"} />
        </Stack>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          {timeSpanToMinutes(slot.duration)} min ·{" "}
          {slot.deliveryMode === DeliveryMode.Online ? "Online" : "In-Person"}
        </Typography>
        <Button
          component={RouterLink}
          to={paths.scheduling.availabilitySlotDetail(slot.availabilitySlotId)}
          size="small"
          variant="outlined"
        >
          View details
        </Button>
      </CardContent>
    </Card>
  );
}

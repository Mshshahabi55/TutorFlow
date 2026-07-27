import { ButtonBase, Stack, Typography } from "@mui/material";
import EventAvailableRoundedIcon from "@mui/icons-material/EventAvailableRounded";
import CheckCircleRoundedIcon from "@mui/icons-material/CheckCircleRounded";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { timeSpanToMinutes } from "@/shared/utils/duration";
import { DeliveryMode } from "@/services/api/dtos";
import type { AvailabilitySlotDto } from "@/services/api/dtos";

export interface AvailabilityCardProps {
  slot: AvailabilitySlotDto;
  selected: boolean;
  onSelect: (slot: AvailabilitySlotDto) => void;
}

/**
 * One of the Tutor's own open Availability Slots (`GET
 * /tutors/{id}/availability-slots`, the same capability
 * `RescheduleSessionForm` on `SessionDetailPage` already reuses), rendered
 * as a selectable card instead of a `<select>` option — same underlying
 * data and the same eventual `availabilitySlotId` form value, just a
 * visual pick instead of typing a raw id.
 */
export function AvailabilityCard({ slot, selected, onSelect }: AvailabilityCardProps) {
  return (
    <ButtonBase
      type="button"
      onClick={() => onSelect(slot)}
      aria-pressed={selected}
      sx={{
        display: "block",
        textAlign: "left",
        borderRadius: 1,
        border: "1px solid",
        borderColor: selected ? "primary.main" : "divider",
        bgcolor: selected ? "action.selected" : "background.paper",
        p: 2,
        flex: "1 1 220px",
        minWidth: 220,
        maxWidth: 280,
        transition: (t) => t.transitions.create(["border-color", "background-color"], { duration: 150 }),
        "&:hover": {
          borderColor: selected ? "primary.main" : "primary.light",
          bgcolor: selected ? "action.selected" : "action.hover",
        },
      }}
    >
      <Stack spacing={1} width="100%">
        <Stack direction="row" alignItems="center" justifyContent="space-between">
          <EventAvailableRoundedIcon color={selected ? "primary" : "action"} aria-hidden="true" />
          {selected ? (
            <CheckCircleRoundedIcon color="primary" fontSize="small" titleAccess="Selected" />
          ) : null}
        </Stack>
        <Typography variant="subtitle1" fontWeight={600}>
          {toTehranDisplay(slot.startTimeUtc)}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {timeSpanToMinutes(slot.duration)} min ·{" "}
          {slot.deliveryMode === DeliveryMode.Online ? "Online" : "In-Person"}
        </Typography>
      </Stack>
    </ButtonBase>
  );
}

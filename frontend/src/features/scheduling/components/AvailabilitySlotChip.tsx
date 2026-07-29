import { Chip } from "@mui/material";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { timeSpanToMinutes } from "@/shared/utils/duration";
import { classifyAvailabilitySlot } from "@/features/scheduling/utils/availabilitySlotState";
import type { AvailabilitySlotDto, SessionDto } from "@/services/api/dtos";

export interface AvailabilitySlotChipProps {
  slot: AvailabilitySlotDto;
  /** The Session this slot produced, if it's booked and that Session is known — resolved by the caller from its own already-fetched schedule data. */
  session?: SessionDto;
  onOpenSession: (session: SessionDto) => void;
  nowMs: number;
}

/**
 * One Availability Slot's colour-coded chip — extracted from
 * `WeeklyAvailabilityCalendar` so `TutorAvailabilityCalendar`'s "daily slot
 * list" panel (Phase 8a) doesn't duplicate the same classify-and-render
 * logic. Colour states come straight from data already on
 * `AvailabilitySlotDto` (`isConsumed`) and the current instant — there is
 * no "Unavailable" state to render, the domain has no such concept. Only a
 * Booked slot is clickable (linking to its Session) — there is no cancel/
 * delete-availability capability in this API, so an open or expired slot
 * has no action to offer beyond being seen.
 */
export function AvailabilitySlotChip({ slot, session, onOpenSession, nowMs }: AvailabilitySlotChipProps) {
  const state = classifyAvailabilitySlot(slot, nowMs);
  const label = `${toTehranDisplay(slot.startTimeUtc).split(", ").pop()} · ${timeSpanToMinutes(slot.duration)}min`;

  return (
    <Chip
      label={label}
      size="small"
      clickable={state === "booked" && Boolean(session)}
      onClick={state === "booked" && session ? () => onOpenSession(session) : undefined}
      color={state === "booked" ? "primary" : state === "available" ? "success" : "default"}
      variant={state === "past" ? "outlined" : "filled"}
      sx={{
        justifyContent: "flex-start",
        opacity: state === "past" ? 0.6 : 1,
        height: "auto",
        minHeight: 32,
        "& .MuiChip-label": { whiteSpace: "normal", py: 0.5 },
      }}
    />
  );
}

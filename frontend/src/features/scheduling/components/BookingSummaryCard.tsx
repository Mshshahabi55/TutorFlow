import { Box, Stack, Typography } from "@mui/material";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { timeSpanToMinutes } from "@/shared/utils/duration";
import { DeliveryMode } from "@/services/api/dtos";
import type { AvailabilitySlotDto } from "@/services/api/dtos";
import { SectionCard } from "@/shared/components/SectionCard";

function SummaryRow({ label, value }: { label: string; value: string }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="body1" fontWeight={600}>
        {value}
      </Typography>
    </Box>
  );
}

/**
 * The final recap before the primary action — every field here is read
 * straight off the already-fetched `AvailabilitySlotDto`, no derived total
 * price is computed (an hourly rate × a slot duration is a calculation
 * this app does not perform anywhere else, and this phase must not invent
 * pricing logic). Phase 5 (Booking Experience): no longer repeats the
 * Tutor's own identity/subject/hourly rate — `BookSessionPage` now keeps
 * `TutorSummaryCard` visible on every step (the "Who?"/"How much?" answer
 * already reused across the whole wizard, per this phase's own "avoid
 * repeating prices"/"avoid showing the same tutor information repeatedly"
 * instruction), so this card focuses only on the booking-specific facts
 * that card doesn't already show: When, and Online/In-Person.
 */
export function BookingSummaryCard({ slot }: { slot: AvailabilitySlotDto }) {
  return (
    <SectionCard title="Booking Summary">
      <Stack spacing={2}>
        <SummaryRow label="Date" value={toTehranDisplay(slot.startTimeUtc)} />
        <SummaryRow
          label="Time"
          value={`${toTehranDisplay(slot.startTimeUtc)} – ${toTehranDisplay(slot.endTimeUtc)} (Tehran)`}
        />
        <SummaryRow label="Duration" value={`${timeSpanToMinutes(slot.duration)} minutes`} />
        <SummaryRow
          label="Platform"
          value={slot.deliveryMode === DeliveryMode.Online ? "Online" : "In-Person"}
        />
      </Stack>
    </SectionCard>
  );
}

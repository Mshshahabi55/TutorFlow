import { Box, Stack, Typography } from "@mui/material";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { timeSpanToMinutes } from "@/shared/utils/duration";
import { formatToman } from "@/shared/money/rial";
import { DeliveryMode } from "@/services/api/dtos";
import type { AvailabilitySlotDto, TutorDto } from "@/services/api/dtos";
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
 * straight off the already-fetched `TutorDto`/`AvailabilitySlotDto`, no
 * derived total price is computed (an hourly rate × a slot duration is a
 * calculation this app does not perform anywhere else, and this phase must
 * not invent pricing logic).
 */
export function BookingSummaryCard({ tutor, slot }: { tutor: TutorDto; slot: AvailabilitySlotDto }) {
  return (
    <SectionCard title="Booking Summary">
      <Stack spacing={2}>
        <SummaryRow label="Tutor" value={tutor.subject ?? "Tutor"} />
        {tutor.subject ? <SummaryRow label="Subject" value={tutor.subject} /> : null}
        <SummaryRow label="Date" value={toTehranDisplay(slot.startTimeUtc)} />
        <SummaryRow
          label="Time"
          value={`${toTehranDisplay(slot.startTimeUtc)} – ${toTehranDisplay(slot.endTimeUtc)} (Tehran)`}
        />
        <SummaryRow label="Duration" value={`${timeSpanToMinutes(slot.duration)} minutes`} />
        <SummaryRow
          label="Price"
          value={tutor.hourlyRate !== null ? `${formatToman(tutor.hourlyRate)} Toman/hr` : "Not set"}
        />
        <SummaryRow
          label="Platform"
          value={slot.deliveryMode === DeliveryMode.Online ? "Online" : "In-Person"}
        />
      </Stack>
    </SectionCard>
  );
}

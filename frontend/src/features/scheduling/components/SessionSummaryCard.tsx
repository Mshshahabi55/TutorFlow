import { Box, Stack, Typography } from "@mui/material";
import type { ReactNode } from "react";
import { SessionStatusBadge } from "@/features/scheduling/components/SessionStatusBadge";
import { SectionCard } from "@/shared/components/SectionCard";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { timeSpanToMinutes } from "@/shared/utils/duration";
import { DeliveryMode } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";

function SummaryRow({ label, value }: { label: string; value: ReactNode }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary" display="block">
        {label}
      </Typography>
      <Typography variant="body1" fontWeight={600}>
        {value}
      </Typography>
    </Box>
  );
}

/** An at-a-glance recap of an already-booked Session — reuses the same shared `SectionCard` container `BookSessionPage`'s `BookingSummaryCard` uses, not a duplicate. */
export function SessionSummaryCard({ session }: { session: SessionDto }) {
  return (
    <SectionCard title="Session Summary">
      <Stack spacing={2}>
        <SummaryRow label="Status" value={<SessionStatusBadge status={session.status} />} />
        <SummaryRow label="Date" value={toTehranDisplay(session.scheduledTimeUtc)} />
        <SummaryRow label="Duration" value={`${timeSpanToMinutes(session.duration)} minutes`} />
        <SummaryRow
          label="Platform"
          value={session.deliveryMode === DeliveryMode.Online ? "Online" : "In-Person"}
        />
      </Stack>
    </SectionCard>
  );
}

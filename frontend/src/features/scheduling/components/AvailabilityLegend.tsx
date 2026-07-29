import { Box, Stack, Typography } from "@mui/material";

function LegendItem({ color, label }: { color: string; label: string }) {
  return (
    <Stack direction="row" spacing={0.75} alignItems="center">
      <Box sx={{ width: 10, height: 10, borderRadius: "50%", bgcolor: color }} />
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
    </Stack>
  );
}

/** The colour-state key for `AvailabilitySlotChip` — shared by every calendar that renders one, so the same three states are always explained the same way. */
export function AvailabilityLegend() {
  return (
    <Stack direction="row" spacing={2} flexWrap="wrap">
      <LegendItem color="success.main" label="Available" />
      <LegendItem color="primary.main" label="Booked" />
      <LegendItem color="text.disabled" label="Past (unbooked)" />
    </Stack>
  );
}

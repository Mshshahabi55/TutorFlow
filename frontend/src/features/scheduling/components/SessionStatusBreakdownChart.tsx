import { Box, Stack, Typography, useTheme } from "@mui/material";
import type { StatusCounts } from "@/features/scheduling/utils/sessionStatusCounts";

interface StatusBar {
  label: string;
  count: number;
  colorKey: "info" | "success" | "error" | "warning";
}

/**
 * A magnitude-by-category job (docs dataviz skill: "choosing a form") — a
 * plain horizontal bar per Session status, not a pie (four categories with
 * one dominant "Scheduled" bucket read poorly as arc angles) and never a
 * second axis alongside it. Colors reuse the app's own already-shipped
 * status palette (`StatusPill`'s tone→MUI-color mapping) rather than a new
 * one — the same colors a Student/Tutor already associates with these
 * exact statuses elsewhere in the product, not a fresh palette needing its
 * own validation pass. Every bar keeps its own visible numeric label
 * (never color-only identity), so this is its own accessible "table" —
 * no separate hidden data table needed for four rows this small.
 */
export function SessionStatusBreakdownChart({ counts }: { counts: StatusCounts }) {
  const theme = useTheme();

  const bars: StatusBar[] = [
    { label: "Upcoming", count: counts.scheduled, colorKey: "info" },
    { label: "Completed", count: counts.completed, colorKey: "success" },
    { label: "Cancelled", count: counts.cancelled, colorKey: "error" },
    { label: "No-Show", count: counts.noShow, colorKey: "warning" },
  ];

  const maxCount = Math.max(1, ...bars.map((b) => b.count));

  return (
    <Stack spacing={1.5} aria-label="Session status breakdown">
      {bars.map((bar) => (
        <Stack key={bar.label} direction="row" spacing={1.5} alignItems="center">
          <Typography variant="body2" color="text.secondary" sx={{ width: 96, flexShrink: 0 }}>
            {bar.label}
          </Typography>
          <Box sx={{ flex: 1, bgcolor: "action.hover", borderRadius: 999, height: 10, overflow: "hidden" }}>
            <Box
              sx={{
                width: `${(bar.count / maxCount) * 100}%`,
                height: "100%",
                bgcolor: theme.palette[bar.colorKey].main,
                borderRadius: 999,
                transition: (t) => t.transitions.create("width"),
                "@media (prefers-reduced-motion: reduce)": { transition: "none" },
              }}
            />
          </Box>
          <Typography variant="body2" fontWeight={700} sx={{ width: 28, flexShrink: 0, textAlign: "right" }}>
            {bar.count}
          </Typography>
        </Stack>
      ))}
    </Stack>
  );
}

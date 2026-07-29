import { useState } from "react";
import { Box, Stack, Typography } from "@mui/material";
import { MonthCalendarGrid } from "@/features/scheduling/components/MonthCalendarGrid";
import { addMonths, monthKeyFromDateKey, todayMonthKey } from "@/features/scheduling/utils/monthCalendar";
import { tehranDateKey, tehranDateLabel } from "@/shared/time/tehranTime";
import type { AvailabilitySlotDto } from "@/services/api/dtos";

export interface AvailabilityPreviewCalendarProps {
  slots: AvailabilitySlotDto[];
  now?: Date;
}

/**
 * A read-only month-calendar preview of a Tutor's availability, for a
 * Student deciding whether to book (Phase 9) — reuses Phase 8a's
 * domain-agnostic `MonthCalendarGrid` exactly as it was designed to be
 * reused ("reusable... without modification"). Day cells are purely
 * informational: no click handler, no slot selection, no navigation into
 * the booking wizard — that experience stays entirely inside
 * `BookSessionPage`'s own calendar, not duplicated here (this page's
 * existing "View full schedule" button is the one path into booking).
 * Unlike the Tutor-facing `TutorAvailabilityCalendar`, this only
 * distinguishes "has openings" from "no openings" — a Student preview has
 * no reason to see "Booked" vs. "Past" as separate states the way the
 * Tutor's own schedule-management view does.
 *
 * Defaults to the month of the Tutor's *earliest* open slot, not always
 * today's calendar month — the same fix `BookSessionPage`'s Choose Date
 * step needed in Phase 8a: a Tutor whose next opening is next month would
 * otherwise show an empty calendar by default. `null` until the Student
 * explicitly navigates, at which point their choice takes over.
 */
export function AvailabilityPreviewCalendar({ slots, now = new Date() }: AvailabilityPreviewCalendarProps) {
  const [monthKeyOverride, setMonthKeyOverride] = useState<string | null>(null);

  const openDateKeys = new Set<string>();
  for (const slot of slots) {
    if (!slot.isConsumed) {
      openDateKeys.add(tehranDateKey(slot.startTimeUtc));
    }
  }
  const earliestOpenDateKey = [...openDateKeys].sort()[0];
  const monthKey = monthKeyOverride ?? (earliestOpenDateKey ? monthKeyFromDateKey(earliestOpenDateKey) : todayMonthKey(now));

  return (
    <Stack spacing={1.5}>
      <MonthCalendarGrid
        monthKey={monthKey}
        onNavigateMonth={(direction) => setMonthKeyOverride(addMonths(monthKey, direction))}
        renderDay={(dateKey, meta) => {
          const hasOpening = openDateKeys.has(dateKey) && !meta.isPast;
          const dayNumber = Number(dateKey.slice(8, 10));

          return (
            <Box
              aria-label={`${tehranDateLabel(dateKey)} — ${hasOpening ? "available" : "no availability"}`}
              sx={{
                width: "100%",
                minHeight: 40,
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                justifyContent: "center",
                gap: 0.25,
                borderRadius: 1,
                opacity: meta.isCurrentMonth ? 1 : 0.35,
              }}
            >
              <Typography variant="body2" fontWeight={meta.isToday ? 700 : 400}>
                {dayNumber}
              </Typography>
              {hasOpening ? (
                <Box aria-hidden="true" sx={{ width: 6, height: 6, borderRadius: "50%", bgcolor: "success.main" }} />
              ) : null}
            </Box>
          );
        }}
      />
      <Stack direction="row" spacing={0.75} alignItems="center">
        <Box aria-hidden="true" sx={{ width: 8, height: 8, borderRadius: "50%", bgcolor: "success.main" }} />
        <Typography variant="caption" color="text.secondary">
          Has open teaching times
        </Typography>
      </Stack>
    </Stack>
  );
}

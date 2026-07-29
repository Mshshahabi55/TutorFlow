import type { ReactNode } from "react";
import { Box, IconButton, Stack, Typography } from "@mui/material";
import ChevronLeftRoundedIcon from "@mui/icons-material/ChevronLeftRounded";
import ChevronRightRoundedIcon from "@mui/icons-material/ChevronRightRounded";
import { buildMonthMatrix, monthLabel, todayDateKey, WEEKDAY_LABELS } from "@/features/scheduling/utils/monthCalendar";

export interface MonthCalendarDayMeta {
  isCurrentMonth: boolean;
  isPast: boolean;
  isToday: boolean;
}

export interface MonthCalendarGridProps {
  /** "YYYY-MM" — the month currently shown. */
  monthKey: string;
  onNavigateMonth: (direction: -1 | 1) => void;
  /** Renders one day cell's content. `MonthCalendarGrid` has no opinion on whether a cell is a button, plain text, or anything else — that's entirely up to the caller. */
  renderDay: (dateKey: string, meta: MonthCalendarDayMeta) => ReactNode;
}

/**
 * A presentation-only month grid: it lays out a month's weeks and delegates
 * every cell's content and every domain decision (what a day means, whether
 * it's selectable, what clicking it does) to `renderDay`. Deliberately
 * knows nothing about Tutor/Student/Session/AvailabilitySlot/booking or any
 * scheduling rule — that separation is what keeps this reusable, unchanged,
 * for any future calendar UI (Admin views, session history, etc.), not just
 * the two Scheduling features that use it today.
 */
export function MonthCalendarGrid({ monthKey, onNavigateMonth, renderDay }: MonthCalendarGridProps) {
  const weeks = buildMonthMatrix(monthKey);
  const today = todayDateKey();

  return (
    <Stack component="section" aria-label={monthLabel(monthKey)} spacing={1.5}>
      <Stack direction="row" alignItems="center" justifyContent="space-between">
        <IconButton aria-label="Previous month" onClick={() => onNavigateMonth(-1)}>
          <ChevronLeftRoundedIcon />
        </IconButton>
        <Typography variant="subtitle1" fontWeight={700} component="h3">
          {monthLabel(monthKey)}
        </Typography>
        <IconButton aria-label="Next month" onClick={() => onNavigateMonth(1)}>
          <ChevronRightRoundedIcon />
        </IconButton>
      </Stack>

      <Stack direction="row">
        {WEEKDAY_LABELS.map((label) => (
          <Box key={label} flex={1} textAlign="center" aria-hidden="true">
            <Typography variant="caption" color="text.secondary">
              {label}
            </Typography>
          </Box>
        ))}
      </Stack>

      <Stack spacing={0.5}>
        {weeks.map((week) => (
          <Stack key={week[0].dateKey} direction="row" spacing={0.5}>
            {week.map((day) => (
              <Box key={day.dateKey} flex={1} minWidth={0}>
                {renderDay(day.dateKey, {
                  isCurrentMonth: day.isCurrentMonth,
                  isPast: day.dateKey < today,
                  isToday: day.dateKey === today,
                })}
              </Box>
            ))}
          </Stack>
        ))}
      </Stack>
    </Stack>
  );
}

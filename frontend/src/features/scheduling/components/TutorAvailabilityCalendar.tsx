import { useState } from "react";
import { Box, Button, Stack, Typography } from "@mui/material";
import AddRoundedIcon from "@mui/icons-material/AddRounded";
import { MonthCalendarGrid } from "@/features/scheduling/components/MonthCalendarGrid";
import { AvailabilitySlotChip } from "@/features/scheduling/components/AvailabilitySlotChip";
import { AvailabilityLegend } from "@/features/scheduling/components/AvailabilityLegend";
import { tehranDateKey, tehranDateLabel } from "@/shared/time/tehranTime";
import { addMonths, todayDateKey, todayMonthKey } from "@/features/scheduling/utils/monthCalendar";
import type { AvailabilitySlotDto, SessionDto } from "@/services/api/dtos";

export interface TutorAvailabilityCalendarProps {
  slots: AvailabilitySlotDto[];
  /** The Tutor's own schedule — used only to resolve which Session a Booked slot belongs to, so it can link there. */
  sessions: SessionDto[];
  onOpenSession: (session: SessionDto) => void;
  /** Opens the existing "Add Teaching Time" dialog prefilled for this day. */
  onAddTeachingTime: (dateKey: string) => void;
  now?: Date;
}

/**
 * A month-at-a-glance view of the Tutor's own teaching time (Phase 8a),
 * alongside `WeeklyAvailabilityCalendar`'s existing rolling 7-day strip —
 * both stay, per this phase's own constraint, since the strip is also used
 * by the Tutor Onboarding Wizard's Availability step. Built on the
 * domain-agnostic `MonthCalendarGrid`: this component supplies every
 * scheduling-specific decision (what a day cell shows, what's selectable)
 * through `renderDay`, `MonthCalendarGrid` itself knows nothing about
 * Availability Slots or Sessions. The month grid doubles as the "preview"
 * of what's published — it renders exactly the declared data, nothing more.
 */
export function TutorAvailabilityCalendar({
  slots,
  sessions,
  onOpenSession,
  onAddTeachingTime,
  now = new Date(),
}: TutorAvailabilityCalendarProps) {
  const nowMs = now.getTime();
  const [monthKey, setMonthKey] = useState(() => todayMonthKey(now));
  const [selectedDateKey, setSelectedDateKey] = useState(() => todayDateKey(now));

  const sessionBySlotId = new Map(sessions.map((session) => [session.availabilitySlotId, session]));

  const slotsByDate = new Map<string, AvailabilitySlotDto[]>();
  for (const slot of slots) {
    const key = tehranDateKey(slot.startTimeUtc);
    const existing = slotsByDate.get(key) ?? [];
    existing.push(slot);
    slotsByDate.set(key, existing);
  }

  const selectedDaySlots = (slotsByDate.get(selectedDateKey) ?? []).sort(
    (a, b) => Date.parse(a.startTimeUtc) - Date.parse(b.startTimeUtc),
  );

  return (
    <Stack spacing={3}>
      <AvailabilityLegend />

      <MonthCalendarGrid
        monthKey={monthKey}
        onNavigateMonth={(direction) => setMonthKey((current) => addMonths(current, direction))}
        renderDay={(dateKey, meta) => {
          const daySlots = slotsByDate.get(dateKey) ?? [];
          const isSelected = dateKey === selectedDateKey;
          const dayNumber = Number(dateKey.slice(8, 10));

          return (
            <Box
              component="button"
              type="button"
              onClick={() => setSelectedDateKey(dateKey)}
              aria-current={meta.isToday ? "date" : undefined}
              aria-pressed={isSelected}
              aria-label={
                daySlots.length > 0
                  ? `${tehranDateLabel(dateKey)} — ${daySlots.length} teaching ${daySlots.length === 1 ? "time" : "times"}`
                  : `${tehranDateLabel(dateKey)} — no teaching time`
              }
              sx={{
                width: "100%",
                minHeight: 44,
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                justifyContent: "center",
                gap: 0.25,
                border: "1px solid",
                borderColor: isSelected ? "primary.main" : "divider",
                borderRadius: 1,
                bgcolor: isSelected ? "action.selected" : "background.paper",
                opacity: meta.isCurrentMonth ? 1 : 0.4,
                cursor: "pointer",
                font: "inherit",
                color: "inherit",
                "&:focus-visible": { outline: "2px solid", outlineColor: "primary.main", outlineOffset: 2 },
              }}
            >
              <Typography variant="body2" fontWeight={meta.isToday ? 700 : 400}>
                {dayNumber}
              </Typography>
              {daySlots.length > 0 ? (
                <Box aria-hidden="true" sx={{ width: 6, height: 6, borderRadius: "50%", bgcolor: "primary.main" }} />
              ) : null}
            </Box>
          );
        }}
      />

      <Stack spacing={1.5} aria-live="polite">
        <Typography variant="subtitle2" fontWeight={600}>
          {tehranDateLabel(selectedDateKey)}
        </Typography>
        {selectedDaySlots.length === 0 ? (
          <Stack spacing={1} alignItems="flex-start">
            <Typography variant="body2" color="text.secondary">
              No teaching time on this day.
            </Typography>
            <Button
              size="small"
              variant="outlined"
              startIcon={<AddRoundedIcon />}
              onClick={() => onAddTeachingTime(selectedDateKey)}
            >
              Add teaching time
            </Button>
          </Stack>
        ) : (
          <Stack spacing={1}>
            {selectedDaySlots.map((slot) => (
              <AvailabilitySlotChip
                key={slot.availabilitySlotId}
                slot={slot}
                session={sessionBySlotId.get(slot.availabilitySlotId)}
                onOpenSession={onOpenSession}
                nowMs={nowMs}
              />
            ))}
          </Stack>
        )}
      </Stack>
    </Stack>
  );
}

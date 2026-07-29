import { Box, Stack, Typography } from "@mui/material";
import { tehranDateKey, tehranDateLabel } from "@/shared/time/tehranTime";
import { AvailabilitySlotChip } from "@/features/scheduling/components/AvailabilitySlotChip";
import { AvailabilityLegend } from "@/features/scheduling/components/AvailabilityLegend";
import type { AvailabilitySlotDto, SessionDto } from "@/services/api/dtos";

const DAYS_SHOWN = 7;

/** The next `DAYS_SHOWN` Tehran-local calendar dates, starting today — the calendar's week window. */
function upcomingDateKeys(now: Date): string[] {
  return Array.from({ length: DAYS_SHOWN }, (_, index) =>
    tehranDateKey(new Date(now.getTime() + index * 86_400_000).toISOString()),
  );
}

export interface WeeklyAvailabilityCalendarProps {
  slots: AvailabilitySlotDto[];
  /** The Tutor's own schedule — used only to resolve which Session a Booked slot belongs to, so it can link there. */
  sessions: SessionDto[];
  onOpenSession: (session: SessionDto) => void;
  now?: Date;
}

/**
 * A week-at-a-glance view of the Tutor's own teaching time — RC2.2's
 * "calendar first" spec. Colour states come straight from data already on
 * `AvailabilitySlotDto` (`isConsumed`) and the current instant — there is
 * no "Unavailable" state to render: the domain has no such concept, an
 * empty day column already communicates it honestly. Only a Booked slot
 * is clickable (linking to its Session, resolved from the same schedule
 * data already fetched) — there is no cancel/delete-availability
 * capability in this API, so an open or expired slot has no action to
 * offer beyond being seen.
 */
export function WeeklyAvailabilityCalendar({
  slots,
  sessions,
  onOpenSession,
  now = new Date(),
}: WeeklyAvailabilityCalendarProps) {
  const nowMs = now.getTime();
  const todayKey = tehranDateKey(now.toISOString());
  const dateKeys = upcomingDateKeys(now);

  const sessionBySlotId = new Map(sessions.map((session) => [session.availabilitySlotId, session]));

  const slotsByDate = new Map<string, AvailabilitySlotDto[]>();
  for (const slot of slots) {
    const key = tehranDateKey(slot.startTimeUtc);
    const existing = slotsByDate.get(key) ?? [];
    existing.push(slot);
    slotsByDate.set(key, existing);
  }

  return (
    <Stack spacing={2}>
      <AvailabilityLegend />

      <Stack direction="row" spacing={2} sx={{ overflowX: "auto", pb: 1 }}>
      {dateKeys.map((dateKey) => {
        const isToday = dateKey === todayKey;
        const daySlots = (slotsByDate.get(dateKey) ?? []).sort(
          (a, b) => Date.parse(a.startTimeUtc) - Date.parse(b.startTimeUtc),
        );

        return (
          <Box
            key={dateKey}
            flex="1 1 160px"
            minWidth={160}
            sx={{
              borderRadius: 1,
              border: "1px solid",
              borderColor: isToday ? "primary.main" : "divider",
              bgcolor: isToday ? "action.hover" : "background.paper",
              p: 1.5,
            }}
          >
            <Typography variant="subtitle2" fontWeight={600} gutterBottom>
              {tehranDateLabel(dateKey)}
              {isToday ? " · Today" : ""}
            </Typography>

            {daySlots.length === 0 ? (
              <Typography variant="caption" color="text.secondary">
                No teaching time
              </Typography>
            ) : (
              <Stack spacing={1} mt={1}>
                {daySlots.map((slot) => (
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
          </Box>
        );
      })}
      </Stack>
    </Stack>
  );
}

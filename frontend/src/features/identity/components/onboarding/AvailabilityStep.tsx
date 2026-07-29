import { useState } from "react";
import { Box, Button, Stack, Typography, alpha } from "@mui/material";
import AddRoundedIcon from "@mui/icons-material/AddRounded";
import EventAvailableRoundedIcon from "@mui/icons-material/EventAvailableRounded";
import { useTutorSchedule } from "@/features/scheduling/hooks/useSessionQueries";
import { useTutorAvailabilitySlots } from "@/features/scheduling/hooks/useAvailabilitySlotQueries";
import { WeeklyAvailabilityCalendar } from "@/features/scheduling/components/WeeklyAvailabilityCalendar";
import { AddTeachingTimeDialog } from "@/features/scheduling/components/AddTeachingTimeDialog";
import { LoadingState } from "@/shared/components/feedback/LoadingState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";

/**
 * Step 5 — Availability. Reuses `WeeklyAvailabilityCalendar` +
 * `AddTeachingTimeDialog` verbatim — the exact same components and hooks
 * `DeclareAvailabilityPage` already uses (ADR-024's own Frontend
 * Implications section) — no new availability capability, no new booking
 * logic, Tehran-time throughout, same as everywhere else in the app.
 */
export function AvailabilityStep({ tutorId }: { tutorId: string }) {
  const [dialogOpen, setDialogOpen] = useState(false);
  const slotsQuery = useTutorAvailabilitySlots(tutorId);
  const scheduleQuery = useTutorSchedule(tutorId);

  return (
    <Stack spacing={2.5}>
      <Typography variant="body2" color="text.secondary">
        Add the times you&rsquo;re available to teach. All times are shown in Tehran local time.
      </Typography>
      <Button
        type="button"
        variant="outlined"
        startIcon={<AddRoundedIcon />}
        onClick={() => setDialogOpen(true)}
        sx={{ alignSelf: "flex-start" }}
      >
        Add teaching time
      </Button>

      {slotsQuery.isPending || scheduleQuery.isPending ? <LoadingState label="Loading your schedule…" /> : null}
      {slotsQuery.isError ? <ErrorState error={slotsQuery.error} onRetry={() => void slotsQuery.refetch()} /> : null}
      {scheduleQuery.isError ? <ErrorState error={scheduleQuery.error} onRetry={() => void scheduleQuery.refetch()} /> : null}
      {slotsQuery.isSuccess && slotsQuery.data.length === 0 ? (
        <Stack
          direction="row"
          spacing={1.5}
          alignItems="flex-start"
          sx={{
            p: 2,
            borderRadius: 1,
            bgcolor: (t) => alpha(t.palette.primary.main, t.palette.mode === "dark" ? 0.12 : 0.06),
          }}
        >
          <EventAvailableRoundedIcon color="primary" fontSize="small" aria-hidden="true" sx={{ mt: 0.25 }} />
          <Box>
            <Typography variant="body2" fontWeight={600}>
              No availability yet
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Students can only book a lesson once you&rsquo;ve added at least one teaching time above.
            </Typography>
          </Box>
        </Stack>
      ) : null}
      {slotsQuery.isSuccess && scheduleQuery.isSuccess ? (
        <WeeklyAvailabilityCalendar
          slots={slotsQuery.data}
          sessions={scheduleQuery.data}
          onOpenSession={() => undefined}
        />
      ) : null}

      <AddTeachingTimeDialog tutorId={tutorId} open={dialogOpen} onClose={() => setDialogOpen(false)} />
    </Stack>
  );
}

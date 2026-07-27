import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button, Stack, Typography } from "@mui/material";
import AddRoundedIcon from "@mui/icons-material/AddRounded";
import EventAvailableRoundedIcon from "@mui/icons-material/EventAvailableRounded";
import { useTutorSchedule } from "@/features/scheduling/hooks/useSessionQueries";
import { useTutorAvailabilitySlots } from "@/features/scheduling/hooks/useAvailabilitySlotQueries";
import { WeeklyAvailabilityCalendar } from "@/features/scheduling/components/WeeklyAvailabilityCalendar";
import { AddTeachingTimeDialog } from "@/features/scheduling/components/AddTeachingTimeDialog";
import { AvailabilitySummaryCard } from "@/features/scheduling/components/AvailabilitySummaryCard";
import { SectionCard } from "@/shared/components/SectionCard";
import { IdentityGate } from "@/shared/components/IdentityGate";
import { PageHeader } from "@/shared/components/PageHeader";
import { EmptyState } from "@/shared/components/feedback/EmptyState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { paths } from "@/routes/paths";
import type { SessionDto } from "@/services/api/dtos";

/**
 * RC2.2: "Never expose raw forms first — calendar first, click, select,
 * save." The Tutor's own teaching time is shown as a week-at-a-glance
 * calendar (`WeeklyAvailabilityCalendar`); the previous always-visible
 * declare form is now a minimal on-demand dialog
 * (`AddTeachingTimeDialog`), and booked-slot history — real, but outside
 * the calendar's forward-looking week window — is a secondary, collapsed
 * section below. Same `POST /availability-slots` mutation and
 * `GET /tutors/{id}/availability-slots` query as before.
 */
function TeachingSchedule({ tutorId }: { tutorId: string }) {
  const navigate = useNavigate();
  const slotsQuery = useTutorAvailabilitySlots(tutorId);
  const scheduleQuery = useTutorSchedule(tutorId);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [showHistory, setShowHistory] = useState(false);

  function openSession(session: SessionDto) {
    void navigate(paths.scheduling.sessionDetail(session.sessionId));
  }

  if (slotsQuery.isError) {
    return <ErrorState error={slotsQuery.error} onRetry={() => void slotsQuery.refetch()} />;
  }

  const slots = slotsQuery.data ?? [];
  const bookedSlots = slots
    .filter((slot) => slot.isConsumed)
    .sort((a, b) => Date.parse(b.startTimeUtc) - Date.parse(a.startTimeUtc));

  return (
    <Stack spacing={3}>
      <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={2}>
        <Typography variant="body2" color="text.secondary">
          Times are shown in Tehran local time.
        </Typography>
        <Button
          variant="contained"
          startIcon={<AddRoundedIcon />}
          onClick={() => setDialogOpen(true)}
        >
          Add Teaching Time
        </Button>
      </Stack>

      {slotsQuery.isPending ? (
        <SectionCard title="Teaching Schedule">
          <Typography variant="body2" color="text.secondary">
            Loading your schedule…
          </Typography>
        </SectionCard>
      ) : slots.length === 0 ? (
        <EmptyState
          title="You haven't added any teaching time"
          description="Add some open times so Students can book a lesson with you."
          action={
            <Button
              variant="contained"
              startIcon={<EventAvailableRoundedIcon />}
              onClick={() => setDialogOpen(true)}
            >
              Add Availability
            </Button>
          }
        />
      ) : (
        <SectionCard title="Teaching Schedule">
          <WeeklyAvailabilityCalendar
            slots={slots}
            sessions={scheduleQuery.data ?? []}
            onOpenSession={openSession}
          />
        </SectionCard>
      )}

      <AddTeachingTimeDialog tutorId={tutorId} open={dialogOpen} onClose={() => setDialogOpen(false)} />

      {bookedSlots.length > 0 ? (
        <SectionCard
          title="History"
          action={
            <Button size="small" onClick={() => setShowHistory((value) => !value)}>
              {showHistory ? "Hide" : "Show"}
            </Button>
          }
        >
          {showHistory ? (
            <Stack spacing={2}>
              {bookedSlots.map((slot) => (
                <AvailabilitySummaryCard key={slot.availabilitySlotId} slot={slot} />
              ))}
            </Stack>
          ) : (
            <Typography variant="body2" color="text.secondary">
              {bookedSlots.length} booked {bookedSlots.length === 1 ? "lesson" : "lessons"} on record.
            </Typography>
          )}
        </SectionCard>
      ) : null}
    </Stack>
  );
}

/** GET /tutors/{id}/availability-slots + POST /availability-slots — same capability as before, presented calendar-first. */
export function DeclareAvailabilityPage() {
  return (
    <Stack spacing={3}>
      <PageHeader
        title="Manage Your Schedule"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            See your teaching time for the week, and add more whenever you like.
          </Typography>
        }
      />

      <IdentityGate
        kind="tutor"
        fieldLabel="Tutor id"
        title="Let's set up your schedule"
        description="Enter your tutor id once — we'll remember it on this device so you won't need to again."
      >
        {(tutorId) => <TeachingSchedule tutorId={tutorId} />}
      </IdentityGate>
    </Stack>
  );
}

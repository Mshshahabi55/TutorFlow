import { useNavigate, useParams } from "react-router-dom";
import { Button, Stack, Typography } from "@mui/material";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useSession } from "@/features/scheduling/hooks/useSessionQueries";
import { useRescheduleSession } from "@/features/scheduling/hooks/useSessionMutations";
import { useTutorAvailabilitySlots } from "@/features/scheduling/hooks/useAvailabilitySlotQueries";
import { useTutor } from "@/features/identity/hooks/useTutorQueries";
import { useStudent } from "@/features/identity/hooks/useStudentQueries";
import {
  rescheduleSessionSchema,
  type RescheduleSessionFormValues,
} from "@/features/scheduling/validation/rescheduleSessionSchema";
import { SessionActions } from "@/features/scheduling/components/SessionActions";
import { SessionTimeline } from "@/features/scheduling/components/SessionTimeline";
import { SessionSummaryCard } from "@/features/scheduling/components/SessionSummaryCard";
import { BookingSectionCard } from "@/features/scheduling/components/BookingSectionCard";
import { TutorSummaryCard } from "@/features/scheduling/components/TutorSummaryCard";
import { StudentSummaryCard } from "@/features/scheduling/components/StudentSummaryCard";
import { SessionDetailSkeleton } from "@/features/scheduling/components/SessionDetailSkeleton";
import { IdLookupForm } from "@/shared/components/forms/IdLookupForm";
import { Form } from "@/shared/components/forms/Form";
import { FormSelect } from "@/shared/components/forms/FormSelect";
import { PageHeader } from "@/shared/components/PageHeader";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { useNotification } from "@/shared/hooks/useNotification";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { timeSpanToMinutes } from "@/shared/utils/duration";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";
import { paths } from "@/routes/paths";

/**
 * Rescheduling targets one of the Tutor's own open Availability Slots
 * (Phase 4.7) rather than an arbitrary Tehran-entered time — the same
 * "cancel and rebook" mechanism as Cancel + BookSession, applied to the
 * same Session. Excludes the Session's current slot (rescheduling onto it
 * is a no-op the backend itself rejects) and any already-consumed slot.
 * Unchanged from before Phase 3 Step 5 — only its placement on the page
 * moved.
 */
function RescheduleSessionForm({
  sessionId,
  tutorId,
  currentAvailabilitySlotId,
}: {
  sessionId: string;
  tutorId: string;
  currentAvailabilitySlotId: string;
}) {
  const rescheduleSession = useRescheduleSession(sessionId);
  const slotsQuery = useTutorAvailabilitySlots(tutorId);
  const { notify } = useNotification();
  const form = useForm<RescheduleSessionFormValues>({
    resolver: zodResolver(rescheduleSessionSchema),
    defaultValues: { newAvailabilitySlotId: "" },
  });

  function handleSubmit(values: RescheduleSessionFormValues) {
    rescheduleSession.mutate(values.newAvailabilitySlotId, {
      onSuccess: () => {
        notify({ message: "Session rescheduled.", severity: "success" });
        form.reset();
      },
    });
  }

  if (slotsQuery.isPending) {
    return (
      <Typography variant="body2" color="text.secondary">
        Loading open slots…
      </Typography>
    );
  }

  if (slotsQuery.isError) {
    return <ErrorState error={slotsQuery.error} onRetry={() => void slotsQuery.refetch()} />;
  }

  const openSlotOptions = slotsQuery.data
    .filter((slot) => !slot.isConsumed && slot.availabilitySlotId !== currentAvailabilitySlotId)
    .map((slot) => ({
      value: slot.availabilitySlotId,
      label: `${toTehranDisplay(slot.startTimeUtc)} · ${timeSpanToMinutes(slot.duration)} min · ${
        slot.deliveryMode === DeliveryMode.Online ? "Online" : "In-Person"
      }`,
    }));

  if (openSlotOptions.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary">
        This Tutor has no other open Availability Slots to reschedule onto.
      </Typography>
    );
  }

  return (
    <Form form={form} onSubmit={handleSubmit}>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={2} alignItems="flex-start">
        <FormSelect name="newAvailabilitySlotId" label="New Availability Slot" options={openSlotOptions} />
        <Button type="submit" variant="outlined" disabled={rescheduleSession.isPending}>
          {rescheduleSession.isPending ? "Rescheduling…" : "Reschedule"}
        </Button>
      </Stack>
    </Form>
  );
}

/**
 * Reuses `useTutor` (identity) and `useStudent` (identity) once per page
 * load — the same hooks the Tutor Profile/Directory/BookSessionPage and
 * StudentDetailPage already use — not a duplicate request.
 */
function SessionDetailContent({ session }: { session: SessionDto }) {
  const tutorQuery = useTutor(session.tutorId);
  const studentQuery = useStudent(session.studentId);

  return (
    <Stack spacing={3}>
      <BookingSectionCard title="Timeline">
        <SessionTimeline session={session} />
      </BookingSectionCard>

      {tutorQuery.isSuccess ? <TutorSummaryCard tutor={tutorQuery.data} /> : null}
      {tutorQuery.isError ? (
        <ErrorState
          error={tutorQuery.error}
          onRetry={() => void tutorQuery.refetch()}
          title="Tutor details could not be loaded"
        />
      ) : null}

      {studentQuery.isSuccess ? <StudentSummaryCard student={studentQuery.data} /> : null}
      {studentQuery.isError ? (
        <ErrorState
          error={studentQuery.error}
          onRetry={() => void studentQuery.refetch()}
          title="Student details could not be loaded"
        />
      ) : null}

      <SessionSummaryCard session={session} />

      <BookingSectionCard title="Actions">
        <Stack spacing={2} alignItems="flex-start">
          <SessionActions session={session} />
          {session.status === SessionStatus.Scheduled ? (
            <RescheduleSessionForm
              sessionId={session.sessionId}
              tutorId={session.tutorId}
              currentAvailabilitySlotId={session.availabilitySlotId}
            />
          ) : null}
        </Stack>
      </BookingSectionCard>
    </Stack>
  );
}

/**
 * Session status transitions follow the backend's own guard exactly: only a
 * Scheduled Session may be rescheduled, cancelled, completed, or marked
 * No-Show (Session.Reschedule/Cancel/Complete/MarkNoShow all throw
 * otherwise) — every action here disables itself once that's no longer
 * true. Same GET /sessions/{id} query and route as before (Phase 3 Step 5
 * is presentation-only).
 */
export function SessionDetailPage() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const navigate = useNavigate();
  const sessionQuery = useSession(sessionId);

  return (
    <Stack spacing={3} maxWidth={720}>
      <PageHeader title="Session details" />

      {!sessionId ? (
        <IdLookupForm
          label="Session id"
          onSubmit={(id) => {
            void navigate(paths.scheduling.sessionDetail(id));
          }}
        />
      ) : sessionQuery.isPending ? (
        <SessionDetailSkeleton />
      ) : sessionQuery.isError ? (
        <ErrorState error={sessionQuery.error} onRetry={() => void sessionQuery.refetch()} />
      ) : (
        <SessionDetailContent session={sessionQuery.data} />
      )}
    </Stack>
  );
}

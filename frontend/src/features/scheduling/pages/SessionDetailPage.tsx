import { Link as RouterLink, useNavigate, useParams } from "react-router-dom";
import { Button, Stack, Typography } from "@mui/material";
import EventRoundedIcon from "@mui/icons-material/EventRounded";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useSession } from "@/features/scheduling/hooks/useSessionQueries";
import { useRescheduleSession } from "@/features/scheduling/hooks/useSessionMutations";
import { useTutorAvailabilitySlots } from "@/features/scheduling/hooks/useAvailabilitySlotQueries";
import { useTutor } from "@/features/identity/hooks/useTutorQueries";
import { useStudent } from "@/features/identity/hooks/useStudentQueries";
import { useEffectiveRole } from "@/shared/hooks/useEffectiveRole";
import {
  rescheduleSessionSchema,
  type RescheduleSessionFormValues,
} from "@/features/scheduling/validation/rescheduleSessionSchema";
import { SessionActions } from "@/features/scheduling/components/SessionActions";
import { SessionTimeline } from "@/features/scheduling/components/SessionTimeline";
import { SessionSummaryCard } from "@/features/scheduling/components/SessionSummaryCard";
import { SectionCard } from "@/shared/components/SectionCard";
import { TutorSummaryCard } from "@/features/scheduling/components/TutorSummaryCard";
import { StudentSummaryCard } from "@/features/scheduling/components/StudentSummaryCard";
import { SessionDetailSkeleton } from "@/features/scheduling/components/SessionDetailSkeleton";
import { MeetingCard } from "@/features/meetings/components/MeetingCard";
import { IdLookupForm } from "@/shared/components/forms/IdLookupForm";
import { Form } from "@/shared/components/forms/Form";
import { FormSelect } from "@/shared/components/forms/FormSelect";
import { PageHeader } from "@/shared/components/PageHeader";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { UnavailableState } from "@/shared/components/feedback/UnavailableState";
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
        <Button type="submit" variant="contained" disabled={rescheduleSession.isPending}>
          {rescheduleSession.isPending ? "Rescheduling…" : "Reschedule"}
        </Button>
      </Stack>
    </Form>
  );
}

/**
 * "Book Again" — every completed lesson gets a one-click way back into the
 * existing booking wizard, pre-filled with the same Tutor
 * (`?tutorId=...`, the same query parameter `TutorCard`/`ChildSummaryCard`
 * already use). Only shown to a role the booking wizard actually accepts
 * (`router.tsx`: Student/ParentGuardian) — a Tutor or Admin viewing a
 * completed lesson has no booking capability to reuse.
 */
function BookAgainAction({ session }: { session: SessionDto }) {
  const role = useEffectiveRole();

  if (session.status !== SessionStatus.Completed || (role !== "Student" && role !== "ParentGuardian")) {
    return null;
  }

  return (
    <Button
      component={RouterLink}
      to={`${paths.scheduling.bookSession}?tutorId=${session.tutorId}`}
      variant="contained"
      startIcon={<EventRoundedIcon />}
      sx={{ alignSelf: { xs: "stretch", sm: "flex-start" } }}
    >
      Book Again
    </Button>
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
      <SectionCard title="Timeline">
        <SessionTimeline session={session} />
      </SectionCard>

      <MeetingCard session={session} />

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

      <SectionCard title="Actions">
        <Stack spacing={2} alignItems="flex-start">
          <SessionActions session={session} />
          <BookAgainAction session={session} />
          {session.status === SessionStatus.Scheduled ? (
            <RescheduleSessionForm
              sessionId={session.sessionId}
              tutorId={session.tutorId}
              currentAvailabilitySlotId={session.availabilitySlotId}
            />
          ) : null}
        </Stack>
      </SectionCard>

      <SectionCard title="Notes">
        <Typography variant="body2" color="text.secondary">
          Lesson notes are coming soon — you&rsquo;ll be able to jot down what to cover next time.
        </Typography>
      </SectionCard>

      <SectionCard title="Homework">
        <Typography variant="body2" color="text.secondary">
          Homework tracking is coming soon — you&rsquo;ll be able to see and set homework here.
        </Typography>
      </SectionCard>

      <SectionCard title="History">
        <Typography variant="body2" color="text.secondary">
          A history of changes to this lesson is coming soon.
        </Typography>
      </SectionCard>
    </Stack>
  );
}

/** Where "My lessons" goes for the current viewer — never a bare "Go Home" for a role that has a real lessons list of its own. */
function myLessonsPathFor(role: ReturnType<typeof useEffectiveRole>): string {
  if (role === "Student") {
    return paths.scheduling.studentScheduleBase;
  }
  if (role === "Tutor") {
    return paths.scheduling.tutorScheduleBase;
  }
  if (role === "AdminStaff") {
    return paths.oversight.globalSessions;
  }
  return paths.home;
}

/**
 * A friendly placeholder for "this session's id doesn't resolve" — never
 * the raw backend error text (RC2: no technical wording for a Student/
 * Tutor-facing failure). RC4.2: "Go Home" is replaced with a role-aware
 * link back to the viewer's own lessons list, since that's always a real,
 * reachable next step rather than a dead end.
 */
function SessionNotFound({ onRetry }: { onRetry: () => void }) {
  const role = useEffectiveRole();

  return (
    <UnavailableState
      title="Session unavailable"
      description="This lesson could not be found. It may have been removed, or the link might be broken."
      actions={[
        { label: "Try again", onClick: onRetry },
        { label: "My lessons", to: myLessonsPathFor(role), variant: "contained" },
      ]}
    />
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
        <SessionNotFound onRetry={() => void sessionQuery.refetch()} />
      ) : (
        <SessionDetailContent session={sessionQuery.data} />
      )}
    </Stack>
  );
}

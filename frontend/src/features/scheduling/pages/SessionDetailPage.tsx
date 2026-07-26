import { useNavigate, useParams } from "react-router-dom";
import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useSession } from "@/features/scheduling/hooks/useSessionQueries";
import { useRescheduleSession } from "@/features/scheduling/hooks/useSessionMutations";
import { useTutorAvailabilitySlots } from "@/features/scheduling/hooks/useAvailabilitySlotQueries";
import {
  rescheduleSessionSchema,
  type RescheduleSessionFormValues,
} from "@/features/scheduling/validation/rescheduleSessionSchema";
import { SessionActions } from "@/features/scheduling/components/SessionActions";
import { SESSION_STATUS_LABEL, SESSION_STATUS_TONE } from "@/features/scheduling/utils/sessionStatus";
import { IdLookupForm } from "@/shared/components/forms/IdLookupForm";
import { Form } from "@/shared/components/forms/Form";
import { FormSelect } from "@/shared/components/forms/FormSelect";
import { PageHeader } from "@/shared/components/PageHeader";
import { LoadingState } from "@/shared/components/feedback/LoadingState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { useNotification } from "@/shared/hooks/useNotification";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { timeSpanToMinutes } from "@/shared/utils/duration";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";
import { paths } from "@/routes/paths";

/**
 * Rescheduling targets one of the Tutor's own open Availability Slots
 * (Phase 4.7) rather than an arbitrary Tehran-entered time — the same
 * "cancel and rebook" mechanism as Cancel + BookSession, applied to the
 * same Session. Excludes the Session's current slot (rescheduling onto it
 * is a no-op the backend itself rejects) and any already-consumed slot.
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
    return <LoadingState label="Loading open slots…" minHeight={60} />;
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
 * Session status transitions follow the backend's own guard exactly: only a
 * Scheduled Session may be rescheduled, cancelled, completed, or marked
 * No-Show (Session.Reschedule/Cancel/Complete/MarkNoShow all throw
 * otherwise) — every action here disables itself once that's no longer true.
 */
export function SessionDetailPage() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const navigate = useNavigate();
  const sessionQuery = useSession(sessionId);

  return (
    <Stack spacing={3} maxWidth={640}>
      <PageHeader title="Session detail" />

      {!sessionId ? (
        <IdLookupForm
          label="Session id"
          onSubmit={(id) => {
            void navigate(paths.scheduling.sessionDetail(id));
          }}
        />
      ) : (
        <Card variant="outlined">
          <CardContent>
            {sessionQuery.isPending ? <LoadingState label="Loading Session…" /> : null}
            {sessionQuery.isError ? (
              <ErrorState error={sessionQuery.error} onRetry={() => void sessionQuery.refetch()} />
            ) : null}
            {sessionQuery.isSuccess ? (
              <Stack spacing={2}>
                <Stack direction="row" spacing={1} flexWrap="wrap">
                  <StatusPill
                    label={SESSION_STATUS_LABEL[sessionQuery.data.status]}
                    tone={SESSION_STATUS_TONE[sessionQuery.data.status]}
                  />
                  <StatusPill
                    label={
                      sessionQuery.data.deliveryMode === DeliveryMode.Online
                        ? "Online"
                        : "In-Person"
                    }
                    tone="neutral"
                  />
                </Stack>

                <Stack spacing={1}>
                  <Typography variant="body2">
                    <b>Tutor id:</b> {sessionQuery.data.tutorId}
                  </Typography>
                  <Typography variant="body2">
                    <b>Student id:</b> {sessionQuery.data.studentId}
                  </Typography>
                  <Typography variant="body2">
                    <b>Parent/Guardian id:</b> {sessionQuery.data.parentGuardianId ?? "None"}
                  </Typography>
                  <Typography variant="body2">
                    <b>Scheduled (Tehran):</b> {toTehranDisplay(sessionQuery.data.scheduledTimeUtc)}
                  </Typography>
                  <Typography variant="body2">
                    <b>End (Tehran):</b> {toTehranDisplay(sessionQuery.data.endTimeUtc)}
                  </Typography>
                </Stack>

                <SessionActions session={sessionQuery.data} />

                {sessionQuery.data.status === SessionStatus.Scheduled ? (
                  <RescheduleSessionForm
                    sessionId={sessionQuery.data.sessionId}
                    tutorId={sessionQuery.data.tutorId}
                    currentAvailabilitySlotId={sessionQuery.data.availabilitySlotId}
                  />
                ) : null}
              </Stack>
            ) : null}
          </CardContent>
        </Card>
      )}
    </Stack>
  );
}

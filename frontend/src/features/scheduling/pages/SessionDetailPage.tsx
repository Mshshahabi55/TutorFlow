import { useNavigate, useParams } from "react-router-dom";
import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useSession } from "@/features/scheduling/hooks/useSessionQueries";
import { useRescheduleSession } from "@/features/scheduling/hooks/useSessionMutations";
import {
  rescheduleSessionSchema,
  type RescheduleSessionFormValues,
} from "@/features/scheduling/validation/rescheduleSessionSchema";
import { SessionActions } from "@/features/scheduling/components/SessionActions";
import { SESSION_STATUS_LABEL, SESSION_STATUS_TONE } from "@/features/scheduling/utils/sessionStatus";
import { IdLookupForm } from "@/shared/components/forms/IdLookupForm";
import { Form } from "@/shared/components/forms/Form";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import { PageHeader } from "@/shared/components/PageHeader";
import { LoadingState } from "@/shared/components/feedback/LoadingState";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { StatusPill } from "@/shared/components/feedback/StatusPill";
import { useNotification } from "@/shared/hooks/useNotification";
import { fromTehranInput, toTehranDisplay } from "@/shared/time/tehranTime";
import { DeliveryMode, SessionStatus } from "@/services/api/dtos";
import { paths } from "@/routes/paths";

function RescheduleSessionForm({ sessionId }: { sessionId: string }) {
  const rescheduleSession = useRescheduleSession(sessionId);
  const { notify } = useNotification();
  const form = useForm<RescheduleSessionFormValues>({
    resolver: zodResolver(rescheduleSessionSchema),
    defaultValues: { newScheduledTimeLocal: "" },
  });

  function handleSubmit(values: RescheduleSessionFormValues) {
    rescheduleSession.mutate(fromTehranInput(values.newScheduledTimeLocal), {
      onSuccess: () => {
        notify({ message: "Session rescheduled.", severity: "success" });
        form.reset();
      },
    });
  }

  return (
    <Form form={form} onSubmit={handleSubmit}>
      <Stack direction={{ xs: "column", sm: "row" }} spacing={2} alignItems="flex-start">
        <FormTextField
          name="newScheduledTimeLocal"
          label="New start time (Tehran)"
          type="datetime-local"
          slotProps={{ inputLabel: { shrink: true } }}
        />
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
                  <RescheduleSessionForm sessionId={sessionQuery.data.sessionId} />
                ) : null}
              </Stack>
            ) : null}
          </CardContent>
        </Card>
      )}
    </Stack>
  );
}

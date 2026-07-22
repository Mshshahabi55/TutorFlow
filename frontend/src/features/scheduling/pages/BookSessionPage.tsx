import { Link as RouterLink, useSearchParams } from "react-router-dom";
import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useBookSession } from "@/features/scheduling/hooks/useSessionMutations";
import {
  bookSessionSchema,
  type BookSessionFormValues,
} from "@/features/scheduling/validation/bookSessionSchema";
import { Form } from "@/shared/components/forms/Form";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { CopyableId } from "@/shared/components/CopyableId";
import { PageHeader } from "@/shared/components/PageHeader";
import { useNotification } from "@/shared/hooks/useNotification";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { paths } from "@/routes/paths";

/**
 * POST /sessions books a Session against an already-declared Availability
 * Slot. Since no lookup-by-name or slot-browsing capability exists, the
 * Availability Slot id, Student id, and (optional) Parent/Guardian id are
 * each typed in directly, exactly like every other Sprint 6/7 identity-
 * scoped form. Arriving from AvailabilitySlotDetailPage's "Book this slot"
 * link pre-fills the slot id via a query parameter.
 */
export function BookSessionPage() {
  const [searchParams] = useSearchParams();
  const bookSession = useBookSession();
  const { notify } = useNotification();

  const form = useForm<BookSessionFormValues>({
    resolver: zodResolver(bookSessionSchema),
    defaultValues: {
      availabilitySlotId: searchParams.get("availabilitySlotId") ?? "",
      studentId: "",
      parentGuardianId: "",
    },
  });

  function handleSubmit(values: BookSessionFormValues) {
    bookSession.mutate(
      {
        availabilitySlotId: values.availabilitySlotId,
        studentId: values.studentId,
        parentGuardianId: values.parentGuardianId || null,
      },
      {
        onSuccess: () => notify({ message: "Session booked.", severity: "success" }),
      },
    );
  }

  return (
    <Stack spacing={3} maxWidth={560}>
      <PageHeader
        title="Book a session"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Parent/Guardian id is optional — leave it blank when an adult Student books
            independently.
          </Typography>
        }
      />

      <Card variant="outlined">
        <CardContent>
          {bookSession.isSuccess ? (
            <Stack spacing={2}>
              <Typography variant="subtitle1" fontWeight={600}>
                Session booked
              </Typography>
              <CopyableId id={bookSession.data.sessionId} />
              <Typography variant="body2" color="text.secondary">
                {toTehranDisplay(bookSession.data.scheduledTimeUtc)} –{" "}
                {toTehranDisplay(bookSession.data.endTimeUtc)} (Tehran)
              </Typography>
              <Button
                component={RouterLink}
                to={paths.scheduling.sessionDetail(bookSession.data.sessionId)}
                variant="contained"
                sx={{ alignSelf: "flex-start" }}
              >
                View this session
              </Button>
            </Stack>
          ) : (
            <Form form={form} onSubmit={handleSubmit}>
              <Stack spacing={2} alignItems="flex-start">
                <FormTextField name="availabilitySlotId" label="Availability Slot id" />
                <FormTextField name="studentId" label="Student id" />
                <FormTextField name="parentGuardianId" label="Parent/Guardian id (optional)" />
                {bookSession.isError ? (
                  <ErrorState error={bookSession.error} title="Could not book session" />
                ) : null}
                <Button type="submit" variant="contained" disabled={bookSession.isPending}>
                  {bookSession.isPending ? "Booking…" : "Book session"}
                </Button>
              </Stack>
            </Form>
          )}
        </CardContent>
      </Card>
    </Stack>
  );
}

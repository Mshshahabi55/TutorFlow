import { Link as RouterLink } from "react-router-dom";
import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useDeclareAvailability } from "@/features/scheduling/hooks/useAvailabilitySlotMutations";
import { useTutorAvailabilitySlots } from "@/features/scheduling/hooks/useAvailabilitySlotQueries";
import {
  declareAvailabilitySchema,
  type DeclareAvailabilityFormValues,
} from "@/features/scheduling/validation/declareAvailabilitySchema";
import { AvailabilitySummaryCard } from "@/features/scheduling/components/AvailabilitySummaryCard";
import { AvailabilitySummaryCardSkeleton } from "@/features/scheduling/components/AvailabilitySummaryCardSkeleton";
import { SectionCard } from "@/shared/components/SectionCard";
import { minutesToTimeSpan } from "@/shared/utils/duration";
import { Form } from "@/shared/components/forms/Form";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import { FormSelect } from "@/shared/components/forms/FormSelect";
import { PageHeader } from "@/shared/components/PageHeader";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { useNotification } from "@/shared/hooks/useNotification";
import { fromTehranInput, toTehranDisplay } from "@/shared/time/tehranTime";
import { guidSchema } from "@/shared/validation/guid";
import { paths } from "@/routes/paths";

const DELIVERY_MODE_OPTIONS = [
  { value: "0", label: "Online" },
  { value: "1", label: "In-Person" },
];

/**
 * Reuses `GET /tutors/{id}/availability-slots` (Phase 4.7) — the same
 * capability `RescheduleSessionForm` and the Booking flow's Availability
 * picker already reuse — to show the Tutor's own slots split into "Your
 * Availability" (open) and "Availability History" (booked), once the same
 * Tutor id already being typed into the declare form above looks like a
 * real id. Not a new endpoint, and not a new interaction step: the id is
 * already required to declare availability at all.
 */
function YourAvailability({ tutorId }: { tutorId: string }) {
  const slotsQuery = useTutorAvailabilitySlots(tutorId);

  if (slotsQuery.isPending) {
    return (
      <Stack spacing={3}>
        <SectionCard title="Your Availability">
          <Stack spacing={2}>
            <AvailabilitySummaryCardSkeleton />
            <AvailabilitySummaryCardSkeleton />
          </Stack>
        </SectionCard>
      </Stack>
    );
  }

  if (slotsQuery.isError) {
    return <ErrorState error={slotsQuery.error} onRetry={() => void slotsQuery.refetch()} />;
  }

  const openSlots = slotsQuery.data
    .filter((slot) => !slot.isConsumed)
    .sort((a, b) => Date.parse(a.startTimeUtc) - Date.parse(b.startTimeUtc));
  const bookedSlots = slotsQuery.data
    .filter((slot) => slot.isConsumed)
    .sort((a, b) => Date.parse(b.startTimeUtc) - Date.parse(a.startTimeUtc));

  return (
    <Stack spacing={3}>
      <SectionCard title="Your Availability">
        {openSlots.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            No open Availability Slots yet — declare one above.
          </Typography>
        ) : (
          <Stack spacing={2}>
            {openSlots.map((slot) => (
              <AvailabilitySummaryCard key={slot.availabilitySlotId} slot={slot} />
            ))}
          </Stack>
        )}
      </SectionCard>

      {bookedSlots.length > 0 ? (
        <SectionCard title="Availability History">
          <Stack spacing={2}>
            {bookedSlots.map((slot) => (
              <AvailabilitySummaryCard key={slot.availabilitySlotId} slot={slot} />
            ))}
          </Stack>
        </SectionCard>
      ) : null}
    </Stack>
  );
}

/** POST /availability-slots declares a new open slot for a Tutor. */
export function DeclareAvailabilityPage() {
  const declareAvailability = useDeclareAvailability();
  const { notify } = useNotification();

  const form = useForm<DeclareAvailabilityFormValues>({
    resolver: zodResolver(declareAvailabilitySchema),
    defaultValues: { tutorId: "", startTimeLocal: "", durationMinutes: "", deliveryMode: "" },
  });

  const tutorIdValue = form.watch("tutorId");
  const hasValidTutorId = guidSchema.safeParse(tutorIdValue).success;

  function handleSubmit(values: DeclareAvailabilityFormValues) {
    declareAvailability.mutate(
      {
        tutorId: values.tutorId,
        startTimeUtc: fromTehranInput(values.startTimeLocal),
        duration: minutesToTimeSpan(Number(values.durationMinutes)),
        deliveryMode: Number(values.deliveryMode),
      },
      {
        onSuccess: () => notify({ message: "Availability declared.", severity: "success" }),
      },
    );
  }

  return (
    <Stack spacing={3} maxWidth={720}>
      <PageHeader
        title="Declare availability"
        subtitle={
          <Typography variant="body1" color="text.secondary">
            Times are entered in Tehran local time and converted to UTC before being sent — the
            backend still stores and receives UTC.
          </Typography>
        }
      />

      <Card variant="outlined">
        <CardContent>
          {declareAvailability.isSuccess ? (
            <Stack spacing={2}>
              <Typography variant="subtitle1" fontWeight={600}>
                Availability declared
              </Typography>
              <Typography variant="body2" fontFamily="ui-monospace, monospace">
                {declareAvailability.data.availabilitySlotId}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {toTehranDisplay(declareAvailability.data.startTimeUtc)} –{" "}
                {toTehranDisplay(declareAvailability.data.endTimeUtc)} (Tehran)
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Share this id with whoever should book it, or find it again below.
              </Typography>
              <Button
                component={RouterLink}
                to={paths.scheduling.availabilitySlotDetail(
                  declareAvailability.data.availabilitySlotId,
                )}
                variant="contained"
                sx={{ alignSelf: "flex-start" }}
              >
                View this slot
              </Button>
            </Stack>
          ) : (
            <Form form={form} onSubmit={handleSubmit}>
              <Stack spacing={2} alignItems="flex-start">
                <FormTextField name="tutorId" label="Tutor id" />
                <FormTextField
                  name="startTimeLocal"
                  label="Start time (Tehran)"
                  type="datetime-local"
                  slotProps={{ inputLabel: { shrink: true } }}
                />
                <FormTextField name="durationMinutes" label="Duration (minutes)" />
                <FormSelect
                  name="deliveryMode"
                  label="Delivery mode"
                  options={DELIVERY_MODE_OPTIONS}
                />
                {declareAvailability.isError ? (
                  <ErrorState error={declareAvailability.error} title="Could not declare availability" />
                ) : null}
                <Button
                  type="submit"
                  variant="contained"
                  disabled={declareAvailability.isPending}
                >
                  {declareAvailability.isPending ? "Declaring…" : "Declare availability"}
                </Button>
              </Stack>
            </Form>
          )}
        </CardContent>
      </Card>

      {hasValidTutorId ? <YourAvailability tutorId={tutorIdValue} /> : null}
    </Stack>
  );
}

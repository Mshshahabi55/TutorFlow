import { Link as RouterLink } from "react-router-dom";
import { Button, Card, CardContent, Stack, Typography } from "@mui/material";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useDeclareAvailability } from "@/features/scheduling/hooks/useAvailabilitySlotMutations";
import {
  declareAvailabilitySchema,
  type DeclareAvailabilityFormValues,
} from "@/features/scheduling/validation/declareAvailabilitySchema";
import { minutesToTimeSpan } from "@/shared/utils/duration";
import { Form } from "@/shared/components/forms/Form";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import { FormSelect } from "@/shared/components/forms/FormSelect";
import { PageHeader } from "@/shared/components/PageHeader";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { useNotification } from "@/shared/hooks/useNotification";
import { fromTehranInput, toTehranDisplay } from "@/shared/time/tehranTime";
import { paths } from "@/routes/paths";

const DELIVERY_MODE_OPTIONS = [
  { value: "0", label: "Online" },
  { value: "1", label: "In-Person" },
];

/**
 * POST /availability-slots declares a new open slot for a Tutor. There is
 * no capability to list or browse a Tutor's open slots (see the Sprint 7
 * Completion Report) — the returned id is the only way anyone, including
 * the declaring Tutor, can find this slot again, so it's surfaced clearly
 * for sharing with whoever should book it.
 */
export function DeclareAvailabilityPage() {
  const declareAvailability = useDeclareAvailability();
  const { notify } = useNotification();

  const form = useForm<DeclareAvailabilityFormValues>({
    resolver: zodResolver(declareAvailabilitySchema),
    defaultValues: { tutorId: "", startTimeLocal: "", durationMinutes: "", deliveryMode: "" },
  });

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
    <Stack spacing={3} maxWidth={560}>
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
                There is no way to browse open slots — share this id with whoever should book it.
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
    </Stack>
  );
}

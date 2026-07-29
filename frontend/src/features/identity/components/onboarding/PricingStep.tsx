import { useController, useFormContext } from "react-hook-form";
import { Collapse, FormControlLabel, Stack, Switch } from "@mui/material";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import type { TutorOnboardingFormValues } from "@/features/identity/validation/tutorOnboardingSchema";

/**
 * Step 4 — Pricing. Single currency only (Rial, displayed in Toman) —
 * ADR-019 forbids a currency selector "anywhere in v1," so this wizard
 * never offers one, regardless of the original brief's own field list.
 * `hourlyRate` is required before publishing (the backend's own
 * SubmitProfile rule); trial-lesson pricing is optional and, like the
 * hourly rate, never processes a payment (ADR-024's own Governance Note).
 */
export function PricingStep() {
  const { control, watch } = useFormContext<TutorOnboardingFormValues>();
  const { field: trialLessonAvailableField } = useController({ control, name: "trialLessonAvailable" });
  const trialLessonAvailable = watch("trialLessonAvailable");

  return (
    <Stack spacing={2.5}>
      <FormTextField
        name="hourlyRate"
        label="Hourly rate (Toman)"
        inputMode="numeric"
        helperText="Required before you can publish your profile."
      />
      <FormControlLabel
        control={
          <Switch
            checked={trialLessonAvailableField.value}
            onChange={(event) => trialLessonAvailableField.onChange(event.target.checked)}
          />
        }
        label="I offer a trial lesson"
      />
      <Collapse in={trialLessonAvailable} unmountOnExit>
        <FormTextField name="trialLessonPrice" label="Trial lesson price (Toman)" inputMode="numeric" />
      </Collapse>
      <FormTextField
        name="offeredDurationsMinutes"
        label="Session lengths offered (minutes, comma-separated)"
        placeholder="30, 60"
        helperText="e.g. 30, 60 — the durations a Student can choose when booking you."
      />
    </Stack>
  );
}

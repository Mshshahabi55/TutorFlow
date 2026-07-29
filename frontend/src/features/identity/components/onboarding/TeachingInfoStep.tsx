import { useFieldArray, useFormContext } from "react-hook-form";
import { Box, Button, Grow, IconButton, Stack, Typography } from "@mui/material";
import AddRoundedIcon from "@mui/icons-material/AddRounded";
import DeleteOutlineRoundedIcon from "@mui/icons-material/DeleteOutlineRounded";
import { FormTextField } from "@/shared/components/forms/FormTextField";
import type { TutorOnboardingFormValues } from "@/features/identity/validation/tutorOnboardingSchema";

/**
 * Step 2 — Teaching Information. `primarySubject` is the existing single
 * Tutor.Subject field (already set via the pre-ADR-024 /tutors/{id}/subject
 * endpoint) — required at Review & Publish, since the backend's own
 * SubmitProfile requires it. `tutorSubjects` (additional subjects/levels)
 * is new and additive (ADR-024) — a dynamic list, since a Tutor may teach
 * any number of them.
 */
export function TeachingInfoStep() {
  const {
    control,
    formState: { errors },
  } = useFormContext<TutorOnboardingFormValues>();
  const { fields, append, remove } = useFieldArray({ control, name: "tutorSubjects" });
  const tutorSubjectsError = errors.tutorSubjects?.root?.message ?? errors.tutorSubjects?.message;

  return (
    <Stack spacing={2.5}>
      <FormTextField
        name="primarySubject"
        label="Primary subject"
        helperText="Required before you can publish your profile."
      />

      <Box>
        <Typography variant="subtitle2" fontWeight={600} mb={1}>
          Additional subjects (optional)
        </Typography>
        <Stack spacing={1.5}>
          {fields.map((field, index) => (
            <Grow key={field.id} in appear timeout={200}>
              <Stack direction="row" spacing={1} alignItems="flex-start">
                <FormTextField name={`tutorSubjects.${index}.subject`} label="Subject" sx={{ flex: 2 }} />
                <FormTextField name={`tutorSubjects.${index}.level`} label="Level (optional)" sx={{ flex: 1 }} />
                <IconButton
                  aria-label={`Remove subject ${index + 1}`}
                  onClick={() => remove(index)}
                  sx={{ mt: 1 }}
                >
                  <DeleteOutlineRoundedIcon />
                </IconButton>
              </Stack>
            </Grow>
          ))}
        </Stack>
        <Button
          type="button"
          variant="outlined"
          startIcon={<AddRoundedIcon />}
          onClick={() => append({ subject: "", level: "" })}
          sx={{ mt: 1.5 }}
        >
          Add a subject
        </Button>
        {tutorSubjectsError ? (
          <Typography variant="body2" color="error" mt={1}>
            {tutorSubjectsError}
          </Typography>
        ) : null}
      </Box>

      <FormTextField name="yearsOfExperience" label="Years of experience" inputMode="numeric" />
      <FormTextField name="education" label="Education" multiline minRows={2} placeholder="e.g. BSc Mathematics, University of Tehran" />
      <FormTextField name="certifications" label="Certifications" multiline minRows={2} placeholder="e.g. TEFL, CELTA" />
      <FormTextField
        name="teachingMethodology"
        label="Teaching methodology"
        multiline
        minRows={3}
        placeholder="Describe your teaching approach"
      />
      <FormTextField
        name="lessonSpecialties"
        label="Lesson specialties (comma-separated)"
        placeholder="Exam prep, Conversation practice"
      />
    </Stack>
  );
}

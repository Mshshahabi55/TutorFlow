import { Stack } from "@mui/material";
import { FormTextField } from "@/shared/components/forms/FormTextField";

/** Step 1 — Personal Information. Every field is optional and self-declared (ADR-024). */
export function PersonalInfoStep() {
  return (
    <Stack spacing={2.5}>
      <FormTextField name="displayName" label="Display name" placeholder="How students will see your name" />
      <FormTextField name="headline" label="Headline" placeholder="A one-line pitch, e.g. Friendly Math Tutor" />
      <FormTextField
        name="biography"
        label="Biography"
        placeholder="Tell students about yourself and your teaching style"
        multiline
        minRows={4}
      />
      <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
        <FormTextField name="country" label="Country" sx={{ flex: 1 }} />
        <FormTextField name="city" label="City" sx={{ flex: 1 }} />
      </Stack>
      <FormTextField name="nativeLanguage" label="Native language" placeholder="e.g. English" />
      <FormTextField
        name="otherLanguages"
        label="Other languages you speak (comma-separated)"
        placeholder="French, German"
      />
    </Stack>
  );
}

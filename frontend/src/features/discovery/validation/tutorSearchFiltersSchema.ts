import { z } from "zod";
import { isoDateTimeUtcSchema } from "@/shared/validation/isoDateTime";

// Mirrors SearchTutorsQueryValidator: every filter is optional (an all-blank
// search behaves like the unfiltered discoverable Tutor list) — the only
// structural rule is that availableFrom, when provided, must be a valid
// UTC ISO 8601 date/time.
export const tutorSearchFiltersSchema = z.object({
  subject: z.string().trim(),
  language: z.string().trim(),
  location: z.string().trim(),
  availableFrom: z.string().trim().refine(
    (value) => value === "" || isoDateTimeUtcSchema.safeParse(value).success,
    "Enter a valid UTC date/time (ISO 8601), or leave blank.",
  ),
});

export type TutorSearchFiltersFormValues = z.infer<typeof tutorSearchFiltersSchema>;

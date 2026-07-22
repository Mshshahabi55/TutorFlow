import { z } from "zod";
import { isValidTehranLocalInput } from "@/shared/time/tehranTime";

// Mirrors SearchTutorsQueryValidator: every filter is optional (an all-blank
// search behaves like the unfiltered discoverable Tutor list) — the only
// structural rule is that availableFrom, when provided, must be a valid
// Tehran-local `<input type="datetime-local">` value. It is converted to
// the UTC wire value (fromTehranInput) in the page component, same as
// Declare Availability/Reschedule Session (Phase 3 Task 2).
export const tutorSearchFiltersSchema = z.object({
  subject: z.string().trim(),
  language: z.string().trim(),
  location: z.string().trim(),
  availableFrom: z.string().trim().refine(
    (value) => value === "" || isValidTehranLocalInput(value),
    "Enter a valid date and time, or leave blank.",
  ),
});

export type TutorSearchFiltersFormValues = z.infer<typeof tutorSearchFiltersSchema>;

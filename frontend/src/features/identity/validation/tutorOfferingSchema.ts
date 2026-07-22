import { z } from "zod";
import { isValidTomanAmount } from "@/shared/money/rial";

function isPositiveNumber(value: string): boolean {
  const parsed = Number(value);
  return value.trim().length > 0 && Number.isFinite(parsed) && parsed > 0;
}

// Mirrors the backend's own structural + Domain constraints exactly:
// HourlyRate.Of requires a positive whole-Rial amount not exceeding
// MaxAmount, Subject/Language/Location.Of require a non-empty value, and
// Tutor.SetOfferedDurations requires at least one positive duration
// (backend/src/Domain/Identity/**). hourlyRate holds the Toman value the
// user enters; it is converted to Rial (tomanToRial) at submit time, in
// the page component — the wire value is still Rial (ADR-019).
export const tutorOfferingSchema = z.object({
  hourlyRate: z
    .string()
    .trim()
    .refine(isValidTomanAmount, "Hourly rate must be a whole number of Toman, greater than zero."),
  subject: z.string().trim().min(1, "Subject is required."),
  language: z.string().trim().min(1, "Language is required."),
  location: z.string().trim().min(1, "Location is required."),
  offeredDurationsMinutes: z
    .string()
    .trim()
    .min(1, "At least one offered duration is required.")
    .refine(
      (value) => value.split(",").every((part) => isPositiveNumber(part)),
      "Enter one or more positive numbers of minutes, separated by commas (e.g. 30, 60).",
    ),
});

export type TutorOfferingFormValues = z.infer<typeof tutorOfferingSchema>;

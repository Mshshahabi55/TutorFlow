import { z } from "zod";
import { guidSchema } from "@/shared/validation/guid";
import { isoDateTimeUtcSchema } from "@/shared/validation/isoDateTime";

function isPositiveNumber(value: string): boolean {
  const parsed = Number(value);
  return value.trim().length > 0 && Number.isFinite(parsed) && parsed > 0;
}

// Mirrors DeclareAvailabilityCommandValidator's structural checks (TutorId,
// StartTimeUtc, Duration all required) plus SessionDuration.Of's Domain
// constraint (duration must be positive).
export const declareAvailabilitySchema = z.object({
  tutorId: guidSchema,
  startTimeUtc: isoDateTimeUtcSchema,
  durationMinutes: z
    .string()
    .trim()
    .refine(isPositiveNumber, "Duration must be a number of minutes greater than zero."),
  // Explicit `: boolean` return type prevents TypeScript's automatic
  // predicate inference (TS 5.5+) from narrowing this to a "0" | "1"
  // literal union — the field must stay a plain string so an empty
  // defaultValue ("" before any option is chosen) type-checks.
  deliveryMode: z
    .string()
    .min(1, "Select a delivery mode.")
    .refine((value): boolean => value === "0" || value === "1", "Select a delivery mode."),
});

export type DeclareAvailabilityFormValues = z.infer<typeof declareAvailabilitySchema>;

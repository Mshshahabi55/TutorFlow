import { z } from "zod";
import { isValidTehranLocalInput } from "@/shared/time/tehranTime";

/**
 * Validates the raw value of an `<input type="datetime-local">` bound to
 * Tehran local time (see frontend/src/shared/time/tehranTime.ts). This is
 * a form-field-shape check only — but `fromTehranInput` (used at submit
 * time to produce the wire value) only ever runs on a value this schema
 * already accepted, and always returns a well-formed UTC ISO 8601 string
 * with an explicit "Z" for one, so the wire contract is never weakened.
 */
export const tehranLocalDateTimeSchema = z
  .string()
  .min(1, "A date and time is required.")
  .refine(isValidTehranLocalInput, "Enter a valid date and time.");

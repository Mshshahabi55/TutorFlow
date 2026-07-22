import { z } from "zod";
import { isValidTehranLocalInput } from "@/shared/time/tehranTime";

/**
 * Validates the raw value of an `<input type="datetime-local">` bound to
 * Tehran local time (see frontend/src/shared/time/tehranTime.ts). This is
 * a form-field-shape check only — the UTC value actually sent over the
 * wire is still validated by `isoDateTimeUtcSchema` after conversion
 * (`fromTehranInput`), so the wire contract is never weakened.
 */
export const tehranLocalDateTimeSchema = z
  .string()
  .min(1, "A date and time is required.")
  .refine(isValidTehranLocalInput, "Enter a valid date and time.");

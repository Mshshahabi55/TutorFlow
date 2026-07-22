import { z } from "zod";
import { tehranLocalDateTimeSchema } from "@/shared/validation/tehranDateTime";

// Mirrors RescheduleSessionCommandValidator exactly: NewScheduledTimeUtc
// required. newScheduledTimeLocal holds the Tehran local
// `<input type="datetime-local">` value; it is converted to the UTC wire
// value (fromTehranInput) at submit time, in the page component.
export const rescheduleSessionSchema = z.object({
  newScheduledTimeLocal: tehranLocalDateTimeSchema,
});

export type RescheduleSessionFormValues = z.infer<typeof rescheduleSessionSchema>;

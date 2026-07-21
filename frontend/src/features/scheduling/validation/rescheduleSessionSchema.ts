import { z } from "zod";
import { isoDateTimeUtcSchema } from "@/shared/validation/isoDateTime";

// Mirrors RescheduleSessionCommandValidator exactly: NewScheduledTimeUtc required.
export const rescheduleSessionSchema = z.object({
  newScheduledTimeUtc: isoDateTimeUtcSchema,
});

export type RescheduleSessionFormValues = z.infer<typeof rescheduleSessionSchema>;

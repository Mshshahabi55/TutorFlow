import { z } from "zod";

// Mirrors RescheduleSessionCommandValidator exactly (Phase 4.7): the target
// is now an Availability Slot id, not a raw Tehran-entered timestamp — the
// form picks from the Tutor's own open slots rather than typing a time.
export const rescheduleSessionSchema = z.object({
  newAvailabilitySlotId: z.string().min(1, "Required"),
});

export type RescheduleSessionFormValues = z.infer<typeof rescheduleSessionSchema>;

import { z } from "zod";
import { guidSchema } from "@/shared/validation/guid";

// Mirrors BookSessionCommandValidator exactly: AvailabilitySlotId and
// StudentId required; ParentGuardianId optional but, when provided, cannot
// be empty (guidSchema itself already rejects empty).
export const bookSessionSchema = z.object({
  availabilitySlotId: guidSchema,
  studentId: guidSchema,
  parentGuardianId: z.union([guidSchema, z.literal("")]),
});

export type BookSessionFormValues = z.infer<typeof bookSessionSchema>;

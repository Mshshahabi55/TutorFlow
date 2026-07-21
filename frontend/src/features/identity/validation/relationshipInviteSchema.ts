import { z } from "zod";
import { guidSchema } from "@/shared/validation/guid";

// Mirrors CreateRelationshipInvitationCommandValidator's structural checks
// (both ids required, non-empty) exactly.
export const relationshipInviteSchema = z.object({
  parentGuardianId: guidSchema,
  studentId: guidSchema,
});

export type RelationshipInviteFormValues = z.infer<typeof relationshipInviteSchema>;

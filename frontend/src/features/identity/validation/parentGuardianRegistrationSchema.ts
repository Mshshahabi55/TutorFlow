import { z } from "zod";
import { emailSchema, passwordSchema } from "@/shared/validation/credentials";

// Mirrors RegisterParentGuardianCommand(string Email, string Password)
// exactly (backend/src/Application/Identity/Commands/RegisterParentGuardianCommand.cs).
export const parentGuardianRegistrationSchema = z.object({
  email: emailSchema,
  password: passwordSchema,
});

export type ParentGuardianRegistrationFormValues = z.infer<typeof parentGuardianRegistrationSchema>;

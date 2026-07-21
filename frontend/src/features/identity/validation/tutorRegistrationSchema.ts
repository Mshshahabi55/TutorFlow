import { z } from "zod";
import { emailSchema, passwordSchema } from "@/shared/validation/credentials";

// Mirrors RegisterTutorCommand(string Email, string Password) exactly
// (backend/src/Application/Identity/Commands/RegisterTutorCommand.cs).
export const tutorRegistrationSchema = z.object({
  email: emailSchema,
  password: passwordSchema,
});

export type TutorRegistrationFormValues = z.infer<typeof tutorRegistrationSchema>;

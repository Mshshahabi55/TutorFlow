import { z } from "zod";
import { emailSchema, passwordSchema } from "@/shared/validation/credentials";

// Mirrors RegisterStudentCommand(string Email, string Password, bool IsMinor)
// exactly (backend/src/Application/Identity/Commands/RegisterStudentCommand.cs).
export const studentRegistrationSchema = z.object({
  email: emailSchema,
  password: passwordSchema,
  isMinor: z.boolean(),
});

export type StudentRegistrationFormValues = z.infer<typeof studentRegistrationSchema>;

import { z } from "zod";
import { emailSchema, passwordSchema } from "@/shared/validation/credentials";

export const loginSchema = z.object({
  email: emailSchema,
  password: passwordSchema,
});

export type LoginFormValues = z.infer<typeof loginSchema>;

import { z } from "zod";
import { guidSchema } from "@/shared/validation/guid";
import { passwordSchema } from "@/shared/validation/credentials";

// Mirrors AdminResetPasswordCommand(Guid AccountId, string NewPassword)
// exactly (backend/src/Application/Identity/Commands/AdminResetPasswordCommand.cs).
export const resetPasswordSchema = z.object({
  accountId: guidSchema,
  newPassword: passwordSchema,
});

export type ResetPasswordFormValues = z.infer<typeof resetPasswordSchema>;

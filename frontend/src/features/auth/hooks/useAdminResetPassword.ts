import { useMutation } from "@tanstack/react-query";
import { resetPassword } from "@/features/auth/api/authService";

export function useAdminResetPassword() {
  return useMutation({
    mutationFn: ({ accountId, newPassword }: { accountId: string; newPassword: string }) =>
      resetPassword(accountId, newPassword),
  });
}

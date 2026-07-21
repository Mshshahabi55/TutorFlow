import { useMutation } from "@tanstack/react-query";
import { registerParentGuardian } from "@/features/identity/api/identityService";

export function useRegisterParentGuardian() {
  return useMutation({
    mutationFn: ({ email, password }: { email: string; password: string }) =>
      registerParentGuardian(email, password),
  });
}

import { useMutation } from "@tanstack/react-query";
import { registerStudent } from "@/features/identity/api/identityService";

export interface RegisterStudentInput {
  email: string;
  password: string;
  isMinor: boolean;
}

export function useRegisterStudent() {
  return useMutation({
    mutationFn: ({ email, password, isMinor }: RegisterStudentInput) =>
      registerStudent(email, password, isMinor),
  });
}

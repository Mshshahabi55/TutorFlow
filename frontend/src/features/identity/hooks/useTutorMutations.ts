import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  approveTutor,
  registerTutor,
  setTutorHourlyRate,
  setTutorLanguage,
  setTutorLocation,
  setTutorOfferedDurations,
  setTutorSubject,
  suspendTutor,
} from "@/features/identity/api/identityService";

/** Every Tutor query key is namespaced under ["identity","tutors",...], so this one invalidation covers detail, directory, and the pending queue together. */
function useInvalidateTutors() {
  const queryClient = useQueryClient();
  return () => {
    void queryClient.invalidateQueries({ queryKey: ["identity", "tutors"] });
  };
}

export function useRegisterTutor() {
  const invalidate = useInvalidateTutors();
  return useMutation({
    mutationFn: ({ email, password }: { email: string; password: string }) =>
      registerTutor(email, password),
    onSuccess: invalidate,
  });
}

export function useApproveTutor(tutorId: string) {
  const invalidate = useInvalidateTutors();
  return useMutation({
    mutationFn: () => approveTutor(tutorId),
    onSuccess: invalidate,
  });
}

export function useSuspendTutor(tutorId: string) {
  const invalidate = useInvalidateTutors();
  return useMutation({
    mutationFn: () => suspendTutor(tutorId),
    onSuccess: invalidate,
  });
}

export function useSetTutorHourlyRate(tutorId: string) {
  const invalidate = useInvalidateTutors();
  return useMutation({
    mutationFn: (amount: number) => setTutorHourlyRate(tutorId, amount),
    onSuccess: invalidate,
  });
}

export function useSetTutorSubject(tutorId: string) {
  const invalidate = useInvalidateTutors();
  return useMutation({
    mutationFn: (subject: string) => setTutorSubject(tutorId, subject),
    onSuccess: invalidate,
  });
}

export function useSetTutorLanguage(tutorId: string) {
  const invalidate = useInvalidateTutors();
  return useMutation({
    mutationFn: (language: string) => setTutorLanguage(tutorId, language),
    onSuccess: invalidate,
  });
}

export function useSetTutorLocation(tutorId: string) {
  const invalidate = useInvalidateTutors();
  return useMutation({
    mutationFn: (location: string) => setTutorLocation(tutorId, location),
    onSuccess: invalidate,
  });
}

export function useSetTutorOfferedDurations(tutorId: string) {
  const invalidate = useInvalidateTutors();
  return useMutation({
    mutationFn: (durations: string[]) => setTutorOfferedDurations(tutorId, durations),
    onSuccess: invalidate,
  });
}

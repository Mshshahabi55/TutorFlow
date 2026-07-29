import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  approveTutor,
  registerTutor,
  setTutorHourlyRate,
  setTutorLanguage,
  setTutorLocation,
  setTutorMedia,
  setTutorOfferedDurations,
  setTutorPersonalInfo,
  setTutorPricing,
  setTutorSubject,
  setTutorTeachingInfo,
  submitTutorProfile,
  suspendTutor,
  type SetTutorMediaRequest,
  type SetTutorPersonalInfoRequest,
  type SetTutorPricingRequest,
  type SetTutorTeachingInfoRequest,
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

// ADR-024 (Accepted, 2026-07-28) — Tutor Onboarding Wizard. Each hook below
// autosaves one wizard step's own field group — the wizard calls the
// matching hook's `mutate` on "Next," never a combined "save everything"
// call, same PATCH-only-what-changed convention every existing Tutor
// self-service hook above already follows.

export function useSetTutorPersonalInfo(tutorId: string) {
  const invalidate = useInvalidateTutors();
  return useMutation({
    mutationFn: (request: SetTutorPersonalInfoRequest) => setTutorPersonalInfo(tutorId, request),
    onSuccess: invalidate,
  });
}

export function useSetTutorTeachingInfo(tutorId: string) {
  const invalidate = useInvalidateTutors();
  return useMutation({
    mutationFn: (request: SetTutorTeachingInfoRequest) => setTutorTeachingInfo(tutorId, request),
    onSuccess: invalidate,
  });
}

export function useSetTutorMedia(tutorId: string) {
  const invalidate = useInvalidateTutors();
  return useMutation({
    mutationFn: (request: SetTutorMediaRequest) => setTutorMedia(tutorId, request),
    onSuccess: invalidate,
  });
}

export function useSetTutorPricing(tutorId: string) {
  const invalidate = useInvalidateTutors();
  return useMutation({
    mutationFn: (request: SetTutorPricingRequest) => setTutorPricing(tutorId, request),
    onSuccess: invalidate,
  });
}

export function useSubmitTutorProfile(tutorId: string) {
  const invalidate = useInvalidateTutors();
  return useMutation({
    mutationFn: () => submitTutorProfile(tutorId),
    onSuccess: invalidate,
  });
}

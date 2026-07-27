import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  declareAvailability,
  type DeclareAvailabilityInput,
} from "@/features/scheduling/api/schedulingService";

/**
 * Invalidates `useTutorAvailabilitySlots`' own query key (`["scheduling",
 * "availabilitySlots", "byTutor", tutorId]`) on success, so the Manage
 * Your Schedule calendar reflects a newly declared slot immediately
 * instead of requiring a manual page refresh — a client-side cache fix,
 * not a new endpoint (there is still no list-changed notification from the
 * backend to react to).
 */
export function useDeclareAvailability() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: DeclareAvailabilityInput) => declareAvailability(input),
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({
        queryKey: ["scheduling", "availabilitySlots", "byTutor", variables.tutorId],
      });
    },
  });
}

import { useMutation } from "@tanstack/react-query";
import {
  declareAvailability,
  type DeclareAvailabilityInput,
} from "@/features/scheduling/api/schedulingService";

/** No Availability Slot list query exists to invalidate (the backend exposes none — see the Sprint 7 Completion Report). */
export function useDeclareAvailability() {
  return useMutation({
    mutationFn: (input: DeclareAvailabilityInput) => declareAvailability(input),
  });
}

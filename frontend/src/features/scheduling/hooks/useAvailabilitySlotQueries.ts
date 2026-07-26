import { useQuery } from "@tanstack/react-query";
import {
  fetchAvailabilitySlotById,
  fetchTutorAvailabilitySlots,
} from "@/features/scheduling/api/schedulingService";

export function useAvailabilitySlot(availabilitySlotId: string | undefined) {
  return useQuery({
    queryKey: ["scheduling", "availabilitySlots", "detail", availabilitySlotId],
    queryFn: () => fetchAvailabilitySlotById(availabilitySlotId as string),
    enabled: Boolean(availabilitySlotId),
  });
}

export function useTutorAvailabilitySlots(tutorId: string | undefined) {
  return useQuery({
    queryKey: ["scheduling", "availabilitySlots", "byTutor", tutorId],
    queryFn: () => fetchTutorAvailabilitySlots(tutorId as string),
    enabled: Boolean(tutorId),
  });
}

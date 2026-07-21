import { useQuery } from "@tanstack/react-query";
import { fetchAvailabilitySlotById } from "@/features/scheduling/api/schedulingService";

export function useAvailabilitySlot(availabilitySlotId: string | undefined) {
  return useQuery({
    queryKey: ["scheduling", "availabilitySlots", "detail", availabilitySlotId],
    queryFn: () => fetchAvailabilitySlotById(availabilitySlotId as string),
    enabled: Boolean(availabilitySlotId),
  });
}

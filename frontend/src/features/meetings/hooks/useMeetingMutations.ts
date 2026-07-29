import { useMutation, useQueryClient } from "@tanstack/react-query";
import { startMeeting } from "@/features/meetings/api/meetingService";

export function useStartMeeting(sessionId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => startMeeting(sessionId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["meetings", "bySession", sessionId] });
      void queryClient.invalidateQueries({ queryKey: ["meetings", "byConversation"] });
    },
  });
}

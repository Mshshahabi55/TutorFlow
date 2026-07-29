import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  markConversationRead,
  sendMessage,
  startConversation,
} from "@/features/communication/api/communicationService";

/** Every Conversation query key is namespaced under ["communication","conversations",...], so this one invalidation covers the list and every open detail. */
function useInvalidateConversations() {
  const queryClient = useQueryClient();
  return () => {
    void queryClient.invalidateQueries({ queryKey: ["communication", "conversations"] });
  };
}

export function useStartConversation() {
  const invalidate = useInvalidateConversations();
  return useMutation({
    mutationFn: (targetAccountId: string) => startConversation(targetAccountId),
    onSuccess: invalidate,
  });
}

export function useSendMessage(conversationId: string) {
  const invalidate = useInvalidateConversations();
  return useMutation({
    mutationFn: (body: string) => sendMessage(conversationId, body),
    onSuccess: invalidate,
  });
}

export function useMarkConversationRead(conversationId: string) {
  const invalidate = useInvalidateConversations();
  return useMutation({
    mutationFn: () => markConversationRead(conversationId),
    onSuccess: invalidate,
  });
}

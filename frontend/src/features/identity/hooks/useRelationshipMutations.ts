import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  confirmRelationship,
  inviteRelationship,
} from "@/features/identity/api/identityService";

export function useInviteRelationship() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      parentGuardianId,
      studentId,
    }: {
      parentGuardianId: string;
      studentId: string;
    }) => inviteRelationship(parentGuardianId, studentId),
    onSuccess: (relationship) => {
      void queryClient.invalidateQueries({
        queryKey: ["identity", "relationships", "byAccount", relationship.parentGuardianId],
      });
      void queryClient.invalidateQueries({
        queryKey: ["identity", "relationships", "byAccount", relationship.studentId],
      });
    },
  });
}

export function useConfirmRelationship() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (relationshipId: string) => confirmRelationship(relationshipId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["identity", "relationships"] });
    },
  });
}

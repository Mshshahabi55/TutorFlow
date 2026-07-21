import { Button, Stack } from "@mui/material";
import { useApproveTutor, useSuspendTutor } from "@/features/identity/hooks/useTutorMutations";
import { useConfirmDialog } from "@/shared/hooks/useConfirmDialog";
import { useNotification } from "@/shared/hooks/useNotification";
import type { TutorDto } from "@/services/api/dtos";

export interface TutorApprovalActionsProps {
  tutor: TutorDto;
}

/**
 * Approve/Suspend, confirm-gated — the two Admin actions on a Tutor
 * (ADM-1, ADM-2). Shared by AdminPendingTutorsPage's row actions and
 * TutorDetailPage, since suspending a Tutor is not limited to ones still in
 * the pending queue — an already-approved, discoverable Tutor can be
 * suspended too, and TutorDetailPage is the only place that Tutor is
 * findable once they've left the pending list.
 */
export function TutorApprovalActions({ tutor }: TutorApprovalActionsProps) {
  const approveTutor = useApproveTutor(tutor.tutorId);
  const suspendTutor = useSuspendTutor(tutor.tutorId);
  const { confirm } = useConfirmDialog();
  const { notify } = useNotification();

  async function handleApprove() {
    const confirmed = await confirm({
      title: "Approve this Tutor?",
      description: "They will become discoverable to Students and Parents/Guardians.",
      confirmLabel: "Approve Tutor",
    });
    if (!confirmed) {
      return;
    }
    approveTutor.mutate(undefined, {
      onSuccess: () => notify({ message: "Tutor approved.", severity: "success" }),
    });
  }

  async function handleSuspend() {
    const confirmed = await confirm({
      title: "Suspend this Tutor?",
      description: "They will no longer be discoverable or bookable.",
      confirmLabel: "Suspend Tutor",
      destructive: true,
    });
    if (!confirmed) {
      return;
    }
    suspendTutor.mutate(undefined, {
      onSuccess: () => notify({ message: "Tutor suspended.", severity: "info" }),
    });
  }

  return (
    <Stack direction="row" spacing={1}>
      <Button
        size="small"
        variant="contained"
        onClick={() => void handleApprove()}
        disabled={approveTutor.isPending || tutor.isApproved}
      >
        Approve
      </Button>
      <Button
        size="small"
        color="error"
        variant="outlined"
        onClick={() => void handleSuspend()}
        disabled={suspendTutor.isPending || tutor.isSuspended}
      >
        Suspend
      </Button>
    </Stack>
  );
}

import { Stack } from "@mui/material";
import Button from "@mui/material/Button";
import {
  useCancelSession,
  useCompleteSession,
  useMarkSessionNoShow,
} from "@/features/scheduling/hooks/useSessionMutations";
import { useConfirmDialog } from "@/shared/hooks/useConfirmDialog";
import { useNotification } from "@/shared/hooks/useNotification";
import { SessionStatus } from "@/services/api/dtos";
import type { SessionDto } from "@/services/api/dtos";

export interface SessionActionsProps {
  session: SessionDto;
}

/**
 * Cancel/Complete/No-Show — the three one-click, confirm-gated Session
 * transitions, each valid only from Scheduled (Session.Cancel/Complete/
 * MarkNoShow all guard "only a Scheduled Session"). Shared by
 * SessionDetailPage and both schedule list pages so the same guard and
 * confirm/notify behavior isn't re-implemented three times. Reschedule is
 * not included here — it needs a new time as input, not a one-click
 * confirm, so it stays a dedicated form on SessionDetailPage only.
 */
export function SessionActions({ session }: SessionActionsProps) {
  const cancelSession = useCancelSession(session.sessionId);
  const completeSession = useCompleteSession(session.sessionId);
  const markNoShow = useMarkSessionNoShow(session.sessionId);
  const { confirm } = useConfirmDialog();
  const { notify } = useNotification();

  const isScheduled = session.status === SessionStatus.Scheduled;
  const isBusy = cancelSession.isPending || completeSession.isPending || markNoShow.isPending;

  async function handleCancel() {
    const confirmed = await confirm({
      title: "Cancel this session?",
      description: "This cannot be undone.",
      confirmLabel: "Cancel session",
      destructive: true,
    });
    if (!confirmed) {
      return;
    }
    cancelSession.mutate(undefined, {
      onSuccess: () => notify({ message: "Session cancelled.", severity: "info" }),
    });
  }

  async function handleComplete() {
    const confirmed = await confirm({
      title: "Mark this session as completed?",
      confirmLabel: "Mark completed",
    });
    if (!confirmed) {
      return;
    }
    completeSession.mutate(undefined, {
      onSuccess: () => notify({ message: "Session marked completed.", severity: "success" }),
    });
  }

  async function handleNoShow() {
    const confirmed = await confirm({
      title: "Mark this session as No-Show?",
      confirmLabel: "Mark No-Show",
      destructive: true,
    });
    if (!confirmed) {
      return;
    }
    markNoShow.mutate(undefined, {
      onSuccess: () => notify({ message: "Session marked No-Show.", severity: "warning" }),
    });
  }

  return (
    <Stack direction="row" spacing={1} flexWrap="wrap">
      <Button
        size="small"
        variant="outlined"
        disabled={!isScheduled || isBusy}
        onClick={() => void handleComplete()}
      >
        Complete
      </Button>
      <Button
        size="small"
        variant="outlined"
        color="warning"
        disabled={!isScheduled || isBusy}
        onClick={() => void handleNoShow()}
      >
        No-Show
      </Button>
      <Button
        size="small"
        variant="outlined"
        color="error"
        disabled={!isScheduled || isBusy}
        onClick={() => void handleCancel()}
      >
        Cancel
      </Button>
    </Stack>
  );
}

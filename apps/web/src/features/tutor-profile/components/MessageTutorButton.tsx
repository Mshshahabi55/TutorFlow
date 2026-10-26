'use client';

import { useRouter } from 'next/navigation';
import { useMutation } from '@tanstack/react-query';
import { Button } from '@/components/ui/Button';
import { useToast } from '@/components/ui/Toast';
import { ApiError } from '@/lib/api/client';
import { createOrGetConversation } from '@/features/messaging/api';

export interface MessageTutorButtonProps {
  tutorUserId: string;
  isAuthenticatedStudent: boolean;
  onRequireAuth: (action: string) => void;
}

/**
 * The public Tutor profile's entry point into Messaging (ADR-0030 Batch 69) — authenticated
 * Students only. A Tutor-only viewer (or a signed-out visitor) is routed through the existing
 * auth-gate flow and never reaches `createOrGetConversation`; the server-side `StudentOnly` policy
 * on that endpoint is the real enforcement, this is only the UX front door to it.
 */
export function MessageTutorButton({ tutorUserId, isAuthenticatedStudent, onRequireAuth }: MessageTutorButtonProps) {
  const router = useRouter();
  const { showToast } = useToast();

  const startConversation = useMutation({
    mutationFn: () => createOrGetConversation(tutorUserId),
    onSuccess: (response) => router.push(`/messages/${response.id}`),
    onError: (error) => {
      const message =
        error instanceof ApiError
          ? (error.problem.detail ?? error.problem.title ?? 'Could not start this conversation. Please try again.')
          : 'Could not start this conversation. Please try again.';
      showToast(message, 'error');
    },
  });

  if (!isAuthenticatedStudent) {
    return (
      <Button variant="secondary" fullWidth onClick={() => onRequireAuth('message this tutor')}>
        Message
      </Button>
    );
  }

  return (
    <Button
      variant="secondary"
      fullWidth
      loading={startConversation.isPending}
      onClick={() => startConversation.mutate()}
    >
      Message
    </Button>
  );
}

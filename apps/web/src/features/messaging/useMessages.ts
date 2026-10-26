'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { getMessages, markConversationRead, sendMessage } from './api';
import { MessageDto, SendMessageCommand, TutorDisplaySummary } from './types';
import { conversationsQueryKey } from './useConversations';

const POLL_INTERVAL_MS = 5000;

export function latestMessagesQueryKey(conversationId: string) {
  return ['messaging', 'conversations', conversationId, 'messages', 'latest'] as const;
}

/** Merges a newly-fetched page into the accumulated, chronologically-ordered message list,
 * de-duplicating by `id`. Safe to call with overlapping pages — the freshest-page poll and a
 * previously-loaded older page always converge on the same entry for any message both contain. */
export function mergeMessages(existing: MessageDto[], incoming: MessageDto[]): MessageDto[] {
  const byId = new Map(existing.map((message) => [message.id, message] as const));
  for (const message of incoming) {
    byId.set(message.id, message);
  }
  return Array.from(byId.values()).sort((a, b) => a.sequenceNumber - b.sequenceNumber);
}

/** No built-in TanStack Query primitive for "only poll while the tab is visible" — tracked here
 * with the Page Visibility API directly. SSR-safe default of `true` since this only ever runs in
 * a `'use client'` hook mounted in the browser. */
export function useIsDocumentVisible(): boolean {
  const [isVisible, setIsVisible] = useState(() => typeof document === 'undefined' || document.visibilityState === 'visible');

  useEffect(() => {
    const handler = () => setIsVisible(document.visibilityState === 'visible');
    document.addEventListener('visibilitychange', handler);
    return () => document.removeEventListener('visibilitychange', handler);
  }, []);

  return isVisible;
}

export interface UseMessagesResult {
  tutor: TutorDisplaySummary | null | undefined;
  messages: MessageDto[];
  isLoading: boolean;
  isError: boolean;
  error: unknown;
  refetch: () => void;
  hasOlder: boolean;
  isLoadingOlder: boolean;
  isOlderError: boolean;
  loadOlder: () => void;
}

/**
 * Polls the newest page every `POLL_INTERVAL_MS` (approved decision #7), only while `enabled`
 * (thread mounted) and the page is visible — never in a background tab. Older history is fetched
 * on demand via `loadOlder`, using the real `nextBeforeCursor` keyset cursor (ADR-0030), and
 * merged into one chronological, de-duplicated list so neither a poll refresh nor a manual
 * "load older" click can ever produce a repeated message.
 */
export function useMessages(conversationId: string, enabled: boolean): UseMessagesResult {
  const isVisible = useIsDocumentVisible();
  const [olderMessages, setOlderMessages] = useState<MessageDto[]>([]);
  const [oldestCursor, setOldestCursor] = useState<number | null>(null);
  const [hasOlder, setHasOlder] = useState(false);
  const [isLoadingOlder, setIsLoadingOlder] = useState(false);
  const [isOlderError, setIsOlderError] = useState(false);

  const latest = useQuery({
    queryKey: latestMessagesQueryKey(conversationId),
    queryFn: ({ signal }) => getMessages(conversationId, undefined, signal),
    enabled,
    refetchInterval: enabled && isVisible ? POLL_INTERVAL_MS : false,
  });

  // Reset accumulated older-page state when the conversation identity itself changes, and freeze
  // the initial backward cursor once the first page for *this* conversation arrives — computed
  // directly during rendering (React's documented escape hatch for state that depends on a prop
  // changing) rather than in an Effect, since an Effect that calls setState synchronously from
  // already-available data is just an extra, unnecessary render pass.
  const [trackedConversationId, setTrackedConversationId] = useState(conversationId);
  const [initializedForConversation, setInitializedForConversation] = useState<string | null>(null);
  if (trackedConversationId !== conversationId) {
    setTrackedConversationId(conversationId);
    setOlderMessages([]);
    setOldestCursor(null);
    setHasOlder(false);
    setInitializedForConversation(null);
  } else if (latest.data && initializedForConversation !== conversationId) {
    setInitializedForConversation(conversationId);
    setOldestCursor(latest.data.nextBeforeCursor);
    setHasOlder(latest.data.nextBeforeCursor !== null);
  }

  const messages = useMemo(
    () => mergeMessages(olderMessages, latest.data?.messages ?? []),
    [olderMessages, latest.data],
  );

  const loadOlder = useCallback(() => {
    if (oldestCursor === null || isLoadingOlder) {
      return;
    }
    setIsLoadingOlder(true);
    setIsOlderError(false);
    getMessages(conversationId, { before: oldestCursor })
      .then((page) => {
        setOlderMessages((prev) => mergeMessages(prev, page.messages));
        setOldestCursor(page.nextBeforeCursor);
        setHasOlder(page.nextBeforeCursor !== null);
      })
      .catch(() => setIsOlderError(true))
      .finally(() => setIsLoadingOlder(false));
  }, [conversationId, oldestCursor, isLoadingOlder]);

  return {
    tutor: latest.data?.tutor,
    messages,
    isLoading: latest.isLoading,
    isError: latest.isError,
    error: latest.error,
    refetch: latest.refetch,
    hasOlder,
    isLoadingOlder,
    isOlderError,
    loadOlder,
  };
}

export function useSendMessage(conversationId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (command: SendMessageCommand) => sendMessage(conversationId, command),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: latestMessagesQueryKey(conversationId) });
      queryClient.invalidateQueries({ queryKey: conversationsQueryKey });
    },
  });
}

/** Fire-and-forget from the caller's perspective (mark-read failures never block reading the
 * thread) — but still a real mutation so callers can observe/guard repeat calls rather than
 * firing blind. */
export function useMarkConversationRead(conversationId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => markConversationRead(conversationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: conversationsQueryKey });
    },
  });
}

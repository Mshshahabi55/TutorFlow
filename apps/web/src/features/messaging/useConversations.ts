'use client';

import { useQuery } from '@tanstack/react-query';
import { listConversations } from './api';

export const conversationsQueryKey = ['messaging', 'conversations'] as const;

/** List is capped at `MAX_CONVERSATIONS` server-side (ADR-0030 Batch 69) — a temporary V2 limit,
 * not full pagination. There is deliberately no "load more conversations" affordance here. */
export function useConversations(enabled: boolean) {
  return useQuery({
    queryKey: conversationsQueryKey,
    queryFn: ({ signal }) => listConversations(signal),
    enabled,
  });
}

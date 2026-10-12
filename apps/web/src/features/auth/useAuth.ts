'use client';

import { useQuery, useQueryClient } from '@tanstack/react-query';
import { getCurrentUser } from './api';
import { AuthRole, Session } from './types';

export type { AuthRole, Session };

export const sessionQueryKey = ['session', 'currentUser'] as const;

export interface AuthState {
  isAuthenticated: boolean;
  isLoading: boolean;
  userId: string | null;
  email: string | null;
  roles: AuthRole[];
}

/**
 * The real session, not a stub (ADR-0023) — backed by one React Query cache entry
 * (`sessionQueryKey`) shared by every caller, so there is exactly one "am I logged in" source
 * of truth across the app. The session cookie is httpOnly, so the only way to know whether it's
 * valid is to ask the API via `/me`; there is no synchronous localStorage-style shortcut, and
 * `isLoading` must be handled by callers rather than assumed-guest.
 */
export function useAuth(): AuthState {
  const { data, isLoading } = useQuery({
    queryKey: sessionQueryKey,
    queryFn: ({ signal }) => getCurrentUser(signal),
    staleTime: 60_000,
    retry: false,
  });

  return {
    isAuthenticated: Boolean(data),
    isLoading,
    userId: data?.userId ?? null,
    email: data?.email ?? null,
    roles: data?.roles ?? [],
  };
}

/** Lets login/register/logout mutations update the shared session cache directly. */
export function useSessionCache() {
  const queryClient = useQueryClient();
  return {
    setSession: (session: Session | null) => queryClient.setQueryData(sessionQueryKey, session),
    invalidate: () => queryClient.invalidateQueries({ queryKey: sessionQueryKey }),
  };
}

'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { getProfile, updateProfile, UpdateProfileInput } from './api';

export const profileQueryKey = ['profile'] as const;

/** First/Last name, Identity-owned — see ApplicationUser.cs. Enabled only when actually needed
 * (the About-stage wizard), not fetched app-wide alongside `useAuth()`. */
export function useProfile(enabled: boolean) {
  return useQuery({
    queryKey: profileQueryKey,
    queryFn: ({ signal }) => getProfile(signal),
    enabled,
  });
}

export function useUpdateProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdateProfileInput) => updateProfile(input),
    onSuccess: (data) => queryClient.setQueryData(profileQueryKey, data),
  });
}

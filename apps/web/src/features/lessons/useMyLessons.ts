'use client';

import { useQuery } from '@tanstack/react-query';
import { getMyLessons } from './api';

export function useMyLessons(enabled: boolean) {
  return useQuery({
    queryKey: ['lessons', 'me'] as const,
    queryFn: ({ signal }) => getMyLessons(signal),
    enabled,
  });
}

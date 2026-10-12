'use client';

import { useQuery } from '@tanstack/react-query';
import { getLanguages, getSpecialties, getSubjects } from './api';

// Reference/catalog data (Subjects/Specialties/Languages) changes rarely compared to search
// results — a long staleTime avoids re-fetching the same small list on every render while
// still allowing a manual refetch/reload to pick up admin-curated changes eventually.
const CATALOG_STALE_TIME_MS = 60 * 60 * 1000;

export function useSubjects() {
  return useQuery({
    queryKey: ['catalog', 'subjects'],
    queryFn: ({ signal }) => getSubjects({ signal }),
    staleTime: CATALOG_STALE_TIME_MS,
  });
}

export function useSpecialties(subjectId?: string) {
  return useQuery({
    queryKey: ['catalog', 'specialties', subjectId ?? 'all'],
    queryFn: ({ signal }) => getSpecialties(subjectId, { signal }),
    staleTime: CATALOG_STALE_TIME_MS,
  });
}

export function useLanguages() {
  return useQuery({
    queryKey: ['catalog', 'languages'],
    queryFn: ({ signal }) => getLanguages({ signal }),
    staleTime: CATALOG_STALE_TIME_MS,
  });
}

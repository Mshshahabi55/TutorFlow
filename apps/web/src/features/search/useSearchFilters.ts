'use client';

import { usePathname, useRouter, useSearchParams } from 'next/navigation';
import { useCallback, useMemo } from 'react';
import { filtersToSearchParams, parseFiltersFromSearchParams } from './filterSerialization';
import { SearchFilters } from './types';

export interface UseSearchFiltersResult {
  filters: SearchFilters;
  /** Updates one or more fields. Discrete controls (selects, checkboxes, pagination) push a
   * new history entry so Back/Forward steps through filter changes one at a time, per the
   * task's requirement that back/forward navigation behave correctly. Pass `replace: true`
   * for continuous input (the debounced free-text box) so every keystroke doesn't produce
   * its own history entry. Any field change other than `page` itself resets `page` to 1 —
   * changing a filter while on page 3 of the old results would otherwise silently show page 3
   * of a completely different result set. */
  updateFilters: (partial: Partial<SearchFilters>, options?: { replace?: boolean }) => void;
  resetFilters: () => void;
}

export function useSearchFilters(): UseSearchFiltersResult {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const filters = useMemo(() => parseFiltersFromSearchParams(searchParams), [searchParams]);

  const updateFilters = useCallback(
    (partial: Partial<SearchFilters>, options?: { replace?: boolean }) => {
      const isPageChangeOnly = Object.keys(partial).length === 1 && 'page' in partial;
      const next: SearchFilters = {
        ...filters,
        ...partial,
        page: isPageChangeOnly ? (partial.page as number) : 1,
      };

      const query = filtersToSearchParams(next).toString();
      const url = query ? `${pathname}?${query}` : pathname;

      if (options?.replace) {
        router.replace(url, { scroll: false });
      } else {
        router.push(url, { scroll: false });
      }
    },
    [filters, pathname, router],
  );

  const resetFilters = useCallback(() => {
    router.push(pathname, { scroll: false });
  }, [pathname, router]);

  return { filters, updateFilters, resetFilters };
}

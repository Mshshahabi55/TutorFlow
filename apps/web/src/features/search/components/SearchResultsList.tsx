'use client';

import { useEffect } from 'react';
import styles from './SearchResultsList.module.css';
import { Card } from '@/components/ui/Card';
import { Skeleton } from '@/components/ui/Skeleton';
import { EmptyState } from '@/components/ui/EmptyState';
import { ErrorState } from '@/components/ui/ErrorState';
import { Pagination } from '@/components/ui/Pagination';
import { Select } from '@/components/ui/Select';
import { Button } from '@/components/ui/Button';
import { TutorResultCard } from './TutorResultCard';
import { SearchFilters, SearchTutorsResponse, SORT_OPTIONS } from '../types';
import { ApiError } from '@/lib/api/client';

export interface SearchResultsListProps {
  data: SearchTutorsResponse | undefined;
  isLoading: boolean;
  isError: boolean;
  error: unknown;
  filters: SearchFilters;
  onChange: (partial: Partial<SearchFilters>, options?: { replace?: boolean }) => void;
  onClearAll: () => void;
  onRetry: () => void;
  /** Auth/favorite state for the real Favorite/Message/Book Trial CTAs on each card (ADR-0024)
   * — owned by FindTutorsView (one AuthGateModal, one bulk favorite-status query) and threaded
   * through unchanged, the same shape every card needs. */
  isStudent: boolean;
  isTutorOnlyViewer: boolean;
  favoritedTutorUserIds: ReadonlySet<string>;
  onToggleFavorite: (tutorUserId: string) => void;
  onRequireAuth: (action: string) => void;
}

function SkeletonCard() {
  return (
    <Card>
      <div style={{ display: 'flex', gap: '0.75rem', marginBottom: '0.75rem' }}>
        <Skeleton width="3rem" height="3rem" />
        <div style={{ flex: 1 }}>
          <Skeleton width="60%" height="1rem" />
          <div style={{ marginTop: '0.5rem' }}>
            <Skeleton width="90%" height="0.875rem" />
          </div>
        </div>
      </div>
      <Skeleton width="100%" height="2.5rem" />
    </Card>
  );
}

export function SearchResultsList({
  data,
  isLoading,
  isError,
  error,
  filters,
  onChange,
  onClearAll,
  onRetry,
  isStudent,
  isTutorOnlyViewer,
  favoritedTutorUserIds,
  onToggleFavorite,
  onRequireAuth,
}: SearchResultsListProps) {
  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1;

  // Regression fix: a URL requesting a page beyond the real result set (e.g. a stale
  // bookmark, or a filter narrowing the results after the page was set) returns zero items
  // with totalCount > 0 — that's "wrong page," not "no results," and showing the empty state
  // for it is misleading. Auto-correct to the last valid page instead of claiming zero matches.
  const isPageBeyondResults = data !== undefined && data.totalCount > 0 && data.items.length === 0 && filters.page > 1;
  useEffect(() => {
    if (isPageBeyondResults) {
      onChange({ page: totalPages }, { replace: true });
    }
  }, [isPageBeyondResults, totalPages, onChange]);

  return (
    <div>
      <div className={styles.header}>
        <span className={styles.resultCount} aria-live="polite">
          {isLoading ? 'Searching…' : data ? `${data.totalCount} tutor${data.totalCount === 1 ? '' : 's'} found` : ''}
        </span>
        <div className={styles.sortRow}>
          <label htmlFor="sort-select" className={styles.sortLabel}>
            Sort by
          </label>
          <Select
            id="sort-select"
            value={filters.sort}
            onValueChange={(sort) => onChange({ sort })}
            options={SORT_OPTIONS}
          />
        </div>
      </div>

      {isError ? (
        <ErrorState
          title="We couldn't load tutors"
          description={
            error instanceof ApiError && error.isValidationError
              ? 'Some of your filters are invalid. Try clearing them and searching again.'
              : "We couldn't load tutors right now. Please try again."
          }
          onRetry={onRetry}
        />
      ) : isLoading && !data ? (
        <div className={styles.grid} aria-busy="true" aria-label="Loading tutors">
          {Array.from({ length: 6 }).map((_, i) => (
            <SkeletonCard key={i} />
          ))}
        </div>
      ) : isPageBeyondResults ? (
        <div className={styles.grid} aria-busy="true" aria-label="Loading tutors">
          {Array.from({ length: 6 }).map((_, i) => (
            <SkeletonCard key={i} />
          ))}
        </div>
      ) : data && data.items.length === 0 ? (
        <EmptyState
          title="No tutors match these filters"
          description="Try removing a filter or widening your price range — there may be more tutors than this view shows."
          action={<Button onClick={onClearAll}>Clear all filters</Button>}
        />
      ) : (
        <>
          <div className={styles.grid}>
            {data?.items.map((tutor) => (
              <TutorResultCard
                key={tutor.tutorUserId}
                tutor={tutor}
                isStudent={isStudent}
                isTutorOnlyViewer={isTutorOnlyViewer}
                isFavorited={favoritedTutorUserIds.has(tutor.tutorUserId)}
                onToggleFavorite={() => onToggleFavorite(tutor.tutorUserId)}
                onRequireAuth={onRequireAuth}
              />
            ))}
          </div>
          <div className={styles.paginationRow}>
            <Pagination page={filters.page} totalPages={totalPages} onPageChange={(page) => onChange({ page })} />
          </div>
        </>
      )}
    </div>
  );
}

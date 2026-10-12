'use client';

import { useEffect, useState } from 'react';
import { usePathname, useSearchParams } from 'next/navigation';
import styles from './FindTutorsView.module.css';
import { PageContainer } from '@/components/layout/PageContainer';
import { FormField } from '@/components/ui/FormField';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { Drawer } from '@/components/ui/Drawer';
import { Badge } from '@/components/ui/Badge';
import { useToast } from '@/components/ui/Toast';
import { AuthGateModal } from '@/components/auth/AuthGateModal';
import { useAuth } from '@/features/auth/useAuth';
import { FiltersPanel } from '@/features/search/components/FiltersPanel';
import { ActiveFilterChips } from '@/features/search/components/ActiveFilterChips';
import { SearchResultsList } from '@/features/search/components/SearchResultsList';
import { useSearchFilters } from '@/features/search/useSearchFilters';
import { useTutorSearch, useFavoriteStatuses, useFavoriteToggle } from '@/features/search/useTutorSearch';
import { countActiveFilters } from '@/features/search/filterSerialization';
import { SearchFilters } from '@/features/search/types';

export function FindTutorsView() {
  const { filters, updateFilters, resetFilters } = useSearchFilters();
  const { data, isLoading, isError, error, refetch } = useTutorSearch(filters);

  const { isAuthenticated, roles } = useAuth();
  const isStudent = isAuthenticated && roles.includes('Student');
  const isTutorOnlyViewer = isAuthenticated && roles.includes('Tutor') && !isStudent;

  const tutorUserIds = data?.items.map((item) => item.tutorUserId) ?? [];
  const { data: favoriteStatuses } = useFavoriteStatuses(tutorUserIds, isStudent);
  const favoritedTutorUserIds = new Set(favoriteStatuses?.favoritedTutorUserIds ?? []);
  const { add: addFavorite, remove: removeFavorite } = useFavoriteToggle();
  const { showToast } = useToast();

  const handleToggleFavorite = (tutorUserId: string) => {
    if (favoritedTutorUserIds.has(tutorUserId)) {
      removeFavorite.mutate(tutorUserId, { onSuccess: () => showToast('Removed from favorites') });
    } else {
      addFavorite.mutate(tutorUserId, { onSuccess: () => showToast('Added to favorites') });
    }
  };

  const [authGate, setAuthGate] = useState<{ open: boolean; action: string }>({ open: false, action: '' });
  const requireAuth = (action: string) => setAuthGate({ open: true, action });

  const pathname = usePathname();
  const searchParams = useSearchParams();
  const returnTo = searchParams.size > 0 ? `${pathname}?${searchParams.toString()}` : pathname;

  const [searchDraft, setSearchDraft] = useState(filters.q);
  // Adjust local draft state during render when the URL's `q` changes externally (back/forward
  // nav, Clear all) — the React-recommended alternative to a setState-in-effect sync
  // (https://react.dev/learn/you-might-not-need-an-effect#adjusting-some-state-when-a-prop-changes).
  const [prevUrlQ, setPrevUrlQ] = useState(filters.q);
  if (filters.q !== prevUrlQ) {
    setPrevUrlQ(filters.q);
    setSearchDraft(filters.q);
  }

  useEffect(() => {
    const timer = setTimeout(() => {
      if (searchDraft !== filters.q) {
        updateFilters({ q: searchDraft }, { replace: true });
      }
    }, 400);
    return () => clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchDraft]);

  const [mobileFiltersOpen, setMobileFiltersOpen] = useState(false);
  const [mobileDraft, setMobileDraft] = useState<SearchFilters>(filters);
  const openMobileFilters = () => {
    setMobileDraft(filters);
    setMobileFiltersOpen(true);
  };
  const applyMobileFilters = () => {
    updateFilters(mobileDraft);
    setMobileFiltersOpen(false);
  };

  const activeFilterCount = countActiveFilters(filters);

  return (
    <PageContainer className={styles.page}>
      <div className={styles.searchRow}>
        <FormField label="Search tutors by name or keyword" hideLabel>
          {(props) => (
            <Input
              {...props}
              type="search"
              placeholder="Search tutors by name or keyword…"
              value={searchDraft}
              onChange={(e) => setSearchDraft(e.target.value)}
            />
          )}
        </FormField>
      </div>

      <div className={styles.toolbar}>
        <div className={styles.chipsWrapper}>
          <ActiveFilterChips filters={filters} onChange={updateFilters} onClearAll={resetFilters} />
        </div>
        <Button
          variant="secondary"
          className={styles.mobileFiltersButton}
          onClick={openMobileFilters}
        >
          Filters {activeFilterCount > 0 && <Badge variant="info">{activeFilterCount}</Badge>}
        </Button>
      </div>

      <div className={styles.layout}>
        <aside className={styles.desktopFilters} aria-label="Filters">
          <FiltersPanel value={filters} onChange={updateFilters} />
        </aside>

        <SearchResultsList
          data={data}
          isLoading={isLoading}
          isError={isError}
          error={error}
          filters={filters}
          onChange={updateFilters}
          onClearAll={resetFilters}
          onRetry={() => refetch()}
          isStudent={isStudent}
          isTutorOnlyViewer={isTutorOnlyViewer}
          favoritedTutorUserIds={favoritedTutorUserIds}
          onToggleFavorite={handleToggleFavorite}
          onRequireAuth={requireAuth}
        />
      </div>

      <Drawer
        open={mobileFiltersOpen}
        onOpenChange={setMobileFiltersOpen}
        title="Filters"
        footer={
          <>
            <Button
              variant="secondary"
              fullWidth
              onClick={() => {
                resetFilters();
                setMobileFiltersOpen(false);
              }}
            >
              Clear all
            </Button>
            <Button fullWidth onClick={applyMobileFilters}>
              Show results
            </Button>
          </>
        }
      >
        <FiltersPanel value={mobileDraft} onChange={(partial) => setMobileDraft((prev) => ({ ...prev, ...partial }))} />
      </Drawer>

      <AuthGateModal
        open={authGate.open}
        onOpenChange={(open) => setAuthGate((prev) => ({ ...prev, open }))}
        action={authGate.action}
        returnTo={returnTo}
      />
    </PageContainer>
  );
}

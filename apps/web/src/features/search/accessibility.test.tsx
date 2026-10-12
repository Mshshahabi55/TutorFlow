import { render } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactElement } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { FiltersPanel } from './components/FiltersPanel';
import { SearchResultsList } from './components/SearchResultsList';
import { ActiveFilterChips } from './components/ActiveFilterChips';
import { Pagination } from '@/components/ui/Pagination';
import { TooltipProvider } from '@/components/ui/Tooltip';
import { DEFAULT_FILTERS, SearchTutorsResponse } from './types';

vi.mock('@/features/catalog/useCatalog', () => ({
  useSubjects: () => ({ data: [{ id: 's1', code: 'english', displayName: 'English' }], isLoading: false }),
  useSpecialties: () => ({
    data: [{ id: 'sp1', subjectId: 's1', code: 'conversation', displayName: 'Conversation practice' }],
    isLoading: false,
  }),
  useLanguages: () => ({ data: [{ code: 'en', displayName: 'English' }], isLoading: false }),
}));

function renderWithProviders(ui: ReactElement) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <TooltipProvider>{ui}</TooltipProvider>
    </QueryClientProvider>,
  );
}

/**
 * Minimal automated accessibility verification (axe-core via vitest-axe) for the Search
 * surface's own components, rendered in isolation — NOT a substitute for a real
 * browser/screen-reader pass (jsdom has no visual layout, so contrast/focus-order issues that
 * depend on actual rendering are out of reach here). This catches the class of defect axe can
 * detect statically: missing accessible names, invalid ARIA usage, label associations,
 * landmark/heading structure within the rendered subtree.
 */
describe('Search accessibility (axe)', () => {
  it('FiltersPanel has no detectable axe violations', async () => {
    const { container } = renderWithProviders(<FiltersPanel value={DEFAULT_FILTERS} onChange={vi.fn()} />);
    const results = await axe(container);
    expect(results.violations).toEqual([]);
  });

  it('ActiveFilterChips (with active filters) has no detectable axe violations', async () => {
    const { container } = renderWithProviders(
      <ActiveFilterChips
        filters={{ ...DEFAULT_FILTERS, subjectId: 's1', professionalTutor: true }}
        onChange={vi.fn()}
        onClearAll={vi.fn()}
      />,
    );
    const results = await axe(container);
    expect(results.violations).toEqual([]);
  });

  it('SearchResultsList (with results) has no detectable axe violations', async () => {
    const data: SearchTutorsResponse = {
      items: [
        {
          tutorUserId: '11111111-1111-1111-1111-111111111111',
          slug: 'maria-garcia-11111111',
          displayName: 'Maria Garcia',
          headline: 'Certified Spanish teacher',
          photoUrl: null,
          subjectIds: ['s1'],
          specialtyIds: ['sp1'],
          spokenLanguages: [{ languageCode: 'es', proficiency: 'Native' }],
          countryOfBirthCode: 'ES',
          isProfessionalTutor: true,
          isSuperTutor: false,
          hourlyRateMinorUnits: 2500,
          currency: 'EUR',
          averageRating: 4.8,
          reviewCount: 42,
          lessonCount: 120,
          hasUpcomingAvailability: true,
          isAcceptingNewStudents: true,
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    };
    const { container } = renderWithProviders(
      <SearchResultsList
        data={data}
        isLoading={false}
        isError={false}
        error={null}
        filters={DEFAULT_FILTERS}
        onChange={vi.fn()}
        onClearAll={vi.fn()}
        onRetry={vi.fn()}
        isStudent={false}
        isTutorOnlyViewer={false}
        favoritedTutorUserIds={new Set()}
        onToggleFavorite={vi.fn()}
        onRequireAuth={vi.fn()}
      />,
    );
    const results = await axe(container);
    expect(results.violations).toEqual([]);
  });

  it('Pagination has no detectable axe violations', async () => {
    const { container } = render(<Pagination page={3} totalPages={10} onPageChange={vi.fn()} />);
    const results = await axe(container);
    expect(results.violations).toEqual([]);
  });
});

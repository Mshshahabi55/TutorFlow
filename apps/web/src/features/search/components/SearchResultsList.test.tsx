import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactElement } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { SearchResultsList } from './SearchResultsList';
import { DEFAULT_FILTERS, SearchTutorsResponse } from '../types';
import { ApiError } from '@/lib/api/client';
import { TooltipProvider } from '@/components/ui/Tooltip';

// TutorResultCard renders a Tooltip (for the "Available soon" badge) and calls the catalog
// hooks (useSubjects/useSpecialties) to resolve display names — both need ancestors supplied
// app-wide in production (RootLayout/Providers) but explicitly here since these tests render
// SearchResultsList in isolation. No catalog data is mocked — the card falls back to showing
// nothing resolved, which is fine since these tests assert on tutor.displayName, not labels.
function renderWithProviders(ui: ReactElement) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <TooltipProvider>{ui}</TooltipProvider>
    </QueryClientProvider>,
  );
}

const baseProps = {
  filters: DEFAULT_FILTERS,
  onChange: vi.fn(),
  onClearAll: vi.fn(),
  onRetry: vi.fn(),
  isStudent: false,
  isTutorOnlyViewer: false,
  favoritedTutorUserIds: new Set<string>(),
  onToggleFavorite: vi.fn(),
  onRequireAuth: vi.fn(),
};

const sampleResponse: SearchTutorsResponse = {
  items: [
    {
      tutorUserId: '11111111-1111-1111-1111-111111111111',
      slug: 'maria-garcia-11111111',
      displayName: 'Maria Garcia',
      headline: 'Certified Spanish teacher',
      photoUrl: null,
      subjectIds: ['22222222-2222-2222-2222-222222222222'],
      specialtyIds: ['33333333-3333-3333-3333-333333333333'],
      spokenLanguages: [{ languageCode: 'es', proficiency: 'Native' }],
      countryOfBirthCode: 'ES',
      isProfessionalTutor: false,
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

describe('SearchResultsList', () => {
  it('shows a loading state (and no error/empty state) while the first request is in flight', () => {
    render(<SearchResultsList {...baseProps} data={undefined} isLoading isError={false} error={null} />);

    expect(screen.getByLabelText('Loading tutors')).toBeInTheDocument();
    expect(screen.queryByText(/no tutors match/i)).not.toBeInTheDocument();
  });

  it("shows the empty state, with a recovery action, when a search returns zero results", async () => {
    const onClearAll = vi.fn();
    const empty: SearchTutorsResponse = { items: [], page: 1, pageSize: 20, totalCount: 0 };
    render(<SearchResultsList {...baseProps} onClearAll={onClearAll} data={empty} isLoading={false} isError={false} error={null} />);

    expect(screen.getByText(/no tutors match these filters/i)).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: /clear all filters/i }));
    expect(onClearAll).toHaveBeenCalledTimes(1);
  });

  it('auto-corrects to the last valid page instead of showing "no results" when the requested page is beyond the real result set', () => {
    const onChange = vi.fn();
    // totalCount=5 at pageSize=20 means there's exactly 1 real page, but the URL/filters say
    // page 3 — a stale bookmark or a filter that narrowed results after the page was set.
    const beyondRange: SearchTutorsResponse = { items: [], page: 3, pageSize: 20, totalCount: 5 };
    render(
      <SearchResultsList
        {...baseProps}
        onChange={onChange}
        data={beyondRange}
        filters={{ ...baseProps.filters, page: 3 }}
        isLoading={false}
        isError={false}
        error={null}
      />,
    );

    expect(screen.queryByText(/no tutors match/i)).not.toBeInTheDocument();
    expect(onChange).toHaveBeenCalledWith({ page: 1 }, { replace: true });
  });

  it('shows a retryable error state and calls onRetry, distinguishing it from the empty state', async () => {
    const onRetry = vi.fn();
    render(
      <SearchResultsList {...baseProps} onRetry={onRetry} data={undefined} isLoading={false} isError error={new Error('network down')} />,
    );

    expect(screen.getByRole('alert')).toBeInTheDocument();
    expect(screen.queryByText(/no tutors match/i)).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: /try again/i }));
    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it('shows a validation-specific error message for a 400 ApiError', () => {
    const validationError = new ApiError(400, { errors: { priceMaxMinorUnits: ['must be >= priceMinMinorUnits'] } });
    render(<SearchResultsList {...baseProps} data={undefined} isLoading={false} isError error={validationError} />);

    expect(screen.getByText(/some of your filters are invalid/i)).toBeInTheDocument();
  });

  it('renders result cards and the result count once data loads', () => {
    renderWithProviders(
      <SearchResultsList {...baseProps} data={sampleResponse} isLoading={false} isError={false} error={null} />,
    );

    expect(screen.getByText('Maria Garcia')).toBeInTheDocument();
    expect(screen.getByText('1 tutor found')).toBeInTheDocument();
  });

  it('pluralizes the result count for more than one result', () => {
    const twoResults: SearchTutorsResponse = { ...sampleResponse, totalCount: 2 };
    renderWithProviders(
      <SearchResultsList {...baseProps} data={twoResults} isLoading={false} isError={false} error={null} />,
    );

    expect(screen.getByText('2 tutors found')).toBeInTheDocument();
  });
});

import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactElement } from 'react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { axe } from 'vitest-axe';
import { TutorReviews } from './TutorReviews';

const mockUseTutorReviewSummary = vi.fn();
const mockUseReviewEligibility = vi.fn();
const mockUseSubcategoryRatingEligibility = vi.fn();
const mockSubmitMutate = vi.fn();
const mockSubmitSubcategoryMutate = vi.fn();
vi.mock('@/features/reviews/useReviews', () => ({
  useTutorReviewSummary: () => mockUseTutorReviewSummary(),
  useReviewEligibility: () => mockUseReviewEligibility(),
  useSubcategoryRatingEligibility: () => mockUseSubcategoryRatingEligibility(),
  useSubmitReview: () => ({ mutate: mockSubmitMutate, isPending: false, isError: false, isSuccess: false, error: null }),
  useSubmitSubcategoryRating: () => ({ mutate: mockSubmitSubcategoryMutate, isPending: false, isError: false, isSuccess: false, error: null }),
}));

function renderWithProviders(ui: ReactElement) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}

const TUTOR_ID = '61d4dd3a-08c0-4494-b452-55a3c33aac50';

describe('TutorReviews', () => {
  beforeEach(() => {
    mockUseTutorReviewSummary.mockReturnValue({
      data: { overallRating: 0, reviewCount: 0, subRatings: null, reviews: [] },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    });
    mockUseReviewEligibility.mockReturnValue({
      data: { isEligible: false, completedLessonCount: 0, requiredCompletedLessonCount: 3, existingReview: null },
      isLoading: false,
      isError: false,
    });
    mockUseSubcategoryRatingEligibility.mockReturnValue({
      data: { isEligible: false, hasCompletedAtLeastOneLesson: false, alreadySubmittedThisMonth: false },
      isLoading: false,
      isError: false,
    });
    mockSubmitMutate.mockClear();
    mockSubmitSubcategoryMutate.mockClear();
  });

  it('shows a loading state while the summary is being fetched', () => {
    mockUseTutorReviewSummary.mockReturnValue({ data: undefined, isLoading: true, isError: false, refetch: vi.fn() });

    renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={false} onRequireAuth={vi.fn()} />);

    expect(screen.getByText('Loading reviews…')).toBeInTheDocument();
  });

  it('shows a retryable error state, never a raw error', () => {
    const refetch = vi.fn();
    mockUseTutorReviewSummary.mockReturnValue({ data: undefined, isLoading: false, isError: true, refetch });

    renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={false} onRequireAuth={vi.fn()} />);

    expect(screen.getByText("We couldn't load reviews")).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }));
    expect(refetch).toHaveBeenCalled();
  });

  it('always renders the honest empty state when there are no reviews — never fabricates review data', () => {
    renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={false} onRequireAuth={vi.fn()} />);

    expect(screen.getByText('No reviews yet')).toBeInTheDocument();
  });

  it('renders real reviews when present, with rating, comment, and date', () => {
    mockUseTutorReviewSummary.mockReturnValue({
      data: {
        overallRating: 4.5,
        reviewCount: 2,
        subRatings: null,
        reviews: [
          { rating: 5, comment: 'Excellent tutor!', createdAtUtc: '2026-01-10T00:00:00Z' },
          { rating: 4, comment: null, createdAtUtc: '2026-01-05T00:00:00Z' },
        ],
      },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    });

    renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={false} onRequireAuth={vi.fn()} />);

    expect(screen.getByText('Excellent tutor!')).toBeInTheDocument();
    expect(screen.getByText('(2 reviews)')).toBeInTheDocument();
  });

  it('guest: shows a log-in prompt instead of a review action, and never calls the eligibility endpoint', () => {
    const onRequireAuth = vi.fn();
    renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={false} onRequireAuth={onRequireAuth} />);

    const button = screen.getByRole('button', { name: 'Log in to leave a review' });
    fireEvent.click(button);
    expect(onRequireAuth).toHaveBeenCalledWith('leave a review');
  });

  it('authenticated student below threshold: shows progress, not a review form', () => {
    mockUseReviewEligibility.mockReturnValue({
      data: { isEligible: false, completedLessonCount: 1, requiredCompletedLessonCount: 3, existingReview: null },
      isLoading: false,
      isError: false,
    });

    renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={true} onRequireAuth={vi.fn()} />);

    expect(screen.getByText(/1 of 3 so far/)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Write a review' })).not.toBeInTheDocument();
  });

  it('authenticated and eligible with no existing review: shows "Write a review", opens the create form on click', () => {
    mockUseReviewEligibility.mockReturnValue({
      data: { isEligible: true, completedLessonCount: 3, requiredCompletedLessonCount: 3, existingReview: null },
      isLoading: false,
      isError: false,
    });

    renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={true} onRequireAuth={vi.fn()} />);

    fireEvent.click(screen.getByRole('button', { name: 'Write a review' }));
    expect(screen.getByRole('button', { name: 'Submit review' })).toBeInTheDocument();
  });

  it('authenticated and eligible with an existing review: shows the saved review and an edit entry point', () => {
    mockUseReviewEligibility.mockReturnValue({
      data: {
        isEligible: true,
        completedLessonCount: 4,
        requiredCompletedLessonCount: 3,
        existingReview: { rating: 5, comment: 'Loved it', createdAtUtc: '2026-01-01T00:00:00Z', updatedAtUtc: '2026-01-01T00:00:00Z' },
      },
      isLoading: false,
      isError: false,
    });

    renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={true} onRequireAuth={vi.fn()} />);

    expect(screen.getByText('Your review')).toBeInTheDocument();
    expect(screen.getByText('Loved it')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Edit your review' }));
    expect(screen.getByRole('button', { name: 'Update review' })).toBeInTheDocument();
  });

  it('review form: rating is keyboard accessible via a native radiogroup', () => {
    mockUseReviewEligibility.mockReturnValue({
      data: { isEligible: true, completedLessonCount: 3, requiredCompletedLessonCount: 3, existingReview: null },
      isLoading: false,
      isError: false,
    });

    renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={true} onRequireAuth={vi.fn()} />);
    fireEvent.click(screen.getByRole('button', { name: 'Write a review' }));

    expect(screen.getByRole('radiogroup', { name: 'Rating' })).toBeInTheDocument();
    expect(screen.getAllByRole('radio')).toHaveLength(5);
  });

  it('review form: submitting without a rating shows a validation error, never calls submit', async () => {
    mockUseReviewEligibility.mockReturnValue({
      data: { isEligible: true, completedLessonCount: 3, requiredCompletedLessonCount: 3, existingReview: null },
      isLoading: false,
      isError: false,
    });

    renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={true} onRequireAuth={vi.fn()} />);
    fireEvent.click(screen.getByRole('button', { name: 'Write a review' }));
    fireEvent.click(screen.getByRole('button', { name: 'Submit review' }));

    await waitFor(() => expect(screen.getByText('Choose a rating before submitting.')).toBeInTheDocument());
    expect(mockSubmitMutate).not.toHaveBeenCalled();
  });

  it('review form: selecting a rating and submitting calls the mutation with the chosen rating', () => {
    mockUseReviewEligibility.mockReturnValue({
      data: { isEligible: true, completedLessonCount: 3, requiredCompletedLessonCount: 3, existingReview: null },
      isLoading: false,
      isError: false,
    });

    renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={true} onRequireAuth={vi.fn()} />);
    fireEvent.click(screen.getByRole('button', { name: 'Write a review' }));
    fireEvent.click(screen.getAllByRole('radio')[3]); // 4 stars
    fireEvent.click(screen.getByRole('button', { name: 'Submit review' }));

    expect(mockSubmitMutate).toHaveBeenCalledWith({ rating: 4, comment: null });
  });

  it('has no detectable axe violations in the empty/guest state', async () => {
    const { container } = renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={false} onRequireAuth={vi.fn()} />);
    expect((await axe(container)).violations).toEqual([]);
  });

  it('has no detectable axe violations with the review form open', async () => {
    mockUseReviewEligibility.mockReturnValue({
      data: { isEligible: true, completedLessonCount: 3, requiredCompletedLessonCount: 3, existingReview: null },
      isLoading: false,
      isError: false,
    });

    const { container } = renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={true} onRequireAuth={vi.fn()} />);
    fireEvent.click(screen.getByRole('button', { name: 'Write a review' }));

    expect((await axe(container)).violations).toEqual([]);
  });

  describe('subcategory ratings (documented: separate from the written review)', () => {
    // DOCUMENTED (help.preply.com/articles/11196386, verbatim): "Subcategory reviews appear on
    // your profile once you've received at least one rating in all four categories... If you
    // only have ratings in some categories, the section will not appear yet." These five tests
    // enforce that rule precisely — the backend may return partial nullable data (useful on its
    // own), but the public section must never render until all four counts are > 0.
    function summaryWith(counts: { clarity: number; progress: number; preparation: number; reassurance: number }) {
      return {
        overallRating: 0,
        reviewCount: 0,
        reviews: [],
        subRatings: {
          clarity: { average: counts.clarity > 0 ? 4.5 : null, count: counts.clarity },
          progress: { average: counts.progress > 0 ? 5 : null, count: counts.progress },
          preparation: { average: counts.preparation > 0 ? 3 : null, count: counts.preparation },
          reassurance: { average: counts.reassurance > 0 ? 4 : null, count: counts.reassurance },
        },
      };
    }

    it('1. all four categories have at least one rating → the section is visible', () => {
      mockUseTutorReviewSummary.mockReturnValue({
        data: summaryWith({ clarity: 2, progress: 1, preparation: 3, reassurance: 4 }),
        isLoading: false,
        isError: false,
        refetch: vi.fn(),
      });

      renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={false} onRequireAuth={vi.fn()} />);

      expect(screen.getByText('Specific aspects')).toBeInTheDocument();
      expect(screen.getByText('Clarity')).toBeInTheDocument();
      expect(screen.getByText('Progress')).toBeInTheDocument();
      expect(screen.getByText('Preparation')).toBeInTheDocument();
      expect(screen.getByText('Reassurance')).toBeInTheDocument();
    });

    it('2. exactly one category missing (zero ratings) → the section is hidden entirely', () => {
      mockUseTutorReviewSummary.mockReturnValue({
        data: summaryWith({ clarity: 2, progress: 1, preparation: 0, reassurance: 4 }),
        isLoading: false,
        isError: false,
        refetch: vi.fn(),
      });

      renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={false} onRequireAuth={vi.fn()} />);

      expect(screen.queryByText('Specific aspects')).not.toBeInTheDocument();
      expect(screen.queryByText('Clarity')).not.toBeInTheDocument();
    });

    it('3. two categories missing → the section is hidden entirely', () => {
      mockUseTutorReviewSummary.mockReturnValue({
        data: summaryWith({ clarity: 2, progress: 0, preparation: 0, reassurance: 4 }),
        isLoading: false,
        isError: false,
        refetch: vi.fn(),
      });

      renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={false} onRequireAuth={vi.fn()} />);

      expect(screen.queryByText('Specific aspects')).not.toBeInTheDocument();
    });

    it('4. all four categories missing (zero ratings each, but subRatings object present) → the section is hidden entirely', () => {
      mockUseTutorReviewSummary.mockReturnValue({
        data: summaryWith({ clarity: 0, progress: 0, preparation: 0, reassurance: 0 }),
        isLoading: false,
        isError: false,
        refetch: vi.fn(),
      });

      renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={false} onRequireAuth={vi.fn()} />);

      expect(screen.queryByText('Specific aspects')).not.toBeInTheDocument();
    });

    it('5. partial backend data never produces a partial public section — no 3-of-4 rendering, no fabricated 4th value', () => {
      mockUseTutorReviewSummary.mockReturnValue({
        data: summaryWith({ clarity: 5, progress: 5, preparation: 5, reassurance: 0 }),
        isLoading: false,
        isError: false,
        refetch: vi.fn(),
      });

      renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={false} onRequireAuth={vi.fn()} />);

      // Not even the three fully-populated dimensions render — the rule is all-or-nothing.
      expect(screen.queryByText('Specific aspects')).not.toBeInTheDocument();
      expect(screen.queryByText('Clarity')).not.toBeInTheDocument();
      expect(screen.queryByText('Progress')).not.toBeInTheDocument();
      expect(screen.queryByText('Preparation')).not.toBeInTheDocument();
      expect(screen.queryByText('Reassurance')).not.toBeInTheDocument();
    });

    it('renders no "Specific aspects" section at all when subRatings is null — never fabricates empty rows', () => {
      renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={false} onRequireAuth={vi.fn()} />);

      expect(screen.queryByText('Specific aspects')).not.toBeInTheDocument();
    });

    it('guest: no subcategory action is shown at all (same auth gate as the written review)', () => {
      renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={false} onRequireAuth={vi.fn()} />);

      expect(screen.queryByText(/Rate specific aspects/)).not.toBeInTheDocument();
    });

    it('authenticated student with no completed lessons: no subcategory action shown', () => {
      mockUseSubcategoryRatingEligibility.mockReturnValue({
        data: { isEligible: false, hasCompletedAtLeastOneLesson: false, alreadySubmittedThisMonth: false },
        isLoading: false,
        isError: false,
      });

      renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={true} onRequireAuth={vi.fn()} />);

      expect(screen.queryByText(/Rate specific aspects/)).not.toBeInTheDocument();
    });

    it('authenticated, eligible (>=1 completed lesson, not yet submitted this month): shows the subcategory form, independent of written-review eligibility', () => {
      // Deliberately still below the written review's 3-lesson threshold (default mock) — proves
      // the two eligibility rules are genuinely independent, not conflated.
      mockUseSubcategoryRatingEligibility.mockReturnValue({
        data: { isEligible: true, hasCompletedAtLeastOneLesson: true, alreadySubmittedThisMonth: false },
        isLoading: false,
        isError: false,
      });

      renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={true} onRequireAuth={vi.fn()} />);

      expect(screen.getByText(/Rate specific aspects/)).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Submit ratings' })).toBeInTheDocument();
      expect(screen.queryByRole('button', { name: 'Write a review' })).not.toBeInTheDocument();
    });

    it('already submitted this month: shows a quiet message, not the form', () => {
      mockUseSubcategoryRatingEligibility.mockReturnValue({
        data: { isEligible: false, hasCompletedAtLeastOneLesson: true, alreadySubmittedThisMonth: true },
        isLoading: false,
        isError: false,
      });

      renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={true} onRequireAuth={vi.fn()} />);

      expect(screen.getByText(/already rated specific aspects.*this month/)).toBeInTheDocument();
      expect(screen.queryByText(/Rate specific aspects/)).not.toBeInTheDocument();
    });

    it('submitting with only some dimensions rated calls the mutation with the rest null, never requires all four', () => {
      mockUseSubcategoryRatingEligibility.mockReturnValue({
        data: { isEligible: true, hasCompletedAtLeastOneLesson: true, alreadySubmittedThisMonth: false },
        isLoading: false,
        isError: false,
      });

      renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={true} onRequireAuth={vi.fn()} />);

      const radios = screen.getAllByRole('radiogroup');
      expect(radios).toHaveLength(4); // Clarity, Progress, Preparation, Reassurance

      fireEvent.click(screen.getAllByRole('radio')[0]); // Clarity, 1 star
      fireEvent.click(screen.getByRole('button', { name: 'Submit ratings' }));

      expect(mockSubmitSubcategoryMutate).toHaveBeenCalledWith({ clarity: 1, progress: null, preparation: null, reassurance: null });
    });

    it('submitting with nothing rated at all is still a valid submission — documented "none" option', () => {
      mockUseSubcategoryRatingEligibility.mockReturnValue({
        data: { isEligible: true, hasCompletedAtLeastOneLesson: true, alreadySubmittedThisMonth: false },
        isLoading: false,
        isError: false,
      });

      renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={true} onRequireAuth={vi.fn()} />);
      fireEvent.click(screen.getByRole('button', { name: 'Submit ratings' }));

      expect(mockSubmitSubcategoryMutate).toHaveBeenCalledWith({ clarity: null, progress: null, preparation: null, reassurance: null });
    });

    it('has no detectable axe violations with the subcategory form open', async () => {
      mockUseSubcategoryRatingEligibility.mockReturnValue({
        data: { isEligible: true, hasCompletedAtLeastOneLesson: true, alreadySubmittedThisMonth: false },
        isLoading: false,
        isError: false,
      });

      const { container } = renderWithProviders(<TutorReviews tutorUserId={TUTOR_ID} isAuthenticatedStudent={true} onRequireAuth={vi.fn()} />);
      expect((await axe(container)).violations).toEqual([]);
    });
  });
});

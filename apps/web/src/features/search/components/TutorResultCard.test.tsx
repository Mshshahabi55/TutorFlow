import { fireEvent, render, screen } from '@testing-library/react';
import type { ReactElement } from 'react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { axe } from 'vitest-axe';
import { TooltipProvider } from '@/components/ui/Tooltip';
import { TutorResultCard } from './TutorResultCard';
import { TutorSearchResult } from '../types';

vi.mock('@/features/catalog/useCatalog', () => ({
  useSubjects: () => ({ data: [], isLoading: false }),
  useSpecialties: () => ({ data: [], isLoading: false }),
}));

function renderWithProviders(ui: ReactElement) {
  return render(<TooltipProvider>{ui}</TooltipProvider>);
}

const tutor: TutorSearchResult = {
  tutorUserId: 't1',
  slug: 'jane-dev',
  displayName: 'Jane Dev',
  headline: 'Friendly tutor',
  photoUrl: null,
  subjectIds: [],
  specialtyIds: [],
  spokenLanguages: [],
  countryOfBirthCode: null,
  isProfessionalTutor: false,
  isSuperTutor: false,
  hourlyRateMinorUnits: 2500,
  currency: 'USD',
  averageRating: 4.5,
  reviewCount: 10,
  lessonCount: 20,
  hasUpcomingAvailability: true,
  isAcceptingNewStudents: true,
};

const baseProps = {
  tutor,
  isStudent: false,
  isTutorOnlyViewer: false,
  isFavorited: false,
  onToggleFavorite: vi.fn(),
  onRequireAuth: vi.fn(),
};

describe('TutorResultCard', () => {
  beforeEach(() => {
    baseProps.onToggleFavorite = vi.fn();
    baseProps.onRequireAuth = vi.fn();
  });

  it('guest: clicking Book a trial lesson opens the auth gate instead of navigating', () => {
    renderWithProviders(<TutorResultCard {...baseProps} />);

    fireEvent.click(screen.getByRole('link', { name: 'Book a trial lesson' }));

    expect(baseProps.onRequireAuth).toHaveBeenCalledWith('book a trial lesson');
  });

  it('guest: clicking Message opens the auth gate', () => {
    renderWithProviders(<TutorResultCard {...baseProps} />);

    fireEvent.click(screen.getByRole('button', { name: 'Message' }));

    expect(baseProps.onRequireAuth).toHaveBeenCalledWith('message this tutor');
  });

  it('guest: clicking the favorite heart opens the auth gate, not a real favorite call', () => {
    renderWithProviders(<TutorResultCard {...baseProps} />);

    fireEvent.click(screen.getByRole('button', { name: 'Add to favorites' }));

    expect(baseProps.onRequireAuth).toHaveBeenCalledWith('favorite this tutor');
    expect(baseProps.onToggleFavorite).not.toHaveBeenCalled();
  });

  it('authenticated student: Message is a real link to /messages, no auth gate', () => {
    renderWithProviders(<TutorResultCard {...baseProps} isStudent />);

    expect(screen.getByRole('link', { name: 'Message' })).toHaveAttribute('href', '/messages');
  });

  it('authenticated student: Book a trial lesson links to the profile availability anchor', () => {
    renderWithProviders(<TutorResultCard {...baseProps} isStudent />);

    expect(screen.getByRole('link', { name: 'Book a trial lesson' })).toHaveAttribute(
      'href',
      '/tutors/jane-dev#tutor-availability',
    );
  });

  it('authenticated student: clicking the heart calls onToggleFavorite directly', () => {
    renderWithProviders(<TutorResultCard {...baseProps} isStudent />);

    fireEvent.click(screen.getByRole('button', { name: 'Add to favorites' }));

    expect(baseProps.onToggleFavorite).toHaveBeenCalledTimes(1);
    expect(baseProps.onRequireAuth).not.toHaveBeenCalled();
  });

  it('authenticated student: already-favorited shows a filled heart and "Remove from favorites"', () => {
    renderWithProviders(<TutorResultCard {...baseProps} isStudent isFavorited />);

    expect(screen.getByRole('button', { name: 'Remove from favorites' })).toBeInTheDocument();
  });

  it('not accepting new students: Book Trial is disabled, never a live link', () => {
    renderWithProviders(
      <TutorResultCard {...baseProps} isStudent tutor={{ ...tutor, isAcceptingNewStudents: false }} />,
    );

    expect(screen.getByRole('button', { name: 'Not accepting new students' })).toBeDisabled();
    expect(screen.queryByRole('link', { name: 'Book a trial lesson' })).not.toBeInTheDocument();
  });

  it('tutor-only viewer (no Student capability): no action row at all, only View profile', () => {
    renderWithProviders(<TutorResultCard {...baseProps} isTutorOnlyViewer />);

    expect(screen.queryByRole('link', { name: 'Book a trial lesson' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Message' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /favorites/i })).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'View profile' })).toBeInTheDocument();
  });

  it('dual-role viewer (Student + Tutor, ADR-0002): Student actions still render', () => {
    renderWithProviders(<TutorResultCard {...baseProps} isStudent isTutorOnlyViewer={false} />);

    expect(screen.getByRole('link', { name: 'Message' })).toHaveAttribute('href', '/messages');
    expect(screen.getByRole('link', { name: 'Book a trial lesson' })).toBeInTheDocument();
  });

  it('has no detectable axe violations for the guest state', async () => {
    const { container } = renderWithProviders(<TutorResultCard {...baseProps} />);
    expect((await axe(container)).violations).toEqual([]);
  });
});

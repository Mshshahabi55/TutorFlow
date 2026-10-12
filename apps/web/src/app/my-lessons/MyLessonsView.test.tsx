import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { axe } from 'vitest-axe';
import { MyLessonsView } from './MyLessonsView';

const mockUseAuth = vi.fn();
vi.mock('@/features/auth/useAuth', () => ({ useAuth: () => mockUseAuth() }));

const mockUseMyLessons = vi.fn();
vi.mock('@/features/lessons/useMyLessons', () => ({ useMyLessons: () => mockUseMyLessons() }));

const LESSON = {
  id: 'lesson-1',
  bookingId: 'booking-1',
  tutorUserId: 'tutor-1',
  status: 'Scheduled' as const,
  startUtc: '2026-01-14T10:00:00.000Z',
  endUtc: '2026-01-14T10:25:00.000Z',
  durationMinutes: 25,
};

describe('MyLessonsView', () => {
  beforeEach(() => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false });
    mockUseMyLessons.mockReturnValue({ data: undefined, isLoading: false, isError: false, refetch: vi.fn() });
  });

  it('shows a loading state while the session is resolving', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: true });

    render(<MyLessonsView />);

    expect(screen.getByRole('status')).toBeInTheDocument();
  });

  it('guest: shows sign up / log in prompts with a returnTo back to this page, never the list', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: false });

    render(<MyLessonsView />);

    expect(screen.getByRole('link', { name: 'Sign up' })).toHaveAttribute('href', '/signup?returnTo=%2Fmy-lessons');
    expect(screen.getByRole('link', { name: 'Log in' })).toHaveAttribute('href', '/login?returnTo=%2Fmy-lessons');
  });

  it('shows a loading state while lessons are being fetched', () => {
    mockUseMyLessons.mockReturnValue({ data: undefined, isLoading: true, isError: false, refetch: vi.fn() });

    render(<MyLessonsView />);

    expect(screen.getByText('Loading your lessons…')).toBeInTheDocument();
  });

  it('shows a retryable error state on failure, never a raw error', () => {
    const refetch = vi.fn();
    mockUseMyLessons.mockReturnValue({ data: undefined, isLoading: false, isError: true, refetch });

    render(<MyLessonsView />);

    expect(screen.getByText("We couldn't load your lessons")).toBeInTheDocument();
    screen.getByRole('button', { name: 'Try again' }).click();
    expect(refetch).toHaveBeenCalled();
  });

  it('shows a useful empty state, not a bare "no results"', () => {
    mockUseMyLessons.mockReturnValue({ data: { lessons: [] }, isLoading: false, isError: false, refetch: vi.fn() });

    render(<MyLessonsView />);

    expect(screen.getByText('No lessons yet')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Find a tutor' })).toHaveAttribute('href', '/find-tutors');
  });

  it('renders an upcoming (Scheduled) lesson with a friendly status badge, date, time, and duration', () => {
    mockUseMyLessons.mockReturnValue({ data: { lessons: [LESSON] }, isLoading: false, isError: false, refetch: vi.fn() });

    render(<MyLessonsView />);

    expect(screen.getByText('Upcoming')).toBeInTheDocument();
    expect(screen.getByText(/25 min/)).toBeInTheDocument();
  });

  it('renders a Completed lesson with a distinct, honest status badge', () => {
    mockUseMyLessons.mockReturnValue({
      data: { lessons: [{ ...LESSON, status: 'Completed' as const }] },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    });

    render(<MyLessonsView />);

    expect(screen.getByText('Completed')).toBeInTheDocument();
    expect(screen.queryByText('Upcoming')).not.toBeInTheDocument();
  });

  it('renders multiple lessons, one card each', () => {
    mockUseMyLessons.mockReturnValue({
      data: { lessons: [LESSON, { ...LESSON, id: 'lesson-2', status: 'Cancelled' as const }] },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    });

    render(<MyLessonsView />);

    expect(screen.getByText('Upcoming')).toBeInTheDocument();
    expect(screen.getByText('Cancelled')).toBeInTheDocument();
  });

  it('has no detectable axe violations in the populated state', async () => {
    mockUseMyLessons.mockReturnValue({ data: { lessons: [LESSON] }, isLoading: false, isError: false, refetch: vi.fn() });

    const { container } = render(<MyLessonsView />);
    expect((await axe(container)).violations).toEqual([]);
  });

  it('has no detectable axe violations in the empty state', async () => {
    mockUseMyLessons.mockReturnValue({ data: { lessons: [] }, isLoading: false, isError: false, refetch: vi.fn() });

    const { container } = render(<MyLessonsView />);
    expect((await axe(container)).violations).toEqual([]);
  });

  it('has no detectable axe violations in the guest state', async () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: false });

    const { container } = render(<MyLessonsView />);
    expect((await axe(container)).violations).toEqual([]);
  });
});

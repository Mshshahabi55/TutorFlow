import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ReactElement } from 'react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { axe } from 'vitest-axe';
import { TooltipProvider } from '@/components/ui/Tooltip';
import { BookingPanel } from './BookingPanel';
import { TutorPublicProfile } from '../types';

function renderWithProviders(ui: ReactElement) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <TooltipProvider>{ui}</TooltipProvider>
    </QueryClientProvider>,
  );
}

const mockUseAuth = vi.fn();
vi.mock('@/features/auth/useAuth', () => ({ useAuth: () => mockUseAuth() }));

const mockPush = vi.fn();
vi.mock('next/navigation', () => ({ useRouter: () => ({ push: mockPush }) }));

const mockCreateOrGetConversation = vi.fn();
vi.mock('@/features/messaging/api', () => ({
  createOrGetConversation: (tutorUserId: string) => mockCreateOrGetConversation(tutorUserId),
}));

const mockAvailability = vi.fn();
const mockFavoriteStatus = vi.fn();
const mockTrialEligibility = vi.fn();
const mockAdd = vi.fn();
const mockRemove = vi.fn();
vi.mock('../useTutorProfile', () => ({
  useAvailabilityPreview: () => mockAvailability(),
  useFavoriteStatus: () => mockFavoriteStatus(),
  useTrialEligibility: () => mockTrialEligibility(),
  useFavoriteMutations: () => ({ add: { mutate: mockAdd }, remove: { mutate: mockRemove } }),
}));

vi.mock('@/components/ui/Toast', () => ({ useToast: () => ({ showToast: vi.fn() }) }));

const profile: TutorPublicProfile = {
  tutorUserId: 't1',
  slug: 'jane-dev',
  displayName: 'Jane Dev',
  headline: null,
  about: null,
  profilePhotoReference: null,
  introVideoReference: null,
  spokenLanguages: [],
  subjectIds: [],
  specialtyIds: [],
  countryOfBirthCode: null,
  isProfessionalTutor: false,
  isSuperTutor: false,
  yearsOfExperience: null,
  experienceSummary: null,
  educationEntries: [],
  certificateEntries: [],
  hourlyRateAmountMinorUnits: 2500,
  hourlyRateCurrency: 'USD',
  isAcceptingNewStudents: true,
};

describe('BookingPanel', () => {
  beforeEach(() => {
    mockAvailability.mockReturnValue({ data: { hasUpcomingAvailability: true, nextAvailableStartUtc: null, timeZoneId: 'UTC' }, isLoading: false });
    mockFavoriteStatus.mockReturnValue({ data: { isFavorited: false }, isLoading: false });
    mockTrialEligibility.mockReturnValue({ data: { hasUsedTrial: false }, isLoading: false });
    mockAdd.mockClear();
    mockRemove.mockClear();
    mockPush.mockClear();
    mockCreateOrGetConversation.mockReset();
  });

  it('guest: clicking Book a trial lesson opens the auth gate instead of navigating', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, userId: null, roles: [] });
    const onRequireAuth = vi.fn();
    renderWithProviders(<BookingPanel profile={profile} onRequireAuth={onRequireAuth} />);

    fireEvent.click(screen.getByRole('link', { name: 'Book a trial lesson' }));

    expect(onRequireAuth).toHaveBeenCalledWith('book a trial lesson');
  });

  it('guest: clicking Message opens the auth gate', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, userId: null, roles: [] });
    const onRequireAuth = vi.fn();
    renderWithProviders(<BookingPanel profile={profile} onRequireAuth={onRequireAuth} />);

    fireEvent.click(screen.getByRole('button', { name: 'Message' }));

    expect(onRequireAuth).toHaveBeenCalledWith('message this tutor');
  });

  it('guest: clicking the favorite heart opens the auth gate, not a real favorite call', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, userId: null, roles: [] });
    const onRequireAuth = vi.fn();
    renderWithProviders(<BookingPanel profile={profile} onRequireAuth={onRequireAuth} />);

    fireEvent.click(screen.getByRole('button', { name: 'Add to favorites' }));

    expect(onRequireAuth).toHaveBeenCalledWith('favorite this tutor');
    expect(mockAdd).not.toHaveBeenCalled();
  });

  it('authenticated student: clicking Message starts/gets a conversation and navigates to its thread, no auth gate', async () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, userId: 's1', roles: ['Student'] });
    mockCreateOrGetConversation.mockResolvedValue({
      id: 'conv-1',
      studentUserId: 's1',
      tutorUserId: 't1',
      createdAtUtc: '2024-01-01T00:00:00Z',
    });
    const onRequireAuth = vi.fn();
    renderWithProviders(<BookingPanel profile={profile} onRequireAuth={onRequireAuth} />);

    fireEvent.click(screen.getByRole('button', { name: 'Message' }));

    await waitFor(() => expect(mockCreateOrGetConversation).toHaveBeenCalledWith('t1'));
    await waitFor(() => expect(mockPush).toHaveBeenCalledWith('/messages/conv-1'));
    expect(onRequireAuth).not.toHaveBeenCalled();
  });

  it('authenticated student: not accepting new students disables the CTA with the right label', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, userId: 's1', roles: ['Student'] });
    renderWithProviders(<BookingPanel profile={{ ...profile, isAcceptingNewStudents: false }} onRequireAuth={vi.fn()} />);

    expect(screen.getByRole('button', { name: 'Not accepting new students' })).toBeDisabled();
  });

  it('authenticated student: no upcoming availability disables the CTA', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, userId: 's1', roles: ['Student'] });
    mockAvailability.mockReturnValue({ data: { hasUpcomingAvailability: false, nextAvailableStartUtc: null, timeZoneId: 'UTC' }, isLoading: false });
    renderWithProviders(<BookingPanel profile={profile} onRequireAuth={vi.fn()} />);

    expect(screen.getByRole('button', { name: 'No availability right now' })).toBeDisabled();
  });

  it('authenticated student: already used trial shows "Schedule a lesson", disabled, with explanatory tooltip', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, userId: 's1', roles: ['Student'] });
    mockTrialEligibility.mockReturnValue({ data: { hasUsedTrial: true }, isLoading: false });
    renderWithProviders(<BookingPanel profile={profile} onRequireAuth={vi.fn()} />);

    expect(screen.getByRole('button', { name: 'Schedule a lesson' })).toBeDisabled();
  });

  it('tutor-only viewer (no Student capability): no booking CTAs are rendered, only the price', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, userId: 't-viewer', roles: ['Tutor'] });
    renderWithProviders(<BookingPanel profile={profile} onRequireAuth={vi.fn()} />);

    expect(screen.queryByRole('link', { name: 'Book a trial lesson' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Message' })).not.toBeInTheDocument();
    expect(screen.getByText(/\$25/)).toBeInTheDocument();
  });

  it('dual-role viewer (Student + Tutor, ADR-0002): Student CTAs still render — Tutor-ness alone must not suppress them', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, userId: 'dual1', roles: ['Student', 'Tutor'] });
    renderWithProviders(<BookingPanel profile={profile} onRequireAuth={vi.fn()} />);

    expect(screen.getByRole('button', { name: 'Message' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Book a trial lesson' })).toBeInTheDocument();
  });

  it('has no detectable axe violations for the guest state', async () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, userId: null, roles: [] });
    const { container } = renderWithProviders(<BookingPanel profile={profile} onRequireAuth={vi.fn()} />);
    expect((await axe(container)).violations).toEqual([]);
  });
});

import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { axe } from 'vitest-axe';
import { BecomeATutorView } from './BecomeATutorView';
import { TutorApplication } from '@/features/tutor-onboarding/types';

const mockUseAuth = vi.fn();
vi.mock('@/features/auth/useAuth', () => ({ useAuth: () => mockUseAuth() }));

const mockBecomeTutor = vi.fn();
vi.mock('@/features/auth/useAuthMutations', () => ({ useBecomeTutor: () => mockBecomeTutor() }));

const mockCreateApplication = vi.fn();
const mockCurrentApplication = vi.fn();
vi.mock('@/features/tutor-onboarding/useTutorOnboarding', () => ({
  useCreateTutorApplication: () => mockCreateApplication(),
  // Captures the real `enabled` argument BecomeATutorView passes, so tests can assert on it
  // directly — this is the one hook call site the Batch 80 fix changed.
  useCurrentTutorApplication: (enabled: boolean) => mockCurrentApplication(enabled),
}));

vi.mock('@/features/tutor-onboarding/components/OnboardingWizard', () => ({
  OnboardingWizard: ({ application }: { application: TutorApplication }) => (
    <div data-testid="wizard">Wizard for {application.id}</div>
  ),
}));

describe('BecomeATutorView', () => {
  beforeEach(() => {
    mockBecomeTutor.mockReturnValue({ mutateAsync: vi.fn(), isPending: false });
    mockCreateApplication.mockReturnValue({ mutateAsync: vi.fn(), isPending: false });
    mockCurrentApplication.mockReset();
    mockCurrentApplication.mockReturnValue({ data: null, isLoading: false, isError: false, refetch: vi.fn() });
  });

  it('guest: shows the recruitment landing page (sign-up CTA + login hint) instead of the application form', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: false, roles: [] });

    render(<BecomeATutorView />);

    expect(screen.getByRole('link', { name: 'Create your tutor profile' })).toHaveAttribute(
      'href',
      '/signup?context=tutor&returnTo=%2Fbecome-a-tutor',
    );
    expect(screen.getByRole('link', { name: 'Log in' })).toHaveAttribute(
      'href',
      '/login?context=tutor&returnTo=%2Fbecome-a-tutor',
    );
    expect(screen.getByRole('heading', { name: 'How it works' })).toBeInTheDocument();
    expect(screen.queryByTestId('wizard')).not.toBeInTheDocument();
  });

  it('guest: the current-application query is disabled (unauthenticated behavior unchanged)', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: false, roles: [] });

    render(<BecomeATutorView />);

    expect(mockCurrentApplication).toHaveBeenCalledWith(false);
  });

  it('authenticated Student, no application yet: does not query the current application, and shows the Start application CTA', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, roles: ['Student'] });

    render(<BecomeATutorView />);

    expect(mockCurrentApplication).toHaveBeenCalledWith(false);
    expect(screen.getByRole('button', { name: 'Start application' })).toBeInTheDocument();
    expect(screen.queryByTestId('wizard')).not.toBeInTheDocument();
    expect(screen.queryByText("Couldn't load your application")).not.toBeInTheDocument();
  });

  it('authenticated Tutor, no application yet: still queries the current application (enabled), and shows the Start application CTA', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, roles: ['Tutor'] });

    render(<BecomeATutorView />);

    expect(mockCurrentApplication).toHaveBeenCalledWith(true);
    expect(screen.getByRole('button', { name: 'Start application' })).toBeInTheDocument();
  });

  it('authenticated dual-role (Student + Tutor): the query is enabled, matching the Tutor-role grant', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, roles: ['Student', 'Tutor'] });

    render(<BecomeATutorView />);

    expect(mockCurrentApplication).toHaveBeenCalledWith(true);
  });

  it('clicking Start application still follows the existing flow: grants the Tutor role, creates the draft, then refetches', async () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, roles: ['Student'] });
    const becomeTutorMutateAsync = vi.fn().mockResolvedValue(undefined);
    const createMutateAsync = vi.fn().mockResolvedValue(undefined);
    const refetch = vi.fn().mockResolvedValue(undefined);
    mockBecomeTutor.mockReturnValue({ mutateAsync: becomeTutorMutateAsync, isPending: false });
    mockCreateApplication.mockReturnValue({ mutateAsync: createMutateAsync, isPending: false });
    mockCurrentApplication.mockReturnValue({ data: null, isLoading: false, isError: false, refetch });

    render(<BecomeATutorView />);
    fireEvent.click(screen.getByRole('button', { name: 'Start application' }));

    await vi.waitFor(() => {
      expect(becomeTutorMutateAsync).toHaveBeenCalledTimes(1);
      expect(createMutateAsync).toHaveBeenCalledTimes(1);
      expect(refetch).toHaveBeenCalledTimes(1);
    });
  });

  it('shows a loading state while the session is resolving', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: true, roles: [] });

    render(<BecomeATutorView />);

    expect(screen.queryByRole('button', { name: 'Start application' })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Create your tutor profile' })).not.toBeInTheDocument();
  });

  it('an authenticated Tutor whose application query errors still sees the retryable error state', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, roles: ['Tutor'] });
    const refetch = vi.fn();
    mockCurrentApplication.mockReturnValue({ data: null, isLoading: false, isError: true, refetch });

    render(<BecomeATutorView />);
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }));

    expect(refetch).toHaveBeenCalledTimes(1);
  });

  it('an existing application (Tutor) renders the wizard, not the Start application CTA', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, roles: ['Tutor'] });
    mockCurrentApplication.mockReturnValue({
      data: { id: 'app-123' } as TutorApplication,
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    });

    render(<BecomeATutorView />);

    expect(screen.getByTestId('wizard')).toHaveTextContent('Wizard for app-123');
    expect(screen.queryByRole('button', { name: 'Start application' })).not.toBeInTheDocument();
  });

  it('has no detectable axe violations in the guest state', async () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: false, roles: [] });
    const { container } = render(<BecomeATutorView />);
    expect((await axe(container)).violations).toEqual([]);
  });
});

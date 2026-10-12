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
  useCurrentTutorApplication: () => mockCurrentApplication(),
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
    mockCurrentApplication.mockReturnValue({ data: null, isLoading: false, isError: false, refetch: vi.fn() });
  });

  it('guest: shows sign-up/login CTAs instead of the application form', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: false });

    render(<BecomeATutorView />);

    expect(screen.getByRole('link', { name: 'Sign up to apply' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Log in' })).toBeInTheDocument();
    expect(screen.queryByTestId('wizard')).not.toBeInTheDocument();
  });

  it('authenticated, no application yet: shows a Start application CTA', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false });

    render(<BecomeATutorView />);

    expect(screen.getByRole('button', { name: 'Start application' })).toBeInTheDocument();
  });

  it('clicking Start application grants the Tutor role, creates the draft, then refetches', async () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false });
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
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: true });

    render(<BecomeATutorView />);

    expect(screen.queryByRole('button', { name: 'Start application' })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Sign up to apply' })).not.toBeInTheDocument();
  });

  it('an error loading the current application shows a retryable error state', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false });
    const refetch = vi.fn();
    mockCurrentApplication.mockReturnValue({ data: null, isLoading: false, isError: true, refetch });

    render(<BecomeATutorView />);
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }));

    expect(refetch).toHaveBeenCalledTimes(1);
  });

  it('an existing application renders the wizard, not the Start application CTA', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false });
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
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: false });
    const { container } = render(<BecomeATutorView />);
    expect((await axe(container)).violations).toEqual([]);
  });
});

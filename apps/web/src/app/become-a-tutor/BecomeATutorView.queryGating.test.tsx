import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import type { ReactElement } from 'react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { BecomeATutorView } from './BecomeATutorView';

/**
 * Unlike `BecomeATutorView.test.tsx` (which mocks the whole `useTutorOnboarding` hook layer),
 * this file renders with the REAL `useCurrentTutorApplication`/`useQuery` machinery and a real
 * `QueryClientProvider`, mocking only the terminal API function. This is the rigorous proof for
 * the Batch 80 fix's central claim: a Student truly never issues
 * `GET /api/tutors/applications/me` — not just "a mock was called with `false`", but "the real
 * TanStack Query `enabled` gate actually suppressed the network call." It also proves there is no
 * authorization bypass: a disabled query cannot be reading stale/fabricated data because it never
 * fetches in the first place.
 */
const mockUseAuth = vi.fn();
vi.mock('@/features/auth/useAuth', () => ({ useAuth: () => mockUseAuth() }));

const mockBecomeTutorMutateAsync = vi.fn();
vi.mock('@/features/auth/useAuthMutations', () => ({
  useBecomeTutor: () => ({ mutateAsync: mockBecomeTutorMutateAsync, isPending: false }),
}));

const mockGetCurrentTutorApplication = vi.fn();
const mockCreateTutorApplication = vi.fn();
vi.mock('@/features/tutor-onboarding/api', () => ({
  getCurrentTutorApplication: (...args: unknown[]) => mockGetCurrentTutorApplication(...args),
  createTutorApplication: (...args: unknown[]) => mockCreateTutorApplication(...args),
  updateTutorApplicationDraft: vi.fn(),
  submitTutorApplication: vi.fn(),
  reopenTutorApplicationForRevision: vi.fn(),
}));

vi.mock('@/features/tutor-onboarding/components/OnboardingWizard', () => ({
  OnboardingWizard: () => <div data-testid="wizard">Wizard</div>,
}));

function renderWithClient(ui: ReactElement) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}

describe('BecomeATutorView — current-application query gating (real hook + real QueryClientProvider)', () => {
  beforeEach(() => {
    mockGetCurrentTutorApplication.mockReset();
    mockCreateTutorApplication.mockReset();
  });

  it('a Student (no Tutor role yet) never calls GET /api/tutors/applications/me', async () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, roles: ['Student'] });

    renderWithClient(<BecomeATutorView />);

    expect(await screen.findByRole('button', { name: 'Start application' })).toBeInTheDocument();
    // Give a wrongly-enabled query a real chance to fire before asserting it never did.
    await new Promise((resolve) => setTimeout(resolve, 50));
    expect(mockGetCurrentTutorApplication).not.toHaveBeenCalled();
  });

  it('a Tutor still calls GET /api/tutors/applications/me — the existing query stays enabled', async () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, roles: ['Tutor'] });
    mockGetCurrentTutorApplication.mockResolvedValue(null);

    renderWithClient(<BecomeATutorView />);

    await waitFor(() => expect(mockGetCurrentTutorApplication).toHaveBeenCalledTimes(1));
    expect(await screen.findByRole('button', { name: 'Start application' })).toBeInTheDocument();
  });

  it('a dual-role (Student + Tutor) account still calls GET /api/tutors/applications/me', async () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, roles: ['Student', 'Tutor'] });
    mockGetCurrentTutorApplication.mockResolvedValue(null);

    renderWithClient(<BecomeATutorView />);

    await waitFor(() => expect(mockGetCurrentTutorApplication).toHaveBeenCalledTimes(1));
  });

  it('an unauthenticated visitor never calls GET /api/tutors/applications/me', async () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: false, roles: [] });

    renderWithClient(<BecomeATutorView />);

    expect(await screen.findByRole('link', { name: 'Create your tutor profile' })).toBeInTheDocument();
    await new Promise((resolve) => setTimeout(resolve, 50));
    expect(mockGetCurrentTutorApplication).not.toHaveBeenCalled();
  });
});

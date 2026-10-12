import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { BecomeATutorAvailabilityView } from './BecomeATutorAvailabilityView';

const mockUseAuth = vi.fn();
vi.mock('@/features/auth/useAuth', () => ({ useAuth: () => mockUseAuth() }));

vi.mock('@/features/availability/components/TutorAvailabilityManager', () => ({
  TutorAvailabilityManager: () => <div data-testid="manager">Manager</div>,
}));

describe('BecomeATutorAvailabilityView', () => {
  beforeEach(() => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: false, roles: [] });
  });

  it('guest: prompts login instead of showing the manager', () => {
    render(<BecomeATutorAvailabilityView />);

    expect(screen.getByRole('link', { name: 'Log in' })).toHaveAttribute('href', '/login?returnTo=%2Fbecome-a-tutor%2Favailability');
    expect(screen.queryByTestId('manager')).not.toBeInTheDocument();
  });

  it('authenticated Student without the Tutor role: prompts starting a tutor application, not the manager', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, roles: ['Student'] });

    render(<BecomeATutorAvailabilityView />);

    expect(screen.getByRole('link', { name: 'Start your tutor application' })).toHaveAttribute('href', '/become-a-tutor');
    expect(screen.queryByTestId('manager')).not.toBeInTheDocument();
  });

  it('authenticated Tutor: renders the real availability manager', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, roles: ['Tutor'] });

    render(<BecomeATutorAvailabilityView />);

    expect(screen.getByTestId('manager')).toBeInTheDocument();
  });

  it('dual-role Student+Tutor: still renders the manager (Tutor capability is never hidden by also being a Student)', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, roles: ['Student', 'Tutor'] });

    render(<BecomeATutorAvailabilityView />);

    expect(screen.getByTestId('manager')).toBeInTheDocument();
  });
});

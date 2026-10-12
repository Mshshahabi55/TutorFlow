import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { axe } from 'vitest-axe';
import { SiteHeader } from './SiteHeader';

const mockUseAuth = vi.fn();
vi.mock('@/features/auth/useAuth', () => ({ useAuth: () => mockUseAuth() }));

const mockLogoutMutate = vi.fn();
const mockUseLogout = vi.fn();
vi.mock('@/features/auth/useAuthMutations', () => ({ useLogout: () => mockUseLogout() }));

const mockPush = vi.fn();
vi.mock('next/navigation', () => ({ usePathname: () => '/', useRouter: () => ({ push: mockPush }) }));

describe('SiteHeader', () => {
  beforeEach(() => {
    mockPush.mockClear();
    mockLogoutMutate.mockClear();
    mockUseLogout.mockReturnValue({ mutate: mockLogoutMutate, isPending: false });
  });

  it('guest: shows Log in / Sign up, not an account email or Log out', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: false, email: null });
    render(<SiteHeader />);

    expect(screen.getAllByRole('link', { name: 'Log in' })[0]).toHaveAttribute('href', '/login');
    expect(screen.getAllByRole('link', { name: 'Sign up' })[0]).toHaveAttribute('href', '/signup');
    expect(screen.queryByRole('button', { name: /log out/i })).not.toBeInTheDocument();
  });

  it('authenticated: shows the account email and a Log out action instead of Log in/Sign up', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, email: 'student@example.com' });
    render(<SiteHeader />);

    expect(screen.getAllByText('student@example.com')[0]).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Log in' })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Sign up' })).not.toBeInTheDocument();
  });

  it('loading: shows neither guest nor authenticated actions (no flash of the wrong state)', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: true, email: null });
    render(<SiteHeader />);

    expect(screen.queryByRole('link', { name: 'Log in' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /log out/i })).not.toBeInTheDocument();
  });

  it('clicking Log out calls the logout mutation', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, email: 'student@example.com' });
    render(<SiteHeader />);

    fireEvent.click(screen.getAllByRole('button', { name: /log out/i })[0]);

    expect(mockLogoutMutate).toHaveBeenCalled();
  });

  it('has no detectable axe violations in the authenticated state', async () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, email: 'student@example.com' });
    const { container } = render(<SiteHeader />);
    expect((await axe(container)).violations).toEqual([]);
  });
});

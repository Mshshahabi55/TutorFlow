import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { SignupPageContent } from './SignupPageContent';

let currentSearchParams = new URLSearchParams();
vi.mock('next/navigation', () => ({
  useSearchParams: () => currentSearchParams,
  useRouter: () => ({ replace: vi.fn() }),
}));

vi.mock('@/features/auth/useAuthMutations', () => ({ useRegister: () => ({ mutate: vi.fn(), isPending: false, isError: false, error: null }) }));

describe('SignupPageContent', () => {
  it('passes a legitimate internal returnTo straight through to the log-in link', () => {
    currentSearchParams = new URLSearchParams({ returnTo: '/tutors/jane-dev' });
    render(<SignupPageContent />);
    expect(screen.getByRole('link', { name: 'Log in' })).toHaveAttribute(
      'href',
      '/login?returnTo=%2Ftutors%2Fjane-dev',
    );
  });

  it('neutralizes an external returnTo (open-redirect attempt) down to "/"', () => {
    currentSearchParams = new URLSearchParams({ returnTo: 'https://evil.example.com' });
    render(<SignupPageContent />);
    expect(screen.getByRole('link', { name: 'Log in' })).toHaveAttribute('href', '/login');
  });

  it('neutralizes a backslash-variant returnTo (open-redirect attempt) down to "/"', () => {
    currentSearchParams = new URLSearchParams({ returnTo: '/\\evil.example.com' });
    render(<SignupPageContent />);
    expect(screen.getByRole('link', { name: 'Log in' })).toHaveAttribute('href', '/login');
  });

  it('shows tutor-flavored heading and CTA when context=tutor', () => {
    currentSearchParams = new URLSearchParams({ context: 'tutor', returnTo: '/become-a-tutor' });
    render(<SignupPageContent />);
    expect(screen.getByRole('heading', { name: 'Sign up as a tutor' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Get started' })).toBeInTheDocument();
  });

  it('shows the generic heading and CTA without a tutor context', () => {
    currentSearchParams = new URLSearchParams({ returnTo: '/tutors/jane-dev' });
    render(<SignupPageContent />);
    expect(screen.getByRole('heading', { name: 'Sign up' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Sign up' })).toBeInTheDocument();
  });

  it('relays context=tutor through to the log-in link when bouncing back', () => {
    currentSearchParams = new URLSearchParams({ context: 'tutor', returnTo: '/become-a-tutor' });
    render(<SignupPageContent />);
    expect(screen.getByRole('link', { name: 'Log in' })).toHaveAttribute(
      'href',
      '/login?context=tutor&returnTo=%2Fbecome-a-tutor',
    );
  });
});

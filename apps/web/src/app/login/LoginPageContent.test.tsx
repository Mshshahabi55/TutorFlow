import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { LoginPageContent } from './LoginPageContent';

let currentSearchParams = new URLSearchParams();
vi.mock('next/navigation', () => ({
  useSearchParams: () => currentSearchParams,
  useRouter: () => ({ replace: vi.fn() }),
}));

vi.mock('@/features/auth/useAuthMutations', () => ({ useLogin: () => ({ mutate: vi.fn(), isPending: false, isError: false, error: null }) }));

describe('LoginPageContent', () => {
  it('passes a legitimate internal returnTo straight through to the sign-up link', () => {
    currentSearchParams = new URLSearchParams({ returnTo: '/tutors/jane-dev' });
    render(<LoginPageContent />);
    expect(screen.getByRole('link', { name: 'Sign up' })).toHaveAttribute(
      'href',
      '/signup?returnTo=%2Ftutors%2Fjane-dev',
    );
  });

  it('neutralizes an external returnTo (open-redirect attempt) down to "/"', () => {
    currentSearchParams = new URLSearchParams({ returnTo: 'https://evil.example.com' });
    render(<LoginPageContent />);
    expect(screen.getByRole('link', { name: 'Sign up' })).toHaveAttribute('href', '/signup');
  });

  it('neutralizes a protocol-relative returnTo (open-redirect attempt) down to "/"', () => {
    currentSearchParams = new URLSearchParams({ returnTo: '//evil.example.com' });
    render(<LoginPageContent />);
    expect(screen.getByRole('link', { name: 'Sign up' })).toHaveAttribute('href', '/signup');
  });

  it('relays context=tutor through to the sign-up link, without changing its own heading', () => {
    currentSearchParams = new URLSearchParams({ context: 'tutor', returnTo: '/become-a-tutor' });
    render(<LoginPageContent />);
    expect(screen.getByRole('heading', { name: 'Log in' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Sign up' })).toHaveAttribute(
      'href',
      '/signup?context=tutor&returnTo=%2Fbecome-a-tutor',
    );
  });

  it('does not forward an unrecognized context value', () => {
    currentSearchParams = new URLSearchParams({ context: 'bogus', returnTo: '/become-a-tutor' });
    render(<LoginPageContent />);
    expect(screen.getByRole('link', { name: 'Sign up' })).toHaveAttribute(
      'href',
      '/signup?returnTo=%2Fbecome-a-tutor',
    );
  });
});

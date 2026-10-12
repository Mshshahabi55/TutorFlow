import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { axe } from 'vitest-axe';
import { ApiError } from '@/lib/api/client';
import { LoginForm } from './LoginForm';

const mockMutate = vi.fn();
const mockUseLogin = vi.fn();
vi.mock('../useAuthMutations', () => ({ useLogin: () => mockUseLogin() }));

const mockReplace = vi.fn();
vi.mock('next/navigation', () => ({ useRouter: () => ({ replace: mockReplace }) }));

describe('LoginForm', () => {
  beforeEach(() => {
    mockMutate.mockClear();
    mockReplace.mockClear();
    mockUseLogin.mockReturnValue({ mutate: mockMutate, isPending: false, isError: false, error: null });
  });

  it('shows validation errors and does not submit when fields are empty', () => {
    render(<LoginForm returnTo="/" />);

    fireEvent.click(screen.getByRole('button', { name: 'Log in' }));

    expect(screen.getByText('Enter your email address.')).toBeInTheDocument();
    expect(screen.getByText('Enter your password.')).toBeInTheDocument();
    expect(mockMutate).not.toHaveBeenCalled();
  });

  it('rejects a malformed email before calling the API', () => {
    render(<LoginForm returnTo="/" />);

    fireEvent.change(screen.getByLabelText(/^email/i), { target: { value: 'not-an-email' } });
    fireEvent.change(screen.getByLabelText(/^password/i), { target: { value: 'Password123' } });
    fireEvent.click(screen.getByRole('button', { name: 'Log in' }));

    expect(screen.getByText('Enter a valid email address.')).toBeInTheDocument();
    expect(mockMutate).not.toHaveBeenCalled();
  });

  it('submits trimmed credentials and redirects to returnTo on success', () => {
    mockMutate.mockImplementation((_input, { onSuccess }) => onSuccess());
    render(<LoginForm returnTo="/tutors/jane-dev" />);

    fireEvent.change(screen.getByLabelText(/^email/i), { target: { value: '  student@example.com  ' } });
    fireEvent.change(screen.getByLabelText(/^password/i), { target: { value: 'Password123' } });
    fireEvent.click(screen.getByRole('button', { name: 'Log in' }));

    expect(mockMutate).toHaveBeenCalledWith(
      { email: 'student@example.com', password: 'Password123' },
      expect.anything(),
    );
    expect(mockReplace).toHaveBeenCalledWith('/tutors/jane-dev');
  });

  it('shows a human-readable message on invalid credentials, never a raw ProblemDetails blob', () => {
    mockUseLogin.mockReturnValue({
      mutate: mockMutate,
      isPending: false,
      isError: true,
      error: new ApiError(401, { title: 'One or more errors occurred.', errors: ['Invalid email or password.'] }),
    });
    render(<LoginForm returnTo="/" />);

    expect(screen.getByRole('alert')).toHaveTextContent('Invalid email or password.');
  });

  it('disables the submit button while the login request is pending', () => {
    mockUseLogin.mockReturnValue({ mutate: mockMutate, isPending: true, isError: false, error: null });
    render(<LoginForm returnTo="/" />);

    expect(screen.getByRole('button', { name: 'Log in' })).toBeDisabled();
  });

  it('has no detectable axe violations', async () => {
    const { container } = render(<LoginForm returnTo="/" />);
    expect((await axe(container)).violations).toEqual([]);
  });
});

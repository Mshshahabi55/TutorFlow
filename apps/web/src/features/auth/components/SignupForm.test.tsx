import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { axe } from 'vitest-axe';
import { ApiError } from '@/lib/api/client';
import { SignupForm } from './SignupForm';

const mockMutate = vi.fn();
const mockUseRegister = vi.fn();
vi.mock('../useAuthMutations', () => ({ useRegister: () => mockUseRegister() }));

const mockReplace = vi.fn();
vi.mock('next/navigation', () => ({ useRouter: () => ({ replace: mockReplace }) }));

describe('SignupForm', () => {
  beforeEach(() => {
    mockMutate.mockClear();
    mockReplace.mockClear();
    mockUseRegister.mockReturnValue({ mutate: mockMutate, isPending: false, isError: false, error: null });
  });

  it('rejects a password under 8 characters before calling the API', () => {
    render(<SignupForm returnTo="/" />);

    fireEvent.change(screen.getByLabelText(/^email/i), { target: { value: 'new@example.com' } });
    fireEvent.change(screen.getByLabelText(/^password/i), { target: { value: 'short' } });
    fireEvent.click(screen.getByRole('button', { name: 'Sign up' }));

    expect(screen.getByText('Password must be at least 8 characters.')).toBeInTheDocument();
    expect(mockMutate).not.toHaveBeenCalled();
  });

  it('has no confirm-password field — matches the backend contract and Preply', () => {
    render(<SignupForm returnTo="/" />);
    expect(screen.queryByLabelText(/confirm password/i)).not.toBeInTheDocument();
  });

  it('submits and redirects to returnTo on success', () => {
    mockMutate.mockImplementation((_input, { onSuccess }) => onSuccess());
    render(<SignupForm returnTo="/tutors/jane-dev" />);

    fireEvent.change(screen.getByLabelText(/^email/i), { target: { value: 'new@example.com' } });
    fireEvent.change(screen.getByLabelText(/^password/i), { target: { value: 'Password123' } });
    fireEvent.click(screen.getByRole('button', { name: 'Sign up' }));

    expect(mockMutate).toHaveBeenCalledWith(
      { email: 'new@example.com', password: 'Password123' },
      expect.anything(),
    );
    expect(mockReplace).toHaveBeenCalledWith('/tutors/jane-dev');
  });

  it('shows the backend message for a duplicate email', () => {
    mockUseRegister.mockReturnValue({
      mutate: mockMutate,
      isPending: false,
      isError: true,
      error: new ApiError(400, {
        title: 'One or more errors occurred.',
        errors: ['A user with this email already exists.'],
      }),
    });
    render(<SignupForm returnTo="/" />);

    expect(screen.getByRole('alert')).toHaveTextContent('A user with this email already exists.');
  });

  it('disables the submit button while the register request is pending', () => {
    mockUseRegister.mockReturnValue({ mutate: mockMutate, isPending: true, isError: false, error: null });
    render(<SignupForm returnTo="/" />);

    expect(screen.getByRole('button', { name: 'Sign up' })).toBeDisabled();
  });

  it('has no detectable axe violations', async () => {
    const { container } = render(<SignupForm returnTo="/" />);
    expect((await axe(container)).violations).toEqual([]);
  });

  it('shows tutor-flavored CTA copy when context="tutor", same submit behavior', () => {
    mockMutate.mockImplementation((_input, { onSuccess }) => onSuccess());
    render(<SignupForm returnTo="/become-a-tutor" context="tutor" />);

    expect(screen.queryByRole('button', { name: 'Sign up' })).not.toBeInTheDocument();
    const submit = screen.getByRole('button', { name: 'Get started' });

    fireEvent.change(screen.getByLabelText(/^email/i), { target: { value: 'new@example.com' } });
    fireEvent.change(screen.getByLabelText(/^password/i), { target: { value: 'Password123' } });
    fireEvent.click(submit);

    expect(mockMutate).toHaveBeenCalledWith(
      { email: 'new@example.com', password: 'Password123' },
      expect.anything(),
    );
    expect(mockReplace).toHaveBeenCalledWith('/become-a-tutor');
  });

  it('relays context="tutor" to the Log in link', () => {
    render(<SignupForm returnTo="/become-a-tutor" context="tutor" />);
    expect(screen.getByRole('link', { name: 'Log in' })).toHaveAttribute(
      'href',
      '/login?context=tutor&returnTo=%2Fbecome-a-tutor',
    );
  });

  it('omits context from the Log in link by default', () => {
    render(<SignupForm returnTo="/become-a-tutor" />);
    expect(screen.getByRole('link', { name: 'Log in' })).toHaveAttribute(
      'href',
      '/login?returnTo=%2Fbecome-a-tutor',
    );
  });
});

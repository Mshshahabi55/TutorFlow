import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ReactElement } from 'react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { axe } from 'vitest-axe';
import { MessageTutorButton } from './MessageTutorButton';

function renderWithClient(ui: ReactElement) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}

const mockPush = vi.fn();
vi.mock('next/navigation', () => ({ useRouter: () => ({ push: mockPush }) }));

const mockCreateOrGetConversation = vi.fn();
vi.mock('@/features/messaging/api', () => ({
  createOrGetConversation: (tutorUserId: string) => mockCreateOrGetConversation(tutorUserId),
}));

const mockShowToast = vi.fn();
vi.mock('@/components/ui/Toast', () => ({ useToast: () => ({ showToast: mockShowToast }) }));

describe('MessageTutorButton', () => {
  beforeEach(() => {
    mockPush.mockClear();
    mockCreateOrGetConversation.mockReset();
    mockShowToast.mockClear();
  });

  it('unauthenticated/non-Student viewer: opens the auth gate, never calls the create-or-get endpoint', () => {
    const onRequireAuth = vi.fn();
    renderWithClient(<MessageTutorButton tutorUserId="tutor-1" isAuthenticatedStudent={false} onRequireAuth={onRequireAuth} />);

    fireEvent.click(screen.getByRole('button', { name: 'Message' }));

    expect(onRequireAuth).toHaveBeenCalledWith('message this tutor');
    expect(mockCreateOrGetConversation).not.toHaveBeenCalled();
  });

  it('authenticated Student: creates/gets the conversation and navigates to its thread on success', async () => {
    mockCreateOrGetConversation.mockResolvedValue({
      id: 'conv-1',
      studentUserId: 's1',
      tutorUserId: 'tutor-1',
      createdAtUtc: '2026-01-01T00:00:00.000Z',
    });
    renderWithClient(<MessageTutorButton tutorUserId="tutor-1" isAuthenticatedStudent onRequireAuth={vi.fn()} />);

    fireEvent.click(screen.getByRole('button', { name: 'Message' }));

    await waitFor(() => expect(mockCreateOrGetConversation).toHaveBeenCalledWith('tutor-1'));
    await waitFor(() => expect(mockPush).toHaveBeenCalledWith('/messages/conv-1'));
  });

  it('shows an accessible pending state while the request is in flight', async () => {
    let resolveRequest: (value: unknown) => void = () => {};
    mockCreateOrGetConversation.mockReturnValue(new Promise((resolve) => (resolveRequest = resolve)));
    renderWithClient(<MessageTutorButton tutorUserId="tutor-1" isAuthenticatedStudent onRequireAuth={vi.fn()} />);

    fireEvent.click(screen.getByRole('button', { name: 'Message' }));

    await waitFor(() => expect(screen.getByRole('button', { name: 'Message' })).toHaveAttribute('aria-busy', 'true'));

    resolveRequest({ id: 'conv-1', studentUserId: 's1', tutorUserId: 'tutor-1', createdAtUtc: '2026-01-01T00:00:00.000Z' });
    await waitFor(() => expect(mockPush).toHaveBeenCalled());
  });

  it('shows an accessible failure notification and never navigates when the request fails', async () => {
    mockCreateOrGetConversation.mockRejectedValue(new Error('network down'));
    renderWithClient(<MessageTutorButton tutorUserId="tutor-1" isAuthenticatedStudent onRequireAuth={vi.fn()} />);

    fireEvent.click(screen.getByRole('button', { name: 'Message' }));

    await waitFor(() => expect(mockShowToast).toHaveBeenCalledWith(expect.any(String), 'error'));
    expect(mockPush).not.toHaveBeenCalled();
  });

  it('has no detectable axe violations', async () => {
    const { container } = renderWithClient(
      <MessageTutorButton tutorUserId="tutor-1" isAuthenticatedStudent onRequireAuth={vi.fn()} />,
    );
    expect((await axe(container)).violations).toEqual([]);
  });
});

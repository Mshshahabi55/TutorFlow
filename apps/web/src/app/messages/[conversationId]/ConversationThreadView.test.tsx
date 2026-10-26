import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen } from '@testing-library/react';
import type { ReactElement } from 'react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { axe } from 'vitest-axe';
import { ApiError } from '@/lib/api/client';
import { conversationsQueryKey } from '@/features/messaging/useConversations';
import { ConversationSummary, MessageDto } from '@/features/messaging/types';
import { ConversationThreadView } from './ConversationThreadView';

const mockUseAuth = vi.fn();
vi.mock('@/features/auth/useAuth', () => ({ useAuth: () => mockUseAuth() }));

const mockUseMessages = vi.fn();
const mockUseSendMessage = vi.fn();
const mockUseMarkConversationRead = vi.fn();
const mockUseIsDocumentVisible = vi.fn();
vi.mock('@/features/messaging/useMessages', () => ({
  useMessages: (...args: unknown[]) => mockUseMessages(...args),
  useSendMessage: (...args: unknown[]) => mockUseSendMessage(...args),
  useMarkConversationRead: (...args: unknown[]) => mockUseMarkConversationRead(...args),
  useIsDocumentVisible: () => mockUseIsDocumentVisible(),
}));

function renderWithClient(ui: ReactElement, seedConversations?: ConversationSummary[]) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  if (seedConversations) {
    queryClient.setQueryData(conversationsQueryKey, seedConversations);
  }
  const result = render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
  return {
    ...result,
    rerenderWithClient: (next: ReactElement) =>
      result.rerender(<QueryClientProvider client={queryClient}>{next}</QueryClientProvider>),
  };
}

function message(overrides: Partial<MessageDto> = {}): MessageDto {
  return {
    id: 'm1',
    senderUserId: 'tutor-1',
    body: 'Hi there',
    createdAtUtc: '2026-01-14T10:00:00.000Z',
    sequenceNumber: 1,
    ...overrides,
  };
}

const CONVERSATION: ConversationSummary = {
  id: 'conv-1',
  studentUserId: 'student-1',
  tutorUserId: 'tutor-1',
  createdAtUtc: '2026-01-01T00:00:00.000Z',
  lastMessageAtUtc: '2026-01-14T10:00:00.000Z',
  myLastReadAtUtc: null,
  tutor: { displayName: 'Jane Dev', profilePhotoReference: null, publicSlug: 'jane-dev' },
};

describe('ConversationThreadView', () => {
  beforeEach(() => {
    mockUseIsDocumentVisible.mockReturnValue(true);
    mockUseSendMessage.mockReturnValue({ isPending: false, error: null, mutate: vi.fn() });
    mockUseMarkConversationRead.mockReturnValue({ mutate: vi.fn() });
  });

  it('shows a loading state while the session is resolving', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: true, userId: null, roles: [] });
    mockUseMessages.mockReturnValue({
      tutor: undefined,
      messages: [],
      isLoading: false,
      isError: false,
      error: null,
      refetch: vi.fn(),
      hasOlder: false,
      isLoadingOlder: false,
      loadOlder: vi.fn(),
    });
    renderWithClient(<ConversationThreadView conversationId="conv-1" />);
    expect(screen.getByRole('status')).toBeInTheDocument();
  });

  it('guest: shows sign up / log in prompts with a returnTo back to this thread', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: false, userId: null, roles: [] });
    mockUseMessages.mockReturnValue({
      tutor: undefined,
      messages: [],
      isLoading: false,
      isError: false,
      error: null,
      refetch: vi.fn(),
      hasOlder: false,
      isLoadingOlder: false,
      loadOlder: vi.fn(),
    });
    renderWithClient(<ConversationThreadView conversationId="conv-1" />);
    expect(screen.getByRole('link', { name: 'Sign up' })).toHaveAttribute(
      'href',
      '/signup?returnTo=%2Fmessages%2Fconv-1',
    );
    expect(screen.getByRole('link', { name: 'Log in' })).toHaveAttribute(
      'href',
      '/login?returnTo=%2Fmessages%2Fconv-1',
    );
  });

  describe('authenticated', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, userId: 'student-1', roles: ['Student'] });
    });

    it('shows a uniform inaccessible state on 404, never distinguishing missing vs. not-yours', () => {
      mockUseMessages.mockReturnValue({
        tutor: undefined,
        messages: [],
        isLoading: false,
        isError: true,
        error: new ApiError(404, {}),
        refetch: vi.fn(),
        hasOlder: false,
        isLoadingOlder: false,
        loadOlder: vi.fn(),
      });
      renderWithClient(<ConversationThreadView conversationId="conv-1" />);
      expect(screen.getByText("This conversation isn't available")).toBeInTheDocument();
      expect(screen.getByRole('link', { name: 'Back to messages' })).toHaveAttribute('href', '/messages');
    });

    it('shows a retryable error state for a non-404 failure', () => {
      const refetch = vi.fn();
      mockUseMessages.mockReturnValue({
        tutor: undefined,
        messages: [],
        isLoading: false,
        isError: true,
        error: new ApiError(500, {}),
        refetch,
        hasOlder: false,
        isLoadingOlder: false,
        loadOlder: vi.fn(),
      });
      renderWithClient(<ConversationThreadView conversationId="conv-1" />);
      expect(screen.getByText("We couldn't load this conversation")).toBeInTheDocument();
      screen.getByRole('button', { name: 'Try again' }).click();
      expect(refetch).toHaveBeenCalled();
    });

    it('Student viewer: renders the Tutor display name in the header', () => {
      mockUseMessages.mockReturnValue({
        tutor: { displayName: 'Jane Dev', profilePhotoReference: null, publicSlug: 'jane-dev' },
        messages: [message()],
        isLoading: false,
        isError: false,
        error: null,
        refetch: vi.fn(),
        hasOlder: false,
        isLoadingOlder: false,
        loadOlder: vi.fn(),
      });
      renderWithClient(<ConversationThreadView conversationId="conv-1" />, [CONVERSATION]);
      expect(screen.getByRole('heading', { name: 'Jane Dev' })).toBeInTheDocument();
    });

    it('correctly distinguishes the sender: own messages vs. the other party\'s', () => {
      mockUseMessages.mockReturnValue({
        tutor: { displayName: 'Jane Dev', profilePhotoReference: null, publicSlug: 'jane-dev' },
        messages: [message({ id: 'own', senderUserId: 'student-1', body: 'My own message' }), message({ id: 'their', senderUserId: 'tutor-1', body: 'Their message' })],
        isLoading: false,
        isError: false,
        error: null,
        refetch: vi.fn(),
        hasOlder: false,
        isLoadingOlder: false,
        loadOlder: vi.fn(),
      });
      renderWithClient(<ConversationThreadView conversationId="conv-1" />, [CONVERSATION]);
      expect(screen.getByText('My own message')).toBeInTheDocument();
      expect(screen.getByText('Their message')).toBeInTheDocument();
    });

    it('shows a "Load older messages" affordance only when the hook reports more history', () => {
      const loadOlder = vi.fn();
      mockUseMessages.mockReturnValue({
        tutor: null,
        messages: [message()],
        isLoading: false,
        isError: false,
        error: null,
        refetch: vi.fn(),
        hasOlder: true,
        isLoadingOlder: false,
        loadOlder,
      });
      renderWithClient(<ConversationThreadView conversationId="conv-1" />, [CONVERSATION]);
      fireEvent.click(screen.getByRole('button', { name: 'Load older messages' }));
      expect(loadOlder).toHaveBeenCalled();
    });

    it('does not show "Load older messages" once there is no more history', () => {
      mockUseMessages.mockReturnValue({
        tutor: null,
        messages: [message()],
        isLoading: false,
        isError: false,
        error: null,
        refetch: vi.fn(),
        hasOlder: false,
        isLoadingOlder: false,
        loadOlder: vi.fn(),
      });
      renderWithClient(<ConversationThreadView conversationId="conv-1" />, [CONVERSATION]);
      expect(screen.queryByRole('button', { name: 'Load older messages' })).not.toBeInTheDocument();
    });

    it('marks the conversation read once on mount, and does not loop on subsequent renders', () => {
      const mutate = vi.fn();
      mockUseMarkConversationRead.mockReturnValue({ mutate });
      mockUseMessages.mockReturnValue({
        tutor: null,
        messages: [message()],
        isLoading: false,
        isError: false,
        error: null,
        refetch: vi.fn(),
        hasOlder: false,
        isLoadingOlder: false,
        loadOlder: vi.fn(),
      });
      const { rerenderWithClient } = renderWithClient(<ConversationThreadView conversationId="conv-1" />, [CONVERSATION]);
      rerenderWithClient(<ConversationThreadView conversationId="conv-1" />);
      rerenderWithClient(<ConversationThreadView conversationId="conv-1" />);
      expect(mutate).toHaveBeenCalledTimes(1);
    });

    it('sends a composed message through useSendMessage', () => {
      const mutate = vi.fn();
      mockUseSendMessage.mockReturnValue({ isPending: false, error: null, mutate });
      mockUseMessages.mockReturnValue({
        tutor: null,
        messages: [],
        isLoading: false,
        isError: false,
        error: null,
        refetch: vi.fn(),
        hasOlder: false,
        isLoadingOlder: false,
        loadOlder: vi.fn(),
      });
      renderWithClient(<ConversationThreadView conversationId="conv-1" />, [CONVERSATION]);
      fireEvent.change(screen.getByLabelText('Message'), { target: { value: 'Hello' } });
      fireEvent.click(screen.getByRole('button', { name: 'Send' }));
      expect(mutate).toHaveBeenCalledWith({ text: 'Hello' });
    });

    it('has no detectable axe violations in the populated state', async () => {
      mockUseMessages.mockReturnValue({
        tutor: { displayName: 'Jane Dev', profilePhotoReference: null, publicSlug: 'jane-dev' },
        messages: [message()],
        isLoading: false,
        isError: false,
        error: null,
        refetch: vi.fn(),
        hasOlder: false,
        isLoadingOlder: false,
        loadOlder: vi.fn(),
      });
      const { container } = renderWithClient(<ConversationThreadView conversationId="conv-1" />, [CONVERSATION]);
      expect((await axe(container)).violations).toEqual([]);
    });
  });
});

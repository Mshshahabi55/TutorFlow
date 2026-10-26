import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor, act } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { mergeMessages, useMarkConversationRead, useMessages, useSendMessage } from './useMessages';
import { MessageDto } from './types';

const mockGetMessages = vi.fn();
const mockMarkConversationRead = vi.fn();
const mockSendMessage = vi.fn();
vi.mock('./api', () => ({
  getMessages: (...args: unknown[]) => mockGetMessages(...args),
  markConversationRead: (...args: unknown[]) => mockMarkConversationRead(...args),
  sendMessage: (...args: unknown[]) => mockSendMessage(...args),
}));

function wrapper({ children }: { children: ReactNode }) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
}

function renderUseMessages(conversationId: string, enabled: boolean) {
  return renderHook(() => useMessages(conversationId, enabled), { wrapper });
}

function message(overrides: Partial<MessageDto> = {}): MessageDto {
  return {
    id: 'm1',
    senderUserId: 'u1',
    body: 'hi',
    createdAtUtc: '2026-01-01T00:00:00Z',
    sequenceNumber: 1,
    ...overrides,
  };
}

describe('mergeMessages', () => {
  it('de-duplicates by id and sorts ascending by sequenceNumber, preferring the newest copy of a shared id', () => {
    const existing = [message({ id: 'a', sequenceNumber: 3 }), message({ id: 'b', sequenceNumber: 1 })];
    const incoming = [message({ id: 'b', sequenceNumber: 1, body: 'updated' }), message({ id: 'c', sequenceNumber: 2 })];

    const result = mergeMessages(existing, incoming);

    expect(result.map((m) => m.id)).toEqual(['b', 'c', 'a']);
    expect(result.find((m) => m.id === 'b')?.body).toBe('updated');
  });

  it('is a no-op merge-wise when the incoming page is a strict subset of what is already held', () => {
    const existing = [message({ id: 'a', sequenceNumber: 1 }), message({ id: 'b', sequenceNumber: 2 })];
    const result = mergeMessages(existing, [message({ id: 'a', sequenceNumber: 1 })]);
    expect(result.map((m) => m.id)).toEqual(['a', 'b']);
  });
});

describe('useMessages', () => {
  beforeEach(() => {
    mockGetMessages.mockReset();
    Object.defineProperty(document, 'visibilityState', { value: 'visible', configurable: true });
  });

  it('polls the newest page every 5s while enabled and the document is visible', async () => {
    mockGetMessages.mockResolvedValue({ tutor: null, messages: [message()], nextBeforeCursor: null });

    vi.useFakeTimers();
    try {
      renderUseMessages('conv-1', true);
      await act(async () => {
        await vi.advanceTimersByTimeAsync(0);
      });
      expect(mockGetMessages).toHaveBeenCalledTimes(1);

      await act(async () => {
        await vi.advanceTimersByTimeAsync(5000);
      });
      expect(mockGetMessages).toHaveBeenCalledTimes(2);

      await act(async () => {
        await vi.advanceTimersByTimeAsync(5000);
      });
      expect(mockGetMessages).toHaveBeenCalledTimes(3);
    } finally {
      vi.useRealTimers();
    }
  });

  it('stops polling once the document becomes hidden, resumes once visible again', async () => {
    mockGetMessages.mockResolvedValue({ tutor: null, messages: [], nextBeforeCursor: null });

    vi.useFakeTimers();
    try {
      renderUseMessages('conv-1', true);
      await act(async () => {
        await vi.advanceTimersByTimeAsync(0);
      });
      expect(mockGetMessages).toHaveBeenCalledTimes(1);

      act(() => {
        Object.defineProperty(document, 'visibilityState', { value: 'hidden', configurable: true });
        document.dispatchEvent(new Event('visibilitychange'));
      });

      await act(async () => {
        await vi.advanceTimersByTimeAsync(15000);
      });
      expect(mockGetMessages).toHaveBeenCalledTimes(1);

      act(() => {
        Object.defineProperty(document, 'visibilityState', { value: 'visible', configurable: true });
        document.dispatchEvent(new Event('visibilitychange'));
      });

      await act(async () => {
        await vi.advanceTimersByTimeAsync(5000);
      });
      expect(mockGetMessages.mock.calls.length).toBeGreaterThan(1);
    } finally {
      vi.useRealTimers();
    }
  });

  it('never fetches while disabled (thread not mounted / not yet authenticated)', async () => {
    vi.useFakeTimers();
    try {
      renderUseMessages('conv-1', false);
      await act(async () => {
        await vi.advanceTimersByTimeAsync(20000);
      });
      expect(mockGetMessages).not.toHaveBeenCalled();
    } finally {
      vi.useRealTimers();
    }
  });

  it('loadOlder pages backward via the real nextBeforeCursor and merges without duplicating overlapping messages', async () => {
    mockGetMessages.mockResolvedValueOnce({
      tutor: null,
      messages: [message({ id: 'new', sequenceNumber: 5 })],
      nextBeforeCursor: 5,
    });
    const { result } = renderUseMessages('conv-1', true);
    await waitFor(() => expect(result.current.hasOlder).toBe(true));
    expect(result.current.messages.map((m) => m.id)).toEqual(['new']);

    mockGetMessages.mockResolvedValueOnce({
      tutor: null,
      messages: [message({ id: 'old', sequenceNumber: 1 }), message({ id: 'new', sequenceNumber: 5 })],
      nextBeforeCursor: null,
    });

    act(() => {
      result.current.loadOlder();
    });

    await waitFor(() => expect(result.current.hasOlder).toBe(false));
    expect(mockGetMessages).toHaveBeenLastCalledWith('conv-1', { before: 5 });
    expect(result.current.messages.map((m) => m.id)).toEqual(['old', 'new']);
  });
});

describe('useMarkConversationRead', () => {
  it('calls the mark-read endpoint for this specific conversation', async () => {
    mockMarkConversationRead.mockResolvedValue(undefined);
    const { result } = renderHook(() => useMarkConversationRead('conv-1'), { wrapper });

    act(() => {
      result.current.mutate();
    });

    await waitFor(() => expect(mockMarkConversationRead).toHaveBeenCalledWith('conv-1'));
  });
});

describe('useSendMessage', () => {
  it('sends the given text to the right conversation', async () => {
    mockSendMessage.mockResolvedValue(message());
    const { result } = renderHook(() => useSendMessage('conv-1'), { wrapper });

    act(() => {
      result.current.mutate({ text: 'hello' });
    });

    await waitFor(() => expect(mockSendMessage).toHaveBeenCalledWith('conv-1', { text: 'hello' }));
  });
});

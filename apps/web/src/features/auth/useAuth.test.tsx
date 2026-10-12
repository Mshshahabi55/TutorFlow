import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { useAuth } from './useAuth';
import { Session } from './types';

const mockGetCurrentUser = vi.fn();
vi.mock('./api', () => ({ getCurrentUser: () => mockGetCurrentUser() }));

function renderUseAuth() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return renderHook(() => useAuth(), {
    wrapper: ({ children }) => <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>,
  });
}

describe('useAuth', () => {
  it('starts loading, then resolves to the guest state when /me returns null (401)', async () => {
    mockGetCurrentUser.mockResolvedValue(null);
    const { result } = renderUseAuth();

    expect(result.current.isLoading).toBe(true);

    await waitFor(() => expect(result.current.isLoading).toBe(false));

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.userId).toBeNull();
    expect(result.current.roles).toEqual([]);
  });

  it('resolves to the authenticated state with roles when a session exists', async () => {
    const session: Session = { userId: 'u1', email: 'student@example.com', roles: ['Student', 'Tutor'] };
    mockGetCurrentUser.mockResolvedValue(session);
    const { result } = renderUseAuth();

    await waitFor(() => expect(result.current.isLoading).toBe(false));

    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.userId).toBe('u1');
    expect(result.current.email).toBe('student@example.com');
    expect(result.current.roles).toEqual(['Student', 'Tutor']);
  });
});

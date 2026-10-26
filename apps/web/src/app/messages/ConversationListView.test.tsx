import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { axe } from 'vitest-axe';
import { ConversationListView } from './ConversationListView';
import { ConversationSummary } from '@/features/messaging/types';

const mockUseAuth = vi.fn();
vi.mock('@/features/auth/useAuth', () => ({ useAuth: () => mockUseAuth() }));

const mockUseConversations = vi.fn();
vi.mock('@/features/messaging/useConversations', () => ({ useConversations: () => mockUseConversations() }));

const CONVERSATION: ConversationSummary = {
  id: 'conv-1',
  studentUserId: 'student-1',
  tutorUserId: 'tutor-1',
  createdAtUtc: '2026-01-01T00:00:00.000Z',
  lastMessageAtUtc: '2026-01-14T10:00:00.000Z',
  myLastReadAtUtc: null,
  tutor: { displayName: 'Jane Dev', profilePhotoReference: null, publicSlug: 'jane-dev' },
};

describe('ConversationListView', () => {
  beforeEach(() => {
    mockUseConversations.mockReturnValue({ data: undefined, isLoading: false, isError: false, refetch: vi.fn() });
  });

  it('shows a loading state while the session is resolving', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: true, userId: null, roles: [] });
    render(<ConversationListView />);
    expect(screen.getByRole('status')).toBeInTheDocument();
  });

  it('guest: shows sign up / log in prompts with a returnTo back to this page', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: false, userId: null, roles: [] });
    render(<ConversationListView />);
    expect(screen.getByRole('link', { name: 'Sign up' })).toHaveAttribute('href', '/signup?returnTo=%2Fmessages');
    expect(screen.getByRole('link', { name: 'Log in' })).toHaveAttribute('href', '/login?returnTo=%2Fmessages');
  });

  it('shows a retryable error state on failure, never a raw error', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, userId: 'student-1', roles: ['Student'] });
    const refetch = vi.fn();
    mockUseConversations.mockReturnValue({ data: undefined, isLoading: false, isError: true, refetch });
    render(<ConversationListView />);
    expect(screen.getByText("We couldn't load your conversations")).toBeInTheDocument();
    screen.getByRole('button', { name: 'Try again' }).click();
    expect(refetch).toHaveBeenCalled();
  });

  it('Student empty state: points at finding a tutor to message', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, userId: 'student-1', roles: ['Student'] });
    mockUseConversations.mockReturnValue({ data: [], isLoading: false, isError: false, refetch: vi.fn() });
    render(<ConversationListView />);
    expect(screen.getByText('No conversations yet')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Find a tutor' })).toHaveAttribute('href', '/find-tutors');
  });

  it('Tutor empty state: explains students will message them, no "Find a tutor" action', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, userId: 'tutor-1', roles: ['Tutor'] });
    mockUseConversations.mockReturnValue({ data: [], isLoading: false, isError: false, refetch: vi.fn() });
    render(<ConversationListView />);
    expect(screen.getByText('No conversations yet')).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Find a tutor' })).not.toBeInTheDocument();
  });

  it('Student viewer: renders the Tutor display name for each conversation', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, userId: 'student-1', roles: ['Student'] });
    mockUseConversations.mockReturnValue({ data: [CONVERSATION], isLoading: false, isError: false, refetch: vi.fn() });
    render(<ConversationListView />);
    expect(screen.getByText('Jane Dev')).toBeInTheDocument();
  });

  it('Tutor viewer: renders the generic "Student" label, never the real student identity', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, userId: 'tutor-1', roles: ['Tutor'] });
    mockUseConversations.mockReturnValue({ data: [CONVERSATION], isLoading: false, isError: false, refetch: vi.fn() });
    render(<ConversationListView />);
    expect(screen.getByText('Student')).toBeInTheDocument();
    expect(screen.queryByText('student-1')).not.toBeInTheDocument();
  });

  it('has no detectable axe violations in the populated state', async () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, userId: 'student-1', roles: ['Student'] });
    mockUseConversations.mockReturnValue({ data: [CONVERSATION], isLoading: false, isError: false, refetch: vi.fn() });
    const { container } = render(<ConversationListView />);
    expect((await axe(container)).violations).toEqual([]);
  });

  it('has no detectable axe violations in the guest state', async () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: false, userId: null, roles: [] });
    const { container } = render(<ConversationListView />);
    expect((await axe(container)).violations).toEqual([]);
  });
});

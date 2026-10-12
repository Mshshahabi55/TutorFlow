import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { axe } from 'vitest-axe';
import { AdminTutorApplicationsView } from './AdminTutorApplicationsView';

const mockUseAuth = vi.fn();
vi.mock('@/features/auth/useAuth', () => ({ useAuth: () => mockUseAuth() }));

const mockUsePendingTutorApplications = vi.fn();
vi.mock('@/features/tutor-approval/useTutorApproval', () => ({
  usePendingTutorApplications: () => mockUsePendingTutorApplications(),
}));

describe('AdminTutorApplicationsView', () => {
  beforeEach(() => {
    mockUsePendingTutorApplications.mockReturnValue({ data: undefined, isLoading: false, isError: false, refetch: vi.fn() });
  });

  it('unauthenticated: shows a sign-in prompt, never the applications list', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: false, roles: [] });

    render(<AdminTutorApplicationsView />);

    expect(screen.getByText('Admin sign-in required')).toBeInTheDocument();
  });

  it('authenticated without the Admin role: shows "Not authorized", never the applications list', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, roles: ['Tutor'] });

    render(<AdminTutorApplicationsView />);

    expect(screen.getByText('Not authorized')).toBeInTheDocument();
  });

  it('loading: shows a loading state while auth resolves', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: true, roles: [] });

    render(<AdminTutorApplicationsView />);

    expect(screen.getByRole('status')).toBeInTheDocument();
  });

  it('admin, list loading: shows a loading state', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, roles: ['Admin'] });
    mockUsePendingTutorApplications.mockReturnValue({ data: undefined, isLoading: true, isError: false, refetch: vi.fn() });

    render(<AdminTutorApplicationsView />);

    expect(screen.getByText('Loading applications…')).toBeInTheDocument();
  });

  it('admin, list errored: shows a retryable error state', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, roles: ['Admin'] });
    const refetch = vi.fn();
    mockUsePendingTutorApplications.mockReturnValue({ data: undefined, isLoading: false, isError: true, refetch });

    render(<AdminTutorApplicationsView />);

    expect(screen.getByText("Couldn't load applications")).toBeInTheDocument();
  });

  it('admin, empty queue: shows an empty state, not a blank page', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, roles: ['Admin'] });
    mockUsePendingTutorApplications.mockReturnValue({
      data: { items: [], page: 1, pageSize: 20, totalCount: 0 },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    });

    render(<AdminTutorApplicationsView />);

    expect(screen.getByText('Nothing to review')).toBeInTheDocument();
  });

  it('admin, populated queue: renders each application with its status badge and links to its detail page', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, roles: ['Admin'] });
    mockUsePendingTutorApplications.mockReturnValue({
      data: {
        items: [
          {
            id: 'app-1',
            userId: 'user-1',
            status: 'Submitted',
            displayName: 'Ada Lovelace',
            headline: 'Patient math tutor',
            profilePhotoReference: null,
            subjectIds: ['s1'],
            submittedAtUtc: '2026-10-01T00:00:00Z',
          },
          {
            id: 'app-2',
            userId: 'user-2',
            status: 'UnderReview',
            displayName: 'Grace Hopper',
            headline: null,
            profilePhotoReference: null,
            subjectIds: [],
            submittedAtUtc: '2026-10-02T00:00:00Z',
          },
        ],
        page: 1,
        pageSize: 20,
        totalCount: 2,
      },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    });

    render(<AdminTutorApplicationsView />);

    expect(screen.getByText('Ada Lovelace')).toBeInTheDocument();
    expect(screen.getByText('Submitted')).toBeInTheDocument();
    expect(screen.getByText('Grace Hopper')).toBeInTheDocument();
    expect(screen.getByText('Under review')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Ada Lovelace/ })).toHaveAttribute('href', '/admin/tutor-applications/app-1');
  });

  it('has no detectable axe violations for an admin with a populated queue', async () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, roles: ['Admin'] });
    mockUsePendingTutorApplications.mockReturnValue({
      data: {
        items: [
          {
            id: 'app-1',
            userId: 'user-1',
            status: 'Submitted',
            displayName: 'Ada Lovelace',
            headline: 'Patient math tutor',
            profilePhotoReference: null,
            subjectIds: ['s1'],
            submittedAtUtc: '2026-10-01T00:00:00Z',
          },
        ],
        page: 1,
        pageSize: 20,
        totalCount: 1,
      },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    });

    const { container } = render(<AdminTutorApplicationsView />);

    expect((await axe(container)).violations).toEqual([]);
  });
});

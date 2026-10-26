import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { axe } from 'vitest-axe';
import { AdminTutorApplicationDetailView } from './AdminTutorApplicationDetailView';
import { AdminTutorApplicationDetail } from '@/features/tutor-approval/types';

const mockUseAuth = vi.fn();
vi.mock('@/features/auth/useAuth', () => ({ useAuth: () => mockUseAuth() }));

const mockUseTutorApplicationForAdmin = vi.fn();
const mockBeginReview = vi.fn();
const mockApprove = vi.fn();
const mockReject = vi.fn();
const mockPublish = vi.fn();
const mockSuspend = vi.fn();
const mockVerifyCertificate = vi.fn();
const mockRejectCertificate = vi.fn();

vi.mock('@/features/tutor-approval/useTutorApproval', () => ({
  useTutorApplicationForAdmin: () => mockUseTutorApplicationForAdmin(),
  useBeginReviewTutorApplication: () => mockBeginReview(),
  useApproveTutorApplication: () => mockApprove(),
  useRejectTutorApplication: () => mockReject(),
  usePublishTutorApplication: () => mockPublish(),
  useSuspendTutorApplication: () => mockSuspend(),
  useVerifyCertificate: () => mockVerifyCertificate(),
  useRejectCertificate: () => mockRejectCertificate(),
}));

function baseApplication(overrides: Partial<AdminTutorApplicationDetail> = {}): AdminTutorApplicationDetail {
  return {
    id: 'app-1',
    userId: 'user-1',
    status: 'Submitted',
    publicSlug: null,
    displayName: 'Ada Lovelace',
    headline: 'Patient math tutor',
    about: null,
    profilePhotoReference: null,
    introVideoReference: null,
    spokenLanguages: [{ languageCode: 'en', proficiency: 'Native' }],
    subjectIds: ['s1'],
    specialtyIds: [],
    countryOfBirthCode: null,
    isProfessionalTutor: false,
    isSuperTutor: false,
    yearsOfExperience: null,
    experienceSummary: null,
    educationEntries: [],
    certificateEntries: [],
    hourlyRateAmountMinorUnits: 2500,
    hourlyRateCurrency: 'EUR',
    availabilitySettingsConfigured: true,
    bookingSettingsConfigured: true,
    createdAtUtc: '2026-09-01T00:00:00Z',
    updatedAtUtc: '2026-09-01T00:00:00Z',
    submittedAtUtc: '2026-10-01T00:00:00Z',
    reviewedAtUtc: null,
    publishedAtUtc: null,
    rejectionReason: null,
    suspensionReason: null,
    ...overrides,
  };
}

describe('AdminTutorApplicationDetailView', () => {
  beforeEach(() => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isLoading: false, roles: ['Admin'] });
    mockBeginReview.mockReturnValue({ mutate: vi.fn(), isPending: false, isError: false });
    mockApprove.mockReturnValue({ mutate: vi.fn(), isPending: false, isError: false });
    mockReject.mockReturnValue({ mutate: vi.fn(), isPending: false, isError: false });
    mockPublish.mockReturnValue({ mutate: vi.fn(), isPending: false, isError: false });
    mockSuspend.mockReturnValue({ mutate: vi.fn(), isPending: false, isError: false });
    mockVerifyCertificate.mockReturnValue({ mutate: vi.fn(), isPending: false, isError: false, variables: undefined });
    mockRejectCertificate.mockReturnValue({ mutate: vi.fn(), isPending: false, isError: false });
  });

  it('unauthenticated: never calls the detail endpoint\'s data into view, shows sign-in prompt', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isLoading: false, roles: [] });
    mockUseTutorApplicationForAdmin.mockReturnValue({ data: undefined, isLoading: false, isError: false, refetch: vi.fn() });

    render(<AdminTutorApplicationDetailView id="app-1" />);

    expect(screen.getByText('Admin sign-in required')).toBeInTheDocument();
  });

  it('loading: shows a loading state', () => {
    mockUseTutorApplicationForAdmin.mockReturnValue({ data: undefined, isLoading: true, isError: false, refetch: vi.fn() });

    render(<AdminTutorApplicationDetailView id="app-1" />);

    expect(screen.getByText('Loading application…')).toBeInTheDocument();
  });

  it('error: shows a retryable error state', () => {
    mockUseTutorApplicationForAdmin.mockReturnValue({ data: undefined, isLoading: false, isError: true, refetch: vi.fn() });

    render(<AdminTutorApplicationDetailView id="app-1" />);

    expect(screen.getByText("Couldn't load this application")).toBeInTheDocument();
  });

  it('Submitted: shows Begin review, Approve, and Reject actions', () => {
    mockUseTutorApplicationForAdmin.mockReturnValue({ data: baseApplication(), isLoading: false, isError: false, refetch: vi.fn() });

    render(<AdminTutorApplicationDetailView id="app-1" />);

    expect(screen.getByRole('button', { name: 'Begin review' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Approve' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Reject' })).toBeInTheDocument();
  });

  it('clicking Approve calls the approve mutation for this application', () => {
    const mutate = vi.fn();
    mockApprove.mockReturnValue({ mutate, isPending: false, isError: false });
    mockUseTutorApplicationForAdmin.mockReturnValue({ data: baseApplication(), isLoading: false, isError: false, refetch: vi.fn() });

    render(<AdminTutorApplicationDetailView id="app-1" />);
    fireEvent.click(screen.getByRole('button', { name: 'Approve' }));

    expect(mutate).toHaveBeenCalled();
  });

  it('Reject: requires a non-empty reason before submitting, never calls the mutation with an empty one', () => {
    const mutate = vi.fn();
    mockReject.mockReturnValue({ mutate, isPending: false, isError: false });
    mockUseTutorApplicationForAdmin.mockReturnValue({ data: baseApplication(), isLoading: false, isError: false, refetch: vi.fn() });

    render(<AdminTutorApplicationDetailView id="app-1" />);
    fireEvent.click(screen.getByRole('button', { name: 'Reject' }));
    fireEvent.click(screen.getByRole('button', { name: 'Reject application' }));

    expect(screen.getByRole('alert')).toHaveTextContent('A reason is required.');
    expect(mutate).not.toHaveBeenCalled();
  });

  it('Reject: submits the typed reason to the mutation', () => {
    const mutate = vi.fn();
    mockReject.mockReturnValue({ mutate, isPending: false, isError: false });
    mockUseTutorApplicationForAdmin.mockReturnValue({ data: baseApplication(), isLoading: false, isError: false, refetch: vi.fn() });

    render(<AdminTutorApplicationDetailView id="app-1" />);
    fireEvent.click(screen.getByRole('button', { name: 'Reject' }));
    fireEvent.change(screen.getByLabelText('Reason for rejection'), { target: { value: 'Blurry profile photo' } });
    fireEvent.click(screen.getByRole('button', { name: 'Reject application' }));

    expect(mutate).toHaveBeenCalledWith('Blurry profile photo', expect.anything());
  });

  it('Approved: shows a Publish action, not Approve/Reject', () => {
    mockUseTutorApplicationForAdmin.mockReturnValue({
      data: baseApplication({ status: 'Approved' }),
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    });

    render(<AdminTutorApplicationDetailView id="app-1" />);

    expect(screen.getByRole('button', { name: 'Publish' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Approve' })).not.toBeInTheDocument();
  });

  it('Published: shows a Suspend action and a link to the public profile', () => {
    mockUseTutorApplicationForAdmin.mockReturnValue({
      data: baseApplication({ status: 'Published', publicSlug: 'ada-lovelace-abc123' }),
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    });

    render(<AdminTutorApplicationDetailView id="app-1" />);

    expect(screen.getByRole('button', { name: 'Suspend' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'View public profile' })).toHaveAttribute('href', '/tutors/ada-lovelace-abc123');
  });

  it('Rejected: shows the recorded reason, no actions', () => {
    mockUseTutorApplicationForAdmin.mockReturnValue({
      data: baseApplication({ status: 'Rejected', rejectionReason: 'Headline needs work' }),
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    });

    render(<AdminTutorApplicationDetailView id="app-1" />);

    expect(screen.getByText('Headline needs work')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Approve' })).not.toBeInTheDocument();
  });

  it('has no detectable axe violations for a Submitted application', async () => {
    mockUseTutorApplicationForAdmin.mockReturnValue({ data: baseApplication(), isLoading: false, isError: false, refetch: vi.fn() });

    const { container } = render(<AdminTutorApplicationDetailView id="app-1" />);

    expect((await axe(container)).violations).toEqual([]);
  });

  describe('certificate review', () => {
    function certificate(overrides: Partial<AdminTutorApplicationDetail['certificateEntries'][number]> = {}) {
      return {
        id: 'cert-1',
        name: 'TEFL Certificate',
        issuingOrganization: 'ITA',
        description: null,
        subjectId: null,
        startYear: null,
        endYear: null,
        hasDocument: true,
        verificationStatus: 'PendingReview' as const,
        rejectionReason: null,
        ...overrides,
      };
    }

    it('a PendingReview certificate shows a document link and Verify/Reject actions', () => {
      mockUseTutorApplicationForAdmin.mockReturnValue({
        data: baseApplication({ certificateEntries: [certificate()] }),
        isLoading: false,
        isError: false,
        refetch: vi.fn(),
      });

      render(<AdminTutorApplicationDetailView id="app-1" />);

      expect(screen.getByRole('link', { name: 'View document' })).toHaveAttribute(
        'href',
        expect.stringContaining('/api/tutors/admin/applications/app-1/certificates/cert-1/document'),
      );
      expect(screen.getByRole('button', { name: 'Verify certificate' })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: 'Reject certificate' })).toBeInTheDocument();
    });

    it('clicking Verify certificate calls the verify mutation with the certificate id', () => {
      const mutate = vi.fn();
      mockVerifyCertificate.mockReturnValue({ mutate, isPending: false, isError: false, variables: undefined });
      mockUseTutorApplicationForAdmin.mockReturnValue({
        data: baseApplication({ certificateEntries: [certificate()] }),
        isLoading: false,
        isError: false,
        refetch: vi.fn(),
      });

      render(<AdminTutorApplicationDetailView id="app-1" />);
      fireEvent.click(screen.getByRole('button', { name: 'Verify certificate' }));

      expect(mutate).toHaveBeenCalledWith('cert-1');
    });

    it('rejecting a certificate requires a reason and submits it to the mutation', () => {
      const mutate = vi.fn();
      mockRejectCertificate.mockReturnValue({ mutate, isPending: false, isError: false });
      mockUseTutorApplicationForAdmin.mockReturnValue({
        data: baseApplication({ certificateEntries: [certificate()] }),
        isLoading: false,
        isError: false,
        refetch: vi.fn(),
      });

      render(<AdminTutorApplicationDetailView id="app-1" />);
      fireEvent.click(screen.getByRole('button', { name: 'Reject certificate' }));
      fireEvent.click(screen.getByRole('button', { name: 'Reject certificate' }));
      expect(mutate).not.toHaveBeenCalled();

      fireEvent.change(screen.getByLabelText('Reason for rejection'), { target: { value: 'Illegible scan' } });
      fireEvent.click(screen.getByRole('button', { name: 'Reject certificate' }));

      expect(mutate).toHaveBeenCalledWith({ certificateId: 'cert-1', reason: 'Illegible scan' }, expect.anything());
    });

    it('a Verified certificate shows the badge with no action buttons', () => {
      mockUseTutorApplicationForAdmin.mockReturnValue({
        data: baseApplication({ certificateEntries: [certificate({ verificationStatus: 'Verified' })] }),
        isLoading: false,
        isError: false,
        refetch: vi.fn(),
      });

      render(<AdminTutorApplicationDetailView id="app-1" />);

      expect(screen.getByText('Verified')).toBeInTheDocument();
      expect(screen.queryByRole('button', { name: 'Verify certificate' })).not.toBeInTheDocument();
      expect(screen.queryByRole('link', { name: 'View document' })).not.toBeInTheDocument();
    });

    it('a Rejected certificate shows its recorded reason', () => {
      mockUseTutorApplicationForAdmin.mockReturnValue({
        data: baseApplication({
          certificateEntries: [certificate({ verificationStatus: 'Rejected', rejectionReason: 'Blurry image' })],
        }),
        isLoading: false,
        isError: false,
        refetch: vi.fn(),
      });

      render(<AdminTutorApplicationDetailView id="app-1" />);

      expect(screen.getByText('Blurry image')).toBeInTheDocument();
    });
  });
});

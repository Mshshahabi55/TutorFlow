import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { axe } from 'vitest-axe';
import { OnboardingWizard } from './OnboardingWizard';
import { TutorApplication } from '../types';

const mockUpdateDraft = vi.fn();
const mockSubmit = vi.fn();
const mockReopen = vi.fn();

vi.mock('../useTutorOnboarding', () => ({
  useUpdateTutorApplicationDraft: () => mockUpdateDraft(),
  useSubmitTutorApplication: () => mockSubmit(),
  useReopenTutorApplicationForRevision: () => mockReopen(),
}));

// AboutStep (the first step a brand-new Draft resumes to) pulls in auth/profile/catalog data
// directly — mocked here the same way AboutStep.test.tsx mocks them, since this file renders the
// real AboutStep through the real OnboardingWizard, not a stub.
vi.mock('@/features/auth/useAuth', () => ({ useAuth: () => ({ email: 'tutor@example.com' }) }));
vi.mock('@/features/auth/useProfile', () => ({
  useProfile: () => ({ data: { firstName: null, lastName: null, email: 'tutor@example.com' }, isLoading: false }),
  useUpdateProfile: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));
vi.mock('@/features/catalog/useCatalog', () => ({
  useSubjects: () => ({ data: [{ id: 's1', code: 'english', displayName: 'English' }], isLoading: false }),
  useLanguages: () => ({ data: [{ code: 'en', displayName: 'English' }], isLoading: false }),
  useSpecialties: () => ({ data: [], isLoading: false }),
}));

const baseApplication: TutorApplication = {
  id: 'app-1',
  userId: 'user-1',
  status: 'Draft',
  publicSlug: null,
  displayName: null,
  headline: null,
  about: null,
  profilePhotoReference: null,
  introVideoReference: null,
  spokenLanguages: [],
  subjectIds: [],
  specialtyIds: [],
  countryOfBirthCode: null,
  phoneNumber: null,
  isProfessionalTutor: false,
  isSuperTutor: false,
  yearsOfExperience: null,
  experienceSummary: null,
  educationEntries: [],
  certificateEntries: [],
  hourlyRateAmountMinorUnits: null,
  hourlyRateCurrency: null,
  availabilitySettingsConfigured: false,
  bookingSettingsConfigured: false,
  createdAtUtc: '2026-01-01T00:00:00.000Z',
  updatedAtUtc: '2026-01-01T00:00:00.000Z',
  submittedAtUtc: null,
  reviewedAtUtc: null,
  publishedAtUtc: null,
  rejectionReason: null,
  suspensionReason: null,
};

// About-complete (displayName + subject + language), used by tests that need to resume past
// About without re-exercising it.
const aboutComplete: TutorApplication = {
  ...baseApplication,
  displayName: 'Ada Lovelace',
  subjectIds: ['s1'],
  spokenLanguages: [{ languageCode: 'en', proficiency: 'Native' }],
};

describe('OnboardingWizard', () => {
  beforeEach(() => {
    mockUpdateDraft.mockReturnValue({ mutate: vi.fn(), isPending: false, isError: false, error: null });
    mockSubmit.mockReturnValue({ mutate: vi.fn(), isPending: false, isError: false, error: null });
    mockReopen.mockReturnValue({ mutate: vi.fn(), isPending: false, isError: false, error: null });
  });

  it('a brand-new Draft resumes on the first step (About), not a blank/random step', () => {
    render(<OnboardingWizard application={baseApplication} />);

    expect(screen.getByRole('heading', { name: 'About' })).toBeInTheDocument();
  });

  it('a Draft past About resumes on Photo, the evidenced second step', () => {
    render(<OnboardingWizard application={aboutComplete} />);

    expect(screen.getByRole('heading', { name: 'Profile photo' })).toBeInTheDocument();
  });

  it('a Draft past About and Photo resumes on Description, the evidenced fifth step', () => {
    const application: TutorApplication = { ...aboutComplete, profilePhotoReference: 'https://example.com/photo.jpg' };
    render(<OnboardingWizard application={application} />);

    expect(screen.getByRole('heading', { name: 'Profile description' })).toBeInTheDocument();
  });

  it('a Draft with every required field already filled resumes on Pricing — the final step, no separate Review step exists', () => {
    const complete: TutorApplication = {
      ...aboutComplete,
      headline: 'Patient, experienced math tutor',
      profilePhotoReference: 'https://example.com/photo.jpg',
      introVideoReference: 'https://example.com/intro.mp4',
      hourlyRateAmountMinorUnits: 2500,
      hourlyRateCurrency: 'EUR',
    };

    render(<OnboardingWizard application={complete} />);

    expect(screen.getByRole('heading', { name: 'Your hourly rate' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Submit application' })).toBeInTheDocument();
  });

  it('continuing from About calls the update-draft mutation with the entered display name', () => {
    const mutate = vi.fn();
    mockUpdateDraft.mockReturnValue({ mutate, isPending: false, isError: false, error: null });

    render(<OnboardingWizard application={baseApplication} />);
    fireEvent.change(screen.getByLabelText(/Display name/), { target: { value: 'New Tutor' } });
    fireEvent.click(screen.getByRole('combobox', { name: 'Subject you teach' }));
    fireEvent.click(screen.getByRole('option', { name: 'English' }));
    fireEvent.click(screen.getByRole('button', { name: 'Languages you speak' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'English' }));
    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    expect(mutate).toHaveBeenCalledWith(
      expect.objectContaining({ displayName: 'New Tutor' }),
      expect.objectContaining({ onSuccess: expect.any(Function) }),
    );
  });

  it('continuing from Description (resumed past an already-complete About+Photo) calls the update-draft mutation with the headline', () => {
    const mutate = vi.fn();
    mockUpdateDraft.mockReturnValue({ mutate, isPending: false, isError: false, error: null });
    const application: TutorApplication = { ...aboutComplete, profilePhotoReference: 'https://example.com/photo.jpg' };

    render(<OnboardingWizard application={application} />);
    expect(screen.getByText('Profile description')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText(/Headline/), { target: { value: 'Friendly, patient tutor' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    expect(mutate).toHaveBeenCalledWith(
      expect.objectContaining({ headline: 'Friendly, patient tutor' }),
      expect.objectContaining({ onSuccess: expect.any(Function) }),
    );
  });

  it('submitting from Pricing saves the rate first, then submits only once that save succeeds', () => {
    const updateMutate = vi.fn((_patch, options?: { onSuccess?: () => void }) => options?.onSuccess?.());
    const submitMutate = vi.fn();
    mockUpdateDraft.mockReturnValue({ mutate: updateMutate, isPending: false, isError: false, error: null });
    mockSubmit.mockReturnValue({ mutate: submitMutate, isPending: false, isError: false, error: null });

    render(<OnboardingWizard application={baseApplication} />);
    fireEvent.click(screen.getByRole('button', { name: /Pricing/ }));
    fireEvent.change(screen.getByLabelText(/Hourly rate/), { target: { value: '25' } });
    fireEvent.click(screen.getByRole('button', { name: 'Submit application' }));

    expect(updateMutate).toHaveBeenCalledWith(
      { hourlyRate: { amountMinorUnits: 2500, currency: 'EUR' } },
      expect.objectContaining({ onSuccess: expect.any(Function) }),
    );
    expect(submitMutate).toHaveBeenCalledTimes(1);
  });

  it('the progress list lets the tutor jump to any section without losing their place (not a locked linear stepper)', () => {
    render(<OnboardingWizard application={baseApplication} />);

    fireEvent.click(screen.getByRole('button', { name: /Pricing/ }));

    expect(screen.getByText('Your hourly rate')).toBeInTheDocument();
  });

  it('a Submitted application shows the review-status panel instead of the editable wizard', () => {
    render(<OnboardingWizard application={{ ...baseApplication, status: 'Submitted' }} />);

    expect(screen.getByText('Application submitted')).toBeInTheDocument();
    expect(screen.queryByText('About')).not.toBeInTheDocument();
  });

  it('a Rejected application shows the rejection reason and a Revise application action', () => {
    const mutate = vi.fn();
    mockReopen.mockReturnValue({ mutate, isPending: false, isError: false, error: null });

    render(
      <OnboardingWizard
        application={{ ...baseApplication, status: 'Rejected', rejectionReason: 'Profile photo needs updating' }}
      />,
    );

    expect(screen.getByText('Profile photo needs updating')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Revise application' }));
    expect(mutate).toHaveBeenCalledTimes(1);
  });

  it('a Published application shows a link to the public profile', () => {
    render(<OnboardingWizard application={{ ...baseApplication, status: 'Published', publicSlug: 'ada-lovelace-abc123' }} />);

    expect(screen.getByRole('link', { name: 'View your public profile' })).toHaveAttribute(
      'href',
      '/tutors/ada-lovelace-abc123',
    );
  });

  it('has no detectable axe violations on the first step', async () => {
    const { container } = render(<OnboardingWizard application={baseApplication} />);
    expect((await axe(container)).violations).toEqual([]);
  });
});

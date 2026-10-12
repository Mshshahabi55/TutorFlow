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

describe('OnboardingWizard', () => {
  beforeEach(() => {
    mockUpdateDraft.mockReturnValue({ mutate: vi.fn(), isPending: false, isError: false, error: null });
    mockSubmit.mockReturnValue({ mutate: vi.fn(), isPending: false, isError: false, error: null });
    mockReopen.mockReturnValue({ mutate: vi.fn(), isPending: false, isError: false, error: null });
  });

  it('a brand-new Draft resumes on the first step (Basics), not a blank/random step', () => {
    render(<OnboardingWizard application={baseApplication} />);

    expect(screen.getByText('The basics')).toBeInTheDocument();
  });

  it('a Draft with the required fields already filled resumes on Review, not the first step', () => {
    const complete: TutorApplication = {
      ...baseApplication,
      displayName: 'Ada Lovelace',
      headline: 'Patient, experienced math tutor',
      profilePhotoReference: 'https://example.com/photo.jpg',
      subjectIds: ['s1'],
      spokenLanguages: [{ languageCode: 'en', proficiency: 'Native' }],
      hourlyRateAmountMinorUnits: 2500,
      hourlyRateCurrency: 'EUR',
    };

    render(<OnboardingWizard application={complete} />);

    expect(screen.getByText('Review & submit')).toBeInTheDocument();
  });

  it('continuing from Basics calls the update-draft mutation with the entered display name', () => {
    const mutate = vi.fn();
    mockUpdateDraft.mockReturnValue({ mutate, isPending: false, isError: false, error: null });

    render(<OnboardingWizard application={baseApplication} />);
    fireEvent.change(screen.getByLabelText(/Display name/), { target: { value: 'New Tutor' } });
    fireEvent.change(screen.getByLabelText(/Headline/), { target: { value: 'Friendly, patient tutor' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    expect(mutate).toHaveBeenCalledWith(
      expect.objectContaining({ displayName: 'New Tutor', headline: 'Friendly, patient tutor' }),
      expect.objectContaining({ onSuccess: expect.any(Function) }),
    );
  });

  it('the progress list lets the tutor jump to any section without losing their place (not a locked linear stepper)', () => {
    render(<OnboardingWizard application={baseApplication} />);

    fireEvent.click(screen.getByRole('button', { name: /Rate/ }));

    expect(screen.getByText('Your hourly rate')).toBeInTheDocument();
  });

  it('a Submitted application shows the review-status panel instead of the editable wizard', () => {
    render(<OnboardingWizard application={{ ...baseApplication, status: 'Submitted' }} />);

    expect(screen.getByText('Application submitted')).toBeInTheDocument();
    expect(screen.queryByText('The basics')).not.toBeInTheDocument();
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

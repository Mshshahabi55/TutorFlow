import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { axe } from 'vitest-axe';
import { AboutStep } from './AboutStep';
import { TutorApplication } from '../types';

const mockUseAuth = vi.fn();
vi.mock('@/features/auth/useAuth', () => ({ useAuth: () => mockUseAuth() }));

const mockUseProfile = vi.fn();
const mockUpdateProfileMutate = vi.fn();
vi.mock('@/features/auth/useProfile', () => ({
  useProfile: () => mockUseProfile(),
  useUpdateProfile: () => ({ mutateAsync: mockUpdateProfileMutate, isPending: false }),
}));

const mockUseSubjects = vi.fn();
const mockUseLanguages = vi.fn();
vi.mock('@/features/catalog/useCatalog', () => ({
  useSubjects: () => mockUseSubjects(),
  useLanguages: () => mockUseLanguages(),
}));

const baseApplication = {
  displayName: null,
  countryOfBirthCode: null,
  subjectIds: [],
  spokenLanguages: [],
  phoneNumber: null,
} as unknown as TutorApplication;

const SUBJECTS = [{ id: 's1', code: 'english', displayName: 'English' }];
const LANGUAGES = [{ code: 'en', displayName: 'English' }];

describe('AboutStep', () => {
  beforeEach(() => {
    mockUseAuth.mockReturnValue({ email: 'tutor@example.com' });
    mockUseProfile.mockReturnValue({ data: { firstName: null, lastName: null, email: 'tutor@example.com' }, isLoading: false });
    mockUseSubjects.mockReturnValue({ data: SUBJECTS, isLoading: false });
    mockUseLanguages.mockReturnValue({ data: LANGUAGES, isLoading: false });
    mockUpdateProfileMutate.mockClear();
    mockUpdateProfileMutate.mockResolvedValue({ firstName: 'Ada', lastName: 'Lovelace', email: 'tutor@example.com' });
  });

  it('Continue is never disabled by client-side field state, even with nothing filled in (ADR-0026 §3 addendum, matching Preply\'s user-confirmed always-enabled Continue)', () => {
    render(<AboutStep application={baseApplication} onContinue={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByRole('button', { name: 'Save & continue' })).toBeEnabled();
  });

  it('shows the read-only email from the authenticated session', () => {
    render(<AboutStep application={baseApplication} onContinue={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByLabelText('Email')).toHaveValue('tutor@example.com');
    expect(screen.getByLabelText('Email')).toBeDisabled();
  });

  it('enables Continue once display name, subject, and language are present, and calls onContinue', async () => {
    const onContinue = vi.fn();
    const application = {
      ...baseApplication,
      displayName: 'Ada Lovelace',
      subjectIds: ['s1'],
      spokenLanguages: [{ languageCode: 'en', proficiency: 'Native' as const }],
    };
    render(<AboutStep application={application} onContinue={onContinue} isSaving={false} error={null} />);

    expect(screen.getByRole('button', { name: 'Save & continue' })).toBeEnabled();
    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    await waitFor(() =>
      expect(onContinue).toHaveBeenCalledWith({
        displayName: 'Ada Lovelace',
        countryOfBirthCode: undefined,
        subjectIds: ['s1'],
        spokenLanguages: [{ languageCode: 'en', proficiency: 'Native' }],
        phoneNumber: undefined,
      }),
    );
  });

  it('saves First/Last name via the Identity profile endpoint only when they changed, before continuing', async () => {
    const onContinue = vi.fn();
    const application = {
      ...baseApplication,
      displayName: 'Ada Lovelace',
      subjectIds: ['s1'],
      spokenLanguages: [{ languageCode: 'en', proficiency: 'Native' as const }],
    };
    render(<AboutStep application={application} onContinue={onContinue} isSaving={false} error={null} />);

    fireEvent.change(screen.getByLabelText('First name'), { target: { value: 'Ada' } });
    fireEvent.change(screen.getByLabelText('Last name'), { target: { value: 'Lovelace' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    await waitFor(() => expect(mockUpdateProfileMutate).toHaveBeenCalledWith({ firstName: 'Ada', lastName: 'Lovelace' }));
    await waitFor(() => expect(onContinue).toHaveBeenCalled());
  });

  it('skips the profile save entirely when First/Last name are unchanged from the loaded profile', async () => {
    mockUseProfile.mockReturnValue({ data: { firstName: 'Ada', lastName: 'Lovelace', email: 'tutor@example.com' }, isLoading: false });
    const onContinue = vi.fn();
    const application = {
      ...baseApplication,
      displayName: 'Ada Lovelace',
      subjectIds: ['s1'],
      spokenLanguages: [{ languageCode: 'en', proficiency: 'Native' as const }],
    };
    render(<AboutStep application={application} onContinue={onContinue} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    await waitFor(() => expect(onContinue).toHaveBeenCalled());
    expect(mockUpdateProfileMutate).not.toHaveBeenCalled();
  });

  it('pre-fills First/Last name from the loaded profile', () => {
    mockUseProfile.mockReturnValue({ data: { firstName: 'Grace', lastName: 'Hopper', email: 'tutor@example.com' }, isLoading: false });
    render(<AboutStep application={baseApplication} onContinue={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByLabelText('First name')).toHaveValue('Grace');
    expect(screen.getByLabelText('Last name')).toHaveValue('Hopper');
  });

  it('surfaces a save error from the parent', () => {
    render(<AboutStep application={baseApplication} onContinue={vi.fn()} isSaving={false} error="Couldn't save." />);

    expect(screen.getByText("Couldn't save.")).toBeInTheDocument();
  });

  it('has no detectable axe violations', async () => {
    const { container } = render(<AboutStep application={baseApplication} onContinue={vi.fn()} isSaving={false} error={null} />);
    expect((await axe(container)).violations).toEqual([]);
  });
});

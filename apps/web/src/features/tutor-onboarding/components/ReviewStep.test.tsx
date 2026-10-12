import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { ReviewStep } from './ReviewStep';
import { TutorApplication } from '../types';

const incompleteApplication = {
  displayName: null,
  headline: null,
  subjectIds: [],
  spokenLanguages: [],
  hourlyRateAmountMinorUnits: null,
  hourlyRateCurrency: null,
  availabilitySettingsConfigured: false,
} as unknown as TutorApplication;

const completeApplication: TutorApplication = {
  ...incompleteApplication,
  displayName: 'Ada Lovelace',
  headline: 'Math tutor',
  profilePhotoReference: 'https://example.com/photo.jpg',
  subjectIds: ['s1'],
  spokenLanguages: [{ languageCode: 'en', proficiency: 'Native' }],
  hourlyRateAmountMinorUnits: 2500,
  hourlyRateCurrency: 'EUR',
  availabilitySettingsConfigured: true,
};

describe('ReviewStep', () => {
  it('lists every missing required section without blocking submission (backend stays authoritative)', () => {
    render(<ReviewStep application={incompleteApplication} onBack={vi.fn()} onSubmit={vi.fn()} isSubmitting={false} error={null} />);

    expect(screen.getByText(/Display name/)).toBeInTheDocument();
    expect(screen.getByText(/At least one subject/)).toBeInTheDocument();
    expect(screen.getByText(/At least one language/)).toBeInTheDocument();
    expect(screen.getByText(/Hourly rate \(Rate\)/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Submit application' })).toBeEnabled();
  });

  it('shows no missing-sections warning once every backend-required field is filled', () => {
    render(<ReviewStep application={completeApplication} onBack={vi.fn()} onSubmit={vi.fn()} isSubmitting={false} error={null} />);

    expect(screen.queryByText('A few required sections are incomplete')).not.toBeInTheDocument();
  });

  it('clicking Submit application calls onSubmit', () => {
    const onSubmit = vi.fn();
    render(<ReviewStep application={completeApplication} onBack={vi.fn()} onSubmit={onSubmit} isSubmitting={false} error={null} />);

    fireEvent.click(screen.getByRole('button', { name: 'Submit application' }));

    expect(onSubmit).toHaveBeenCalledTimes(1);
  });

  it('surfaces a real submission error from the backend (e.g. bypassed client-side readiness)', () => {
    render(
      <ReviewStep
        application={incompleteApplication}
        onBack={vi.fn()}
        onSubmit={vi.fn()}
        isSubmitting={false}
        error="Display name is required before submission."
      />,
    );

    expect(screen.getByText('Display name is required before submission.')).toBeInTheDocument();
  });
});

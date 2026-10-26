import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { PricingStep } from './PricingStep';
import { TutorApplication } from '../types';

const baseApplication = {
  hourlyRateAmountMinorUnits: null,
  hourlyRateCurrency: null,
} as unknown as TutorApplication;

describe('PricingStep', () => {
  it('disables Submit until a rate is entered', () => {
    render(<PricingStep application={baseApplication} onSubmit={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByRole('button', { name: 'Submit application' })).toBeDisabled();
  });

  it('converts a EUR amount to minor units and defaults the currency to EUR, then submits', () => {
    const onSubmit = vi.fn();
    render(<PricingStep application={baseApplication} onSubmit={onSubmit} onBack={vi.fn()} isSaving={false} error={null} />);

    fireEvent.change(screen.getByLabelText(/Hourly rate/), { target: { value: '25.50' } });
    fireEvent.click(screen.getByRole('button', { name: 'Submit application' }));

    expect(onSubmit).toHaveBeenCalledWith({ hourlyRate: { amountMinorUnits: 2550, currency: 'EUR' } });
  });

  it('pre-fills from an already-saved rate', () => {
    const application = { ...baseApplication, hourlyRateAmountMinorUnits: 3000, hourlyRateCurrency: 'EUR' } as unknown as TutorApplication;
    render(<PricingStep application={application} onSubmit={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByLabelText(/Hourly rate/)).toHaveValue(30);
  });

  it('surfaces a submit/save error from the parent — this is the final step, so any backend validation failure (e.g. a missing photo or video from an earlier step) must still be visible here', () => {
    render(
      <PricingStep
        application={baseApplication}
        onSubmit={vi.fn()}
        onBack={vi.fn()}
        isSaving={false}
        error="A profile photo is required before submission."
      />,
    );

    expect(screen.getByText('A profile photo is required before submission.')).toBeInTheDocument();
  });

  it('calls onBack when Back is clicked', () => {
    const onBack = vi.fn();
    render(<PricingStep application={baseApplication} onSubmit={vi.fn()} onBack={onBack} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('button', { name: 'Back' }));

    expect(onBack).toHaveBeenCalledTimes(1);
  });
});

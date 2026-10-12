import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { RateStep } from './RateStep';
import { TutorApplication } from '../types';

const baseApplication = {
  hourlyRateAmountMinorUnits: null,
  hourlyRateCurrency: null,
} as unknown as TutorApplication;

describe('RateStep', () => {
  it('disables Continue until a rate is entered', () => {
    render(<RateStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByRole('button', { name: 'Save & continue' })).toBeDisabled();
  });

  it('converts a EUR amount to minor units and defaults the currency to EUR', () => {
    const onContinue = vi.fn();
    render(<RateStep application={baseApplication} onContinue={onContinue} onBack={vi.fn()} isSaving={false} error={null} />);

    fireEvent.change(screen.getByLabelText(/Hourly rate/), { target: { value: '25.50' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    expect(onContinue).toHaveBeenCalledWith({ hourlyRate: { amountMinorUnits: 2550, currency: 'EUR' } });
  });

  it('pre-fills from an already-saved rate', () => {
    const application = { ...baseApplication, hourlyRateAmountMinorUnits: 3000, hourlyRateCurrency: 'EUR' } as unknown as TutorApplication;
    render(<RateStep application={application} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByLabelText(/Hourly rate/)).toHaveValue(30);
  });

  it('surfaces a save error from the parent', () => {
    render(
      <RateStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error="Rate must be positive." />,
    );

    expect(screen.getByText('Rate must be positive.')).toBeInTheDocument();
  });
});

import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { DescriptionStep } from './DescriptionStep';
import { TutorApplication } from '../types';

const baseApplication = {
  headline: null,
  about: null,
} as unknown as TutorApplication;

describe('DescriptionStep', () => {
  it('shows the evidenced "Profile description" heading (step 5 of 8)', () => {
    render(<DescriptionStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByRole('heading', { name: 'Profile description' })).toBeInTheDocument();
  });

  it('Continue is never disabled by client-side field state, even with no headline filled in (ADR-0026 §3 addendum)', () => {
    render(<DescriptionStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByRole('button', { name: 'Save & continue' })).toBeEnabled();
  });

  it('calls onContinue with the entered fields once a headline is entered', () => {
    const onContinue = vi.fn();
    render(<DescriptionStep application={baseApplication} onContinue={onContinue} onBack={vi.fn()} isSaving={false} error={null} />);

    fireEvent.change(screen.getByLabelText(/Headline/), { target: { value: 'Patient math tutor' } });
    fireEvent.change(screen.getByLabelText(/About you/), { target: { value: 'I love teaching.' } });
    expect(screen.getByRole('button', { name: 'Save & continue' })).toBeEnabled();

    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    expect(onContinue).toHaveBeenCalledWith({
      headline: 'Patient math tutor',
      about: 'I love teaching.',
    });
  });

  it('remains enabled even when the headline is only whitespace — the backend, not this button, is the real gate', () => {
    render(<DescriptionStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    fireEvent.change(screen.getByLabelText(/Headline/), { target: { value: '   ' } });

    expect(screen.getByRole('button', { name: 'Save & continue' })).toBeEnabled();
  });

  it('pre-fills from an already-saved draft', () => {
    const application = { ...baseApplication, headline: 'Patient math tutor', about: 'Hello' };
    render(<DescriptionStep application={application} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByLabelText(/Headline/)).toHaveValue('Patient math tutor');
    expect(screen.getByLabelText(/About you/)).toHaveValue('Hello');
  });

  it('surfaces a save error from the parent and disables Continue while saving', () => {
    render(<DescriptionStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving error="Something went wrong." />);

    expect(screen.getByText('Something went wrong.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Save & continue' })).toBeDisabled();
  });

  it('calls onBack when Back is clicked', () => {
    const onBack = vi.fn();
    render(<DescriptionStep application={baseApplication} onContinue={vi.fn()} onBack={onBack} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('button', { name: 'Back' }));

    expect(onBack).toHaveBeenCalledTimes(1);
  });

  it('has no detectable axe violations', async () => {
    const { container } = render(<DescriptionStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);
    expect((await axe(container)).violations).toEqual([]);
  });
});

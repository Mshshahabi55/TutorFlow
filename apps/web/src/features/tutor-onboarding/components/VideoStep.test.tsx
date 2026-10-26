import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { VideoStep } from './VideoStep';
import { TutorApplication } from '../types';

const baseApplication = {
  introVideoReference: null,
} as unknown as TutorApplication;

describe('VideoStep', () => {
  it('is optional — Continue is enabled even with no video URL entered', () => {
    render(<VideoStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByRole('button', { name: 'Save & continue' })).toBeEnabled();
  });

  it('calls onContinue with the entered video URL', () => {
    const onContinue = vi.fn();
    render(<VideoStep application={baseApplication} onContinue={onContinue} onBack={vi.fn()} isSaving={false} error={null} />);

    fireEvent.change(screen.getByLabelText(/Video URL/), { target: { value: 'https://youtube.com/watch?v=abc' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    expect(onContinue).toHaveBeenCalledWith({ introVideoReference: 'https://youtube.com/watch?v=abc' });
  });

  it('calls onContinue with an empty string when left blank, never a fabricated value', () => {
    const onContinue = vi.fn();
    render(<VideoStep application={baseApplication} onContinue={onContinue} onBack={vi.fn()} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    expect(onContinue).toHaveBeenCalledWith({ introVideoReference: '' });
  });

  it('pre-fills from an already-saved video reference', () => {
    const application = { ...baseApplication, introVideoReference: 'https://youtube.com/watch?v=abc' };
    render(<VideoStep application={application} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByLabelText(/Video URL/)).toHaveValue('https://youtube.com/watch?v=abc');
  });

  it('calls onBack when Back is clicked', () => {
    const onBack = vi.fn();
    render(<VideoStep application={baseApplication} onContinue={vi.fn()} onBack={onBack} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('button', { name: 'Back' }));

    expect(onBack).toHaveBeenCalledTimes(1);
  });

  it('surfaces a save error from the parent', () => {
    render(<VideoStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error="Couldn't save." />);

    expect(screen.getByText("Couldn't save.")).toBeInTheDocument();
  });

  it('disables Continue while saving', () => {
    render(<VideoStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving error={null} />);

    expect(screen.getByRole('button', { name: 'Save & continue' })).toBeDisabled();
  });

  it('shows the full Preply-matched guideline set (18-screenshot-evidence.md SS08/SS09)', () => {
    render(<VideoStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByText(/stable surface/)).toBeInTheDocument();
    expect(screen.getByText(/face is clearly visible/)).toBeInTheDocument();
    expect(screen.getByText(/Include logos or links/)).toBeInTheDocument();
  });

  it('has no detectable axe violations', async () => {
    const { container } = render(
      <VideoStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />,
    );
    expect((await axe(container)).violations).toEqual([]);
  });
});

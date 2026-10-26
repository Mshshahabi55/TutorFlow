import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { axe } from 'vitest-axe';
import { TeachingStep } from './TeachingStep';
import { TutorApplication } from '../types';

const mockUseSpecialties = vi.fn();
vi.mock('@/features/catalog/useCatalog', () => ({
  useSpecialties: () => mockUseSpecialties(),
}));

const baseApplication = {
  specialtyIds: [],
  yearsOfExperience: null,
  experienceSummary: null,
} as unknown as TutorApplication;

describe('TeachingStep', () => {
  beforeEach(() => {
    mockUseSpecialties.mockReturnValue({ data: [], isLoading: false });
  });

  it('Continue is always enabled — none of this step\'s fields are required', () => {
    render(<TeachingStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByRole('button', { name: 'Save & continue' })).toBeEnabled();
  });

  it('shows a loading skeleton instead of the select while catalog data is still loading', () => {
    mockUseSpecialties.mockReturnValue({ data: undefined, isLoading: true });
    render(<TeachingStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.queryByRole('button', { name: 'Specialties' })).not.toBeInTheDocument();
  });

  it('calls onContinue with the exact unmodified state when nothing is changed', () => {
    const onContinue = vi.fn();
    render(<TeachingStep application={baseApplication} onContinue={onContinue} onBack={vi.fn()} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    expect(onContinue).toHaveBeenCalledWith({
      specialtyIds: [],
      yearsOfExperience: undefined,
      experienceSummary: '',
    });
  });

  it('pre-fills years of experience and experience summary from an already-saved draft', () => {
    const application = { ...baseApplication, yearsOfExperience: 5, experienceSummary: 'Five years tutoring.' };
    render(<TeachingStep application={application} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByLabelText('Years of experience')).toHaveValue(5);
    expect(screen.getByLabelText('Experience summary')).toHaveValue('Five years tutoring.');
  });

  it('calls onBack when Back is clicked', () => {
    const onBack = vi.fn();
    render(<TeachingStep application={baseApplication} onContinue={vi.fn()} onBack={onBack} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('button', { name: 'Back' }));

    expect(onBack).toHaveBeenCalledTimes(1);
  });

  it('surfaces a save error from the parent', () => {
    render(<TeachingStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error="Couldn't save." />);

    expect(screen.getByText("Couldn't save.")).toBeInTheDocument();
  });

  it('has no detectable axe violations', async () => {
    const { container } = render(
      <TeachingStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />,
    );
    expect((await axe(container)).violations).toEqual([]);
  });
});

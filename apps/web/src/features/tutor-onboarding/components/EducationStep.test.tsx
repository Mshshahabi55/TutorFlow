import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { EducationStep } from './EducationStep';
import { EducationEntryDraft, TutorApplication } from '../types';

const baseApplication = {
  educationEntries: [],
} as unknown as TutorApplication;

function entry(overrides: Partial<EducationEntryDraft> = {}): EducationEntryDraft {
  return { institution: '', degree: '', fieldOfStudy: null, startYear: null, endYear: null, ...overrides };
}

describe('EducationStep', () => {
  it('is entirely optional — Continue is enabled with no entries at all', () => {
    render(<EducationStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByRole('button', { name: 'Save & continue' })).toBeEnabled();
    expect(screen.queryByLabelText('Institution')).not.toBeInTheDocument();
  });

  it('"Add another education" adds one empty, editable entry', () => {
    render(<EducationStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('button', { name: 'Add another education' }));

    expect(screen.getByLabelText('Institution')).toHaveValue('');
    expect(screen.getByLabelText('Degree')).toHaveValue('');
    expect(screen.getByLabelText('Specialization')).toHaveValue('');
  });

  it('filling in a new education entry and continuing sends it in educationEntries', () => {
    const onContinue = vi.fn();
    render(<EducationStep application={baseApplication} onContinue={onContinue} onBack={vi.fn()} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('button', { name: 'Add another education' }));
    fireEvent.change(screen.getByLabelText('Institution'), { target: { value: 'University of Leeds' } });
    fireEvent.change(screen.getByLabelText('Degree'), { target: { value: 'BA Linguistics' } });
    fireEvent.change(screen.getByLabelText('Specialization'), { target: { value: 'Applied Linguistics' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    expect(onContinue).toHaveBeenCalledWith({
      educationEntries: [
        { institution: 'University of Leeds', degree: 'BA Linguistics', fieldOfStudy: 'Applied Linguistics', startYear: null, endYear: null },
      ],
    });
  });

  it('"Remove" deletes that specific entry, not all entries', () => {
    const application = {
      educationEntries: [entry({ institution: 'University A', degree: 'Degree A' }), entry({ institution: 'University B', degree: 'Degree B' })],
    } as unknown as TutorApplication;
    render(<EducationStep application={application} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    fireEvent.click(screen.getAllByRole('button', { name: 'Remove' })[0]);

    expect(screen.queryByDisplayValue('University A')).not.toBeInTheDocument();
    expect(screen.getByDisplayValue('University B')).toBeInTheDocument();
  });

  it('pre-fills existing education entries, including specialization and years', () => {
    const application = {
      educationEntries: [entry({ institution: 'University of Leeds', degree: 'BA Linguistics', fieldOfStudy: 'Applied Linguistics', startYear: 2018, endYear: 2021 })],
    } as unknown as TutorApplication;
    render(<EducationStep application={application} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByDisplayValue('University of Leeds')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Applied Linguistics')).toBeInTheDocument();
    expect(screen.getByText('2018')).toBeInTheDocument();
    expect(screen.getByText('2021')).toBeInTheDocument();
  });

  it('a null end year displays and submits as "Present"', () => {
    const application = {
      educationEntries: [entry({ institution: 'University of Leeds', degree: 'BA Linguistics', startYear: 2018, endYear: null })],
    } as unknown as TutorApplication;
    render(<EducationStep application={application} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByText('Present')).toBeInTheDocument();
  });

  it('rejects a start year after the end year before calling onContinue', () => {
    const application = {
      educationEntries: [entry({ institution: 'University of Leeds', degree: 'BA Linguistics', startYear: 2024, endYear: 2020 })],
    } as unknown as TutorApplication;
    const onContinue = vi.fn();
    render(<EducationStep application={application} onContinue={onContinue} onBack={vi.fn()} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    expect(onContinue).not.toHaveBeenCalled();
    expect(screen.getByText('Start year must not be after end year.')).toBeInTheDocument();
  });

  it('checking "I don\'t have a higher education degree" hides and clears the entry list', () => {
    const application = {
      educationEntries: [entry({ institution: 'University of Leeds', degree: 'BA Linguistics' })],
    } as unknown as TutorApplication;
    const onContinue = vi.fn();
    render(<EducationStep application={application} onContinue={onContinue} onBack={vi.fn()} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('checkbox', { name: /I don.t have a higher education degree/i }));

    expect(screen.queryByDisplayValue('University of Leeds')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Add another education' })).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));
    expect(onContinue).toHaveBeenCalledWith({ educationEntries: [] });
  });

  it('unchecking "I don\'t have a higher education degree" restores an empty, editable form — not the previous entries', () => {
    const application = {
      educationEntries: [entry({ institution: 'University of Leeds', degree: 'BA Linguistics' })],
    } as unknown as TutorApplication;
    render(<EducationStep application={application} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    const checkbox = screen.getByRole('checkbox', { name: /I don.t have a higher education degree/i });
    fireEvent.click(checkbox);
    fireEvent.click(checkbox);

    expect(screen.queryByDisplayValue('University of Leeds')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Add another education' })).toBeInTheDocument();
  });

  it('calls onBack when Back is clicked', () => {
    const onBack = vi.fn();
    render(<EducationStep application={baseApplication} onContinue={vi.fn()} onBack={onBack} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('button', { name: 'Back' }));

    expect(onBack).toHaveBeenCalledTimes(1);
  });

  it('surfaces a save error from the parent', () => {
    render(<EducationStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error="Couldn't save." />);

    expect(screen.getByText("Couldn't save.")).toBeInTheDocument();
  });

  it('has no detectable axe violations', async () => {
    const { container } = render(
      <EducationStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />,
    );
    expect((await axe(container)).violations).toEqual([]);
  });
});

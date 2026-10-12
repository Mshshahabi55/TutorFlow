import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { ActiveFilterChips } from './ActiveFilterChips';
import { DEFAULT_FILTERS } from '../types';

const SUBJECTS = [{ id: 'math-id', code: 'math', displayName: 'Mathematics' }];
const SPECIALTIES = [
  { id: 'exam-prep-id', subjectId: 'math-id', code: 'exam-prep', displayName: 'Exam preparation' },
  { id: 'business-id', subjectId: 'math-id', code: 'business', displayName: 'Business language' },
];

vi.mock('@/features/catalog/useCatalog', () => ({
  useSubjects: () => ({ data: SUBJECTS }),
  useSpecialties: () => ({ data: SPECIALTIES }),
  useLanguages: () => ({ data: [] }),
}));

describe('ActiveFilterChips', () => {
  it('renders nothing when no filters are active', () => {
    const { container } = render(
      <ActiveFilterChips filters={DEFAULT_FILTERS} onChange={vi.fn()} onClearAll={vi.fn()} />,
    );
    expect(container).toBeEmptyDOMElement();
  });

  it('renders one chip per active filter dimension, resolving IDs to catalog display names', () => {
    render(
      <ActiveFilterChips
        filters={{ ...DEFAULT_FILTERS, subjectId: 'math-id', specialtyIds: ['exam-prep-id', 'business-id'] }}
        onChange={vi.fn()}
        onClearAll={vi.fn()}
      />,
    );

    expect(screen.getByText('Mathematics')).toBeInTheDocument();
    expect(screen.getByText('Exam preparation')).toBeInTheDocument();
    expect(screen.getByText('Business language')).toBeInTheDocument();
  });

  it('renders badge filter chips', () => {
    render(
      <ActiveFilterChips
        filters={{ ...DEFAULT_FILTERS, professionalTutor: true, superTutor: true }}
        onChange={vi.fn()}
        onClearAll={vi.fn()}
      />,
    );

    expect(screen.getByText('Professional tutor')).toBeInTheDocument();
    expect(screen.getByText('Super Tutor')).toBeInTheDocument();
  });

  it('removing a single specialty chip only clears that specialty, not the others', async () => {
    const onChange = vi.fn();
    render(
      <ActiveFilterChips
        filters={{ ...DEFAULT_FILTERS, specialtyIds: ['exam-prep-id', 'business-id'] }}
        onChange={onChange}
        onClearAll={vi.fn()}
      />,
    );

    await userEvent.click(screen.getByRole('button', { name: /Remove filter: Exam preparation/i }));

    expect(onChange).toHaveBeenCalledWith({ specialtyIds: ['business-id'] });
  });

  it('"Clear all" calls onClearAll, not onChange', async () => {
    const onChange = vi.fn();
    const onClearAll = vi.fn();
    render(
      <ActiveFilterChips
        filters={{ ...DEFAULT_FILTERS, subjectId: 'math-id' }}
        onChange={onChange}
        onClearAll={onClearAll}
      />,
    );

    await userEvent.click(screen.getByRole('button', { name: 'Clear all' }));

    expect(onClearAll).toHaveBeenCalledTimes(1);
    expect(onChange).not.toHaveBeenCalled();
  });

  it('collapses a price-min-only and a price-range filter into a single chip', () => {
    const { rerender } = render(
      <ActiveFilterChips
        filters={{ ...DEFAULT_FILTERS, priceMinMinorUnits: 1000 }}
        onChange={vi.fn()}
        onClearAll={vi.fn()}
      />,
    );
    expect(screen.getAllByText(/€10/)).toHaveLength(1);

    rerender(
      <ActiveFilterChips
        filters={{ ...DEFAULT_FILTERS, priceMinMinorUnits: 1000, priceMaxMinorUnits: 5000 }}
        onChange={vi.fn()}
        onClearAll={vi.fn()}
      />,
    );
    expect(screen.getAllByText(/€10 – €50/)).toHaveLength(1);
  });
});

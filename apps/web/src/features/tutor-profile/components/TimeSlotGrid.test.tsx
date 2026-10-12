import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { TimeSlotGrid } from './TimeSlotGrid';
import { TutorSlotDto } from '../api';

const slotA: TutorSlotDto = { startUtc: '2026-01-12T09:00:00.000Z', endUtc: '2026-01-12T09:50:00.000Z' };
const slotB: TutorSlotDto = { startUtc: '2026-01-12T10:00:00.000Z', endUtc: '2026-01-12T10:50:00.000Z' };

describe('TimeSlotGrid', () => {
  it('shows an empty-date message instead of a blank grid when there are no slots', () => {
    render(<TimeSlotGrid slots={[]} selectedStartUtc={null} onSelect={vi.fn()} />);

    expect(screen.getByText('No available times on this date. Try another date.')).toBeInTheDocument();
    expect(screen.queryByRole('radio')).not.toBeInTheDocument();
  });

  it('renders one radio per slot', () => {
    render(<TimeSlotGrid slots={[slotA, slotB]} selectedStartUtc={null} onSelect={vi.fn()} />);

    expect(screen.getAllByRole('radio')).toHaveLength(2);
  });

  it('checks the radio matching the selected slot', () => {
    render(<TimeSlotGrid slots={[slotA, slotB]} selectedStartUtc={slotB.startUtc} onSelect={vi.fn()} />);

    const radios = screen.getAllByRole('radio');
    expect(radios[0]).not.toBeChecked();
    expect(radios[1]).toBeChecked();
  });

  it('calls onSelect with the full slot object when a time is clicked', () => {
    const onSelect = vi.fn();
    render(<TimeSlotGrid slots={[slotA, slotB]} selectedStartUtc={null} onSelect={onSelect} />);

    fireEvent.click(screen.getAllByRole('radio')[0]);

    expect(onSelect).toHaveBeenCalledWith(slotA);
  });

  it('has no detectable axe violations with slots present', async () => {
    const { container } = render(
      <TimeSlotGrid slots={[slotA, slotB]} selectedStartUtc={slotA.startUtc} onSelect={vi.fn()} />,
    );
    expect((await axe(container)).violations).toEqual([]);
  });
});

import { render, screen, within, fireEvent } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { AvailabilityCalendar, CalendarDate } from './AvailabilityCalendar';

function makeDates(availability: boolean[]): CalendarDate[] {
  const base = new Date('2026-01-12T00:00:00.000Z');
  return availability.map((hasAvailability, i) => {
    const date = new Date(base.getTime() + i * 24 * 60 * 60 * 1000);
    return { key: date.toDateString(), date, hasAvailability };
  });
}

describe('AvailabilityCalendar', () => {
  it('renders one button per date, disabling dates with no availability', () => {
    const dates = makeDates([true, false, true, false, false, false, false]);
    render(
      <AvailabilityCalendar
        dates={dates}
        selectedDateKey={null}
        onSelectDate={vi.fn()}
        onPrevWeek={vi.fn()}
        onNextWeek={vi.fn()}
        canGoPrev={false}
        canGoNext={true}
      />,
    );

    const group = screen.getByRole('group', { name: 'Select a date' });
    const buttons = within(group).getAllByRole('button');
    expect(buttons).toHaveLength(7);
    expect(buttons[0]).toBeEnabled();
    expect(buttons[1]).toBeDisabled();
    expect(buttons[2]).toBeEnabled();
  });

  it('marks the selected date with aria-pressed', () => {
    const dates = makeDates([true, true]);
    render(
      <AvailabilityCalendar
        dates={dates}
        selectedDateKey={dates[1].key}
        onSelectDate={vi.fn()}
        onPrevWeek={vi.fn()}
        onNextWeek={vi.fn()}
        canGoPrev={false}
        canGoNext={true}
      />,
    );

    const group = screen.getByRole('group', { name: 'Select a date' });
    const buttons = within(group).getAllByRole('button');
    expect(buttons[0]).toHaveAttribute('aria-pressed', 'false');
    expect(buttons[1]).toHaveAttribute('aria-pressed', 'true');
  });

  it('clicking an available date calls onSelectDate with its key', () => {
    const dates = makeDates([true, true]);
    const onSelectDate = vi.fn();
    render(
      <AvailabilityCalendar
        dates={dates}
        selectedDateKey={null}
        onSelectDate={onSelectDate}
        onPrevWeek={vi.fn()}
        onNextWeek={vi.fn()}
        canGoPrev={false}
        canGoNext={true}
      />,
    );

    const group = screen.getByRole('group', { name: 'Select a date' });
    fireEvent.click(within(group).getAllByRole('button')[1]);

    expect(onSelectDate).toHaveBeenCalledWith(dates[1].key);
  });

  it('Prev/Next week buttons respect canGoPrev/canGoNext and call the right handlers', () => {
    const onPrevWeek = vi.fn();
    const onNextWeek = vi.fn();
    render(
      <AvailabilityCalendar
        dates={makeDates([true])}
        selectedDateKey={null}
        onSelectDate={vi.fn()}
        onPrevWeek={onPrevWeek}
        onNextWeek={onNextWeek}
        canGoPrev={false}
        canGoNext={true}
      />,
    );

    expect(screen.getByRole('button', { name: 'Previous week' })).toBeDisabled();
    fireEvent.click(screen.getByRole('button', { name: 'Next week' }));
    expect(onNextWeek).toHaveBeenCalledTimes(1);
    expect(onPrevWeek).not.toHaveBeenCalled();
  });

  it('has no detectable axe violations', async () => {
    const { container } = render(
      <AvailabilityCalendar
        dates={makeDates([true, false, true])}
        selectedDateKey={null}
        onSelectDate={vi.fn()}
        onPrevWeek={vi.fn()}
        onNextWeek={vi.fn()}
        canGoPrev={true}
        canGoNext={true}
      />,
    );
    expect((await axe(container)).violations).toEqual([]);
  });
});

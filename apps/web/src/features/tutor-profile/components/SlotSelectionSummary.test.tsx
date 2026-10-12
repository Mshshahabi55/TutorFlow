import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { axe } from 'vitest-axe';
import { SlotSelectionSummary } from './SlotSelectionSummary';

describe('SlotSelectionSummary', () => {
  it('announces the selection politely for assistive tech', () => {
    render(<SlotSelectionSummary startUtc="2026-01-14T10:00:00.000Z" endUtc="2026-01-14T10:50:00.000Z" />);

    const summary = screen.getByText(/Selected:/);
    expect(summary).toHaveAttribute('aria-live', 'polite');
  });

  it('has no detectable axe violations', async () => {
    const { container } = render(
      <SlotSelectionSummary startUtc="2026-01-14T10:00:00.000Z" endUtc="2026-01-14T10:50:00.000Z" />,
    );
    expect((await axe(container)).violations).toEqual([]);
  });
});

import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { TrialDurationSelector } from './TrialDurationSelector';

describe('TrialDurationSelector', () => {
  it('renders exactly the two approved trial durations, 25 and 50 minutes', () => {
    render(<TrialDurationSelector value={50} onChange={vi.fn()} />);

    expect(screen.getAllByRole('radio')).toHaveLength(2);
    expect(screen.getByRole('radio', { name: '25 min' })).toBeInTheDocument();
    expect(screen.getByRole('radio', { name: '50 min' })).toBeInTheDocument();
  });

  it('checks the radio matching the current value', () => {
    render(<TrialDurationSelector value={25} onChange={vi.fn()} />);

    expect(screen.getByRole('radio', { name: '25 min' })).toBeChecked();
    expect(screen.getByRole('radio', { name: '50 min' })).not.toBeChecked();
  });

  it('calls onChange with the selected duration', () => {
    const onChange = vi.fn();
    render(<TrialDurationSelector value={50} onChange={onChange} />);

    fireEvent.click(screen.getByRole('radio', { name: '25 min' }));

    expect(onChange).toHaveBeenCalledWith(25);
  });

  it('disables both options when disabled', () => {
    render(<TrialDurationSelector value={50} onChange={vi.fn()} disabled />);

    expect(screen.getByRole('radio', { name: '25 min' })).toBeDisabled();
    expect(screen.getByRole('radio', { name: '50 min' })).toBeDisabled();
  });

  it('has no detectable axe violations', async () => {
    const { container } = render(<TrialDurationSelector value={50} onChange={vi.fn()} />);
    expect((await axe(container)).violations).toEqual([]);
  });
});

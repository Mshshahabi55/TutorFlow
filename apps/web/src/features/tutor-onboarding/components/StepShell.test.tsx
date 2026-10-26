import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { StepShell } from './StepShell';

/**
 * jsdom does not evaluate `@media` queries against a real viewport and does not compute layout
 * (no `position: fixed`, no actual rect/overlap). These tests verify the footer's DOM structure
 * and interaction contract only — the same contract the mobile `@media (max-width: 767px)` rule
 * in `StepShell.module.css` must not change (it only repositions the existing markup, it does not
 * add/remove/rename anything). They do not and cannot prove the footer is actually fixed, that it
 * clears safe-area insets, or that it visually avoids overlapping content on a real device —
 * batch 76 already established no real browser is available in this environment for that.
 */
describe('StepShell', () => {
  it('renders the title, description, and children', () => {
    render(
      <StepShell title="A step" description="Some help text" onContinue={vi.fn()}>
        <p>Step body content</p>
      </StepShell>,
    );

    expect(screen.getByRole('heading', { name: 'A step' })).toBeInTheDocument();
    expect(screen.getByText('Some help text')).toBeInTheDocument();
    expect(screen.getByText('Step body content')).toBeInTheDocument();
  });

  it('omits the Back button when onBack is not provided (first step)', () => {
    render(
      <StepShell title="A step" onContinue={vi.fn()}>
        <p>Body</p>
      </StepShell>,
    );

    expect(screen.queryByRole('button', { name: 'Back' })).not.toBeInTheDocument();
  });

  it('renders Back and calls onBack when provided', () => {
    const onBack = vi.fn();
    render(
      <StepShell title="A step" onContinue={vi.fn()} onBack={onBack}>
        <p>Body</p>
      </StepShell>,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Back' }));

    expect(onBack).toHaveBeenCalledTimes(1);
  });

  it('calls onContinue with the default "Save & continue" label', () => {
    const onContinue = vi.fn();
    render(
      <StepShell title="A step" onContinue={onContinue}>
        <p>Body</p>
      </StepShell>,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    expect(onContinue).toHaveBeenCalledTimes(1);
  });

  it('supports a custom continue label (e.g. a final-step "Submit")', () => {
    render(
      <StepShell title="A step" onContinue={vi.fn()} continueLabel="Submit">
        <p>Body</p>
      </StepShell>,
    );

    expect(screen.getByRole('button', { name: 'Submit' })).toBeInTheDocument();
  });

  it('disables Continue when continueDisabled is set, independently of Back', () => {
    const onBack = vi.fn();
    render(
      <StepShell title="A step" onContinue={vi.fn()} onBack={onBack} continueDisabled>
        <p>Body</p>
      </StepShell>,
    );

    expect(screen.getByRole('button', { name: 'Save & continue' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Back' })).toBeEnabled();
  });

  it('disables both Back and Continue, and shows a loading indicator, while isSaving', () => {
    render(
      <StepShell title="A step" onContinue={vi.fn()} onBack={vi.fn()} isSaving>
        <p>Body</p>
      </StepShell>,
    );

    expect(screen.getByRole('button', { name: 'Back' })).toBeDisabled();
    const continueButton = screen.getByRole('button', { name: 'Save & continue' });
    expect(continueButton).toBeDisabled();
    expect(continueButton).toHaveAttribute('aria-busy', 'true');
  });

  it('surfaces an error message accessibly', () => {
    render(
      <StepShell title="A step" onContinue={vi.fn()} error="Something went wrong.">
        <p>Body</p>
      </StepShell>,
    );

    expect(screen.getByRole('alert')).toHaveTextContent('Something went wrong.');
  });

  it('renders no error element at all when error is absent', () => {
    render(
      <StepShell title="A step" onContinue={vi.fn()}>
        <p>Body</p>
      </StepShell>,
    );

    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('has no detectable axe violations', async () => {
    const { container } = render(
      <StepShell title="A step" description="Some help text" onContinue={vi.fn()} onBack={vi.fn()}>
        <p>Step body content</p>
      </StepShell>,
    );
    expect((await axe(container)).violations).toEqual([]);
  });
});

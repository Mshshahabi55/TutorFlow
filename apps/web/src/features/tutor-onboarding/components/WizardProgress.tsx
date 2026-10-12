import { WIZARD_STEPS, WizardStep } from '../types';
import styles from './WizardProgress.module.css';

const STEP_LABELS: Record<WizardStep, string> = {
  basics: 'Basics',
  photo: 'Photo',
  teaching: 'Teaching',
  credentials: 'Credentials',
  video: 'Intro video',
  availability: 'Availability',
  rate: 'Rate',
  review: 'Review',
};

export interface WizardProgressProps {
  currentStep: WizardStep;
  completedSteps: ReadonlySet<WizardStep>;
  onSelectStep: (step: WizardStep) => void;
}

/** A recommended best-practice addition, not a Preply-confirmed component (no progress
 * indicator for the tutor application itself was reliably observable — ADR-0026,
 * `PREPLY_WIZARD_COMPONENT_INVENTORY.md`). Steps are freely revisitable, not gated — "Do not
 * force users to restart the entire application" rules out a strictly linear-only stepper. */
export function WizardProgress({ currentStep, completedSteps, onSelectStep }: WizardProgressProps) {
  return (
    <ol className={styles.list} aria-label="Application sections">
      {WIZARD_STEPS.map((step, index) => (
        <li key={step}>
          <button
            type="button"
            className={styles.step}
            data-current={step === currentStep}
            data-complete={completedSteps.has(step)}
            aria-current={step === currentStep ? 'step' : undefined}
            onClick={() => onSelectStep(step)}
          >
            <span className={styles.index} aria-hidden="true">
              {completedSteps.has(step) ? '✓' : index + 1}
            </span>
            {STEP_LABELS[step]}
          </button>
        </li>
      ))}
    </ol>
  );
}

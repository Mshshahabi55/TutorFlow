import { ReactNode } from 'react';
import { Button } from '@/components/ui/Button';
import styles from './StepShell.module.css';

export interface StepShellProps {
  title: string;
  description?: string;
  children: ReactNode;
  onBack?: () => void;
  onContinue: () => void;
  continueLabel?: string;
  continueDisabled?: boolean;
  isSaving?: boolean;
  error?: string | null;
}

/** Shared chrome for every wizard step — title, body, error, Back/Continue footer. Not a
 * generic cross-project wizard engine (ADR-0026 §7); this composes existing primitives only
 * (`Button`), scoped to this one feature. */
export function StepShell({
  title,
  description,
  children,
  onBack,
  onContinue,
  continueLabel = 'Save & continue',
  continueDisabled,
  isSaving,
  error,
}: StepShellProps) {
  return (
    <div className={styles.step}>
      <h2 className={styles.title}>{title}</h2>
      {description && <p className={styles.description}>{description}</p>}

      <div className={styles.body}>{children}</div>

      {error && (
        <p className={styles.error} role="alert">
          {error}
        </p>
      )}

      <div className={styles.footer}>
        {onBack && (
          <Button type="button" variant="ghost" onClick={onBack} disabled={isSaving}>
            Back
          </Button>
        )}
        <Button type="button" onClick={onContinue} disabled={continueDisabled || isSaving} loading={isSaving}>
          {continueLabel}
        </Button>
      </div>
    </div>
  );
}

import { ReactNode } from 'react';
import styles from './EmptyState.module.css';
import { Button } from './Button';

export interface ErrorStateProps {
  title?: string;
  description?: string;
  onRetry?: () => void;
  retryLabel?: string;
  action?: ReactNode;
}

/** Per UX_STATES_PRINCIPLES.md: distinguish retryable vs. non-retryable errors; never show
 * a raw exception message to the user. */
export function ErrorState({
  title = 'Something went wrong',
  description = "We couldn't load this right now. Please try again.",
  onRetry,
  retryLabel = 'Try again',
  action,
}: ErrorStateProps) {
  return (
    <div className={styles.root} role="alert">
      <div className={styles.icon} aria-hidden="true">
        ⚠
      </div>
      <div className={styles.title}>{title}</div>
      <div className={styles.description}>{description}</div>
      {(onRetry || action) && (
        <div className={styles.actions}>
          {onRetry && <Button onClick={onRetry}>{retryLabel}</Button>}
          {action}
        </div>
      )}
    </div>
  );
}

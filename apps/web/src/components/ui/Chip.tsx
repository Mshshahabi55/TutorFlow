import { ReactNode } from 'react';
import styles from './Chip.module.css';

export interface ChipProps {
  children: ReactNode;
  variant?: 'accent' | 'neutral';
  onRemove?: () => void;
  removeLabel?: string;
}

export function Chip({ children, variant = 'accent', onRemove, removeLabel }: ChipProps) {
  return (
    <span className={[styles.chip, variant === 'neutral' ? styles.neutral : ''].join(' ')}>
      {children}
      {onRemove && (
        <button
          type="button"
          className={styles.removeButton}
          onClick={onRemove}
          aria-label={removeLabel ?? `Remove ${typeof children === 'string' ? children : 'filter'}`}
        >
          ✕
        </button>
      )}
    </span>
  );
}

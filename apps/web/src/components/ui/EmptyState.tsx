import { ReactNode } from 'react';
import styles from './EmptyState.module.css';

export interface EmptyStateProps {
  icon?: ReactNode;
  title: string;
  description?: string;
  action?: ReactNode;
}

/** Per UX_STATES_PRINCIPLES.md: an empty state must explain *why* and *what to do next* —
 * never a bare "No results." */
export function EmptyState({ icon, title, description, action }: EmptyStateProps) {
  return (
    <div className={styles.root}>
      {icon && <div className={styles.icon} aria-hidden="true">{icon}</div>}
      <div className={styles.title}>{title}</div>
      {description && <div className={styles.description}>{description}</div>}
      {action && <div className={styles.actions}>{action}</div>}
    </div>
  );
}

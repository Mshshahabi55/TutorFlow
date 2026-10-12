import { ReactNode } from 'react';
import styles from './Alert.module.css';

export type AlertVariant = 'info' | 'success' | 'warning' | 'error';

export interface AlertProps {
  variant: AlertVariant;
  title?: string;
  children: ReactNode;
  action?: ReactNode;
}

const ICONS: Record<AlertVariant, string> = {
  info: 'ⓘ',
  success: '✓',
  warning: '!',
  error: '✕',
};

export function Alert({ variant, title, children, action }: AlertProps) {
  const isUrgent = variant === 'error' || variant === 'warning';
  return (
    <div className={[styles.alert, styles[variant]].join(' ')} role={isUrgent ? 'alert' : 'status'}>
      <span className={styles.icon} aria-hidden="true">
        {ICONS[variant]}
      </span>
      <div className={styles.body}>
        {title && <div className={styles.title}>{title}</div>}
        <div>{children}</div>
        {action}
      </div>
    </div>
  );
}

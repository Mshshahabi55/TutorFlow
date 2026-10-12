import { ReactNode } from 'react';
import styles from './Badge.module.css';

export type BadgeVariant = 'info' | 'success' | 'warning' | 'neutral';

export function Badge({ variant = 'neutral', children }: { variant?: BadgeVariant; children: ReactNode }) {
  return <span className={[styles.badge, styles[variant]].join(' ')}>{children}</span>;
}

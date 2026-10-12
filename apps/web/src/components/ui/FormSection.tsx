import { ReactNode } from 'react';
import styles from './FormSection.module.css';

export function FormSection({ title, children }: { title: string; children: ReactNode }) {
  return (
    <fieldset className={styles.section}>
      <legend className={styles.title}>{title}</legend>
      {children}
    </fieldset>
  );
}

import { ReactNode, useId } from 'react';
import styles from './ProfileSection.module.css';

export function ProfileSection({ title, children }: { title: string; children: ReactNode }) {
  const headingId = useId();

  return (
    <section className={styles.root} aria-labelledby={headingId}>
      <h2 id={headingId} className={styles.heading}>
        {title}
      </h2>
      {children}
    </section>
  );
}

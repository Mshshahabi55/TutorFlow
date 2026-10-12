import styles from './LoadingState.module.css';

/** Action-shaped loading (page/section still resolving) — per UX_STATES_PRINCIPLES.md,
 * content-shaped loads should use Skeleton instead. */
export function LoadingState({ label = 'Loading…' }: { label?: string }) {
  return (
    <div className={styles.root} role="status">
      <span className={styles.spinner} aria-hidden="true" />
      <span>{label}</span>
    </div>
  );
}

import styles from './SlotSelectionSummary.module.css';

export interface SlotSelectionSummaryProps {
  startUtc: string;
  endUtc: string;
}

export function SlotSelectionSummary({ startUtc, endUtc }: SlotSelectionSummaryProps) {
  const start = new Date(startUtc);
  const end = new Date(endUtc);
  const dateLabel = start.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' });
  const startLabel = start.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' });
  const endLabel = end.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit', timeZoneName: 'short' });

  return (
    <p className={styles.summary} aria-live="polite">
      Selected:{' '}
      <strong>
        {dateLabel} · {startLabel}–{endLabel}
      </strong>{' '}
      (your local time)
    </p>
  );
}

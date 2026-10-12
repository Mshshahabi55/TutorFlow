import { TutorSlotDto } from '../api';
import styles from './TimeSlotGrid.module.css';

export interface TimeSlotGridProps {
  slots: TutorSlotDto[];
  selectedStartUtc: string | null;
  onSelect: (slot: TutorSlotDto) => void;
}

function formatTimeLabel(iso: string): string {
  return new Date(iso).toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' });
}

/** Times for the currently-selected date only — selecting here never creates a hold, it only
 * sets local component state (ADR-0025). The backend remains authoritative at the next phase's
 * actual booking-creation step; this list can go stale between fetch and click, same as any
 * other availability snapshot. */
export function TimeSlotGrid({ slots, selectedStartUtc, onSelect }: TimeSlotGridProps) {
  if (slots.length === 0) {
    return <p className={styles.empty}>No available times on this date. Try another date.</p>;
  }

  return (
    <fieldset className={styles.fieldset}>
      <legend className="visually-hidden">Select a time</legend>
      <div className={styles.grid}>
        {slots.map((slot) => (
          <span key={slot.startUtc}>
            <input
              type="radio"
              id={`time-slot-${slot.startUtc}`}
              name="tutor-availability-time-slot"
              className={styles.input}
              checked={selectedStartUtc === slot.startUtc}
              onChange={() => onSelect(slot)}
            />
            <label htmlFor={`time-slot-${slot.startUtc}`} className={styles.label}>
              {formatTimeLabel(slot.startUtc)}
            </label>
          </span>
        ))}
      </div>
    </fieldset>
  );
}

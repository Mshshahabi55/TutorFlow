import { Button } from '@/components/ui/Button';
import styles from './AvailabilityCalendar.module.css';

export interface CalendarDate {
  /** A stable key for this local calendar day — see `localDateKey` in TutorAvailability.tsx. */
  key: string;
  date: Date;
  hasAvailability: boolean;
}

export interface AvailabilityCalendarProps {
  dates: CalendarDate[];
  selectedDateKey: string | null;
  onSelectDate: (key: string) => void;
  onPrevWeek: () => void;
  onNextWeek: () => void;
  canGoPrev: boolean;
  canGoNext: boolean;
}

/** A horizontal, one-week-at-a-time date strip (matching the observed Preply pattern) rather
 * than a month calendar grid — paging is server-driven: each week change refetches real slots
 * for that window (ADR-0025), it never reconstructs availability from already-fetched data. */
export function AvailabilityCalendar({
  dates,
  selectedDateKey,
  onSelectDate,
  onPrevWeek,
  onNextWeek,
  canGoPrev,
  canGoNext,
}: AvailabilityCalendarProps) {
  return (
    <div className={styles.calendar}>
      <Button variant="ghost" size="sm" onClick={onPrevWeek} disabled={!canGoPrev} aria-label="Previous week">
        ‹
      </Button>
      <div className={styles.dateStrip} role="group" aria-label="Select a date">
        {dates.map((d) => (
          <button
            key={d.key}
            type="button"
            className={styles.dateButton}
            data-selected={selectedDateKey === d.key}
            disabled={!d.hasAvailability}
            aria-pressed={selectedDateKey === d.key}
            aria-label={d.date.toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric' })}
            onClick={() => onSelectDate(d.key)}
          >
            <span className={styles.dayOfWeek}>{d.date.toLocaleDateString(undefined, { weekday: 'short' })}</span>
            <span className={styles.dayNumber}>{d.date.getDate()}</span>
          </button>
        ))}
      </div>
      <Button variant="ghost" size="sm" onClick={onNextWeek} disabled={!canGoNext} aria-label="Next week">
        ›
      </Button>
    </div>
  );
}

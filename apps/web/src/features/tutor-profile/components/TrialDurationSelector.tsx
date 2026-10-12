import { TrialDurationMinutes, TRIAL_DURATIONS_MINUTES } from '../types';
import styles from './TrialDurationSelector.module.css';

export interface TrialDurationSelectorProps {
  value: TrialDurationMinutes;
  onChange: (value: TrialDurationMinutes) => void;
  disabled?: boolean;
}

/** A different duration triggers a real backend recomputation of which slots are open
 * (ADR-0025) — selecting here is never a client-side filter of a fixed slot list. */
export function TrialDurationSelector({ value, onChange, disabled }: TrialDurationSelectorProps) {
  return (
    <fieldset className={styles.fieldset}>
      <legend className={styles.legend}>Trial lesson length</legend>
      <div className={styles.options}>
        {TRIAL_DURATIONS_MINUTES.map((duration) => (
          <span key={duration}>
            <input
              type="radio"
              id={`trial-duration-${duration}`}
              name="trial-duration"
              className={styles.input}
              checked={value === duration}
              disabled={disabled}
              onChange={() => onChange(duration)}
            />
            <label htmlFor={`trial-duration-${duration}`} className={styles.label}>
              {duration} min
            </label>
          </span>
        ))}
      </div>
    </fieldset>
  );
}

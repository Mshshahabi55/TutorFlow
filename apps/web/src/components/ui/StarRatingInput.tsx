import styles from './StarRatingInput.module.css';

export interface StarRatingInputProps {
  name: string;
  value: number | null;
  onChange: (value: number) => void;
  'aria-describedby'?: string;
  'aria-invalid'?: boolean;
}

const STARS = [1, 2, 3, 4, 5];

/** A native radio group, not a custom roving-tabindex widget — every browser/screen reader
 * already knows how to operate a radio group with the keyboard (Tab in, arrow keys between
 * options, Space/click to select), so this is fully keyboard accessible without any bespoke
 * ARIA key-handling code to get subtly wrong. */
export function StarRatingInput({ name, value, onChange, 'aria-describedby': describedBy, 'aria-invalid': invalid }: StarRatingInputProps) {
  return (
    <div className={styles.root} role="radiogroup" aria-label="Rating" aria-describedby={describedBy} aria-invalid={invalid}>
      {STARS.map((star) => (
        <label key={star} className={styles.star} data-filled={value !== null && star <= value}>
          <input
            type="radio"
            name={name}
            value={star}
            checked={value === star}
            onChange={() => onChange(star)}
            className={styles.input}
          />
          <span aria-hidden="true">★</span>
          <span className="visually-hidden">{star} star{star === 1 ? '' : 's'}</span>
        </label>
      ))}
    </div>
  );
}

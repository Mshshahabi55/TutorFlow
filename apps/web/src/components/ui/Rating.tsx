import styles from './Rating.module.css';

export interface RatingProps {
  value: number;
  reviewCount: number;
}

function starsFor(value: number): string {
  const rounded = Math.round(value);
  return '★★★★★'.slice(0, rounded) + '☆☆☆☆☆'.slice(rounded);
}

export function Rating({ value, reviewCount }: RatingProps) {
  if (reviewCount === 0) {
    return <span className={styles.count}>No reviews yet</span>;
  }

  return (
    <span className={styles.root}>
      <span className={styles.stars} aria-hidden="true">
        {starsFor(value)}
      </span>
      <span className={styles.value}>{value.toFixed(1)}</span>
      <span className={styles.count}>
        ({reviewCount} review{reviewCount === 1 ? '' : 's'})
      </span>
    </span>
  );
}

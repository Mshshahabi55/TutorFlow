import { CSSProperties } from 'react';
import styles from './Skeleton.module.css';

export function Skeleton({ width, height = '1em', className }: { width?: string | number; height?: string | number; className?: string }) {
  const style: CSSProperties = { width, height };
  return <span className={[styles.skeleton, className ?? ''].join(' ')} style={style} aria-hidden="true" />;
}

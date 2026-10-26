'use client';

import * as RadixAvatar from '@radix-ui/react-avatar';
import styles from './Avatar.module.css';

export interface AvatarProps {
  src?: string | null;
  name: string;
  size?: 'sm' | 'md' | 'lg' | 'xl';
  /** Defaults to the usual circular avatar; 'square' renders a rounded-rectangle frame instead —
   * matches the real Preply wizard's Photo-step preview, which uses a portrait-rectangle photo
   * tile, not a circle (docs/product/preply-reverse-engineering/evidence/tutor.zip,
   * Screenshot 2026-10-06 074216.png). */
  shape?: 'circle' | 'square';
}

function initials(name: string): string {
  const parts = name.trim().split(/\s+/);
  return parts
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase() ?? '')
    .join('');
}

export function Avatar({ src, name, size = 'md', shape = 'circle' }: AvatarProps) {
  return (
    <RadixAvatar.Root className={[styles.root, styles[size], shape === 'square' ? styles.square : ''].join(' ')}>
      {src && <RadixAvatar.Image className={styles.image} src={src} alt={name} />}
      <RadixAvatar.Fallback aria-hidden={Boolean(src)}>{initials(name)}</RadixAvatar.Fallback>
    </RadixAvatar.Root>
  );
}

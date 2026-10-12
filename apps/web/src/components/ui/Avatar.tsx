'use client';

import * as RadixAvatar from '@radix-ui/react-avatar';
import styles from './Avatar.module.css';

export interface AvatarProps {
  src?: string | null;
  name: string;
  size?: 'sm' | 'md' | 'lg';
}

function initials(name: string): string {
  const parts = name.trim().split(/\s+/);
  return parts
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase() ?? '')
    .join('');
}

export function Avatar({ src, name, size = 'md' }: AvatarProps) {
  return (
    <RadixAvatar.Root className={[styles.root, styles[size]].join(' ')}>
      {src && <RadixAvatar.Image className={styles.image} src={src} alt={name} />}
      <RadixAvatar.Fallback aria-hidden={Boolean(src)}>{initials(name)}</RadixAvatar.Fallback>
    </RadixAvatar.Root>
  );
}

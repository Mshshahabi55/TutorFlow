'use client';

import * as Dialog from '@radix-ui/react-dialog';
import { ReactNode } from 'react';
import styles from './Drawer.module.css';

export interface DrawerProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  children: ReactNode;
  footer?: ReactNode;
}

/**
 * Mobile filter sheet (bottom sheet on phones, side panel from tablet up — see the
 * responsive @media block in Drawer.module.css). Built on the same Radix Dialog primitive
 * as Modal for identical focus-trap/Esc/focus-restore behavior, just different positioning.
 */
export function Drawer({ open, onOpenChange, title, children, footer }: DrawerProps) {
  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className={styles.overlay} />
        <Dialog.Content className={styles.content} aria-describedby={undefined}>
          <div className={styles.header}>
            <Dialog.Title className={styles.title}>{title}</Dialog.Title>
            <Dialog.Close className={styles.closeButton} aria-label="Close">
              ✕
            </Dialog.Close>
          </div>
          {children}
          {footer && <div className={styles.footer}>{footer}</div>}
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}

'use client';

import * as RadixCheckbox from '@radix-ui/react-checkbox';
import { ReactNode, useId } from 'react';
import styles from './Checkbox.module.css';

export interface CheckboxProps {
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
  children: ReactNode;
  disabled?: boolean;
}

export function Checkbox({ checked, onCheckedChange, children, disabled }: CheckboxProps) {
  const id = useId();
  return (
    <label htmlFor={id} className={styles.label}>
      <RadixCheckbox.Root
        id={id}
        className={styles.root}
        checked={checked}
        onCheckedChange={(c) => onCheckedChange(c === true)}
        disabled={disabled}
      >
        <RadixCheckbox.Indicator className={styles.indicator}>✓</RadixCheckbox.Indicator>
      </RadixCheckbox.Root>
      {children}
    </label>
  );
}

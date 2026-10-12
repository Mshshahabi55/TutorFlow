'use client';

import * as Popover from '@radix-ui/react-popover';
import styles from './MultiSelect.module.css';
import { Checkbox } from './Checkbox';

export interface MultiSelectOption {
  value: string;
  label: string;
}

export interface MultiSelectProps {
  options: MultiSelectOption[];
  selected: string[];
  onChange: (selected: string[]) => void;
  placeholder: string;
  'aria-label': string;
}

/**
 * Searchable-combobox's simpler sibling: a popover checkbox list. Chosen over a full
 * type-to-filter combobox because V1's option counts (specialties, languages) are small
 * enough that scanning beats typing — re-evaluate if a catalog grows past ~30 options.
 */
export function MultiSelect({
  options,
  selected,
  onChange,
  placeholder,
  ...rest
}: MultiSelectProps) {
  const toggle = (value: string) => {
    onChange(selected.includes(value) ? selected.filter((v) => v !== value) : [...selected, value]);
  };

  const triggerLabel =
    selected.length === 0
      ? placeholder
      : `${selected.length} selected`;

  return (
    <Popover.Root>
      <Popover.Trigger asChild>
        <button type="button" className={styles.trigger} aria-label={rest['aria-label']}>
          <span className={selected.length === 0 ? styles.placeholder : undefined}>
            {triggerLabel}
          </span>
          <span aria-hidden="true">▾</span>
        </button>
      </Popover.Trigger>
      <Popover.Portal>
        <Popover.Content className={styles.content} sideOffset={4} align="start">
          {options.map((option) => (
            <div key={option.value} className={styles.option}>
              <Checkbox checked={selected.includes(option.value)} onCheckedChange={() => toggle(option.value)}>
                {option.label}
              </Checkbox>
            </div>
          ))}
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  );
}

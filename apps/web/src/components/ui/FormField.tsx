import { ReactNode, useId } from 'react';
import styles from './FormField.module.css';

export interface FormFieldRenderProps {
  id: string;
  'aria-describedby'?: string;
  'aria-invalid'?: boolean;
}

export interface FormFieldProps {
  label: string;
  required?: boolean;
  helperText?: string;
  errorText?: string;
  hideLabel?: boolean;
  children: (props: FormFieldRenderProps) => ReactNode;
}

/**
 * Wires up label/helper/error association via aria-describedby so screen readers announce
 * the constraint and the error together with the control — not just a visually-styled message
 * (docs/design/ACCESSIBILITY_REQUIREMENTS.md: "Form labels & errors").
 */
export function FormField({
  label,
  required,
  helperText,
  errorText,
  hideLabel = false,
  children,
}: FormFieldProps) {
  const id = useId();
  const helperId = helperText ? `${id}-helper` : undefined;
  const errorId = errorText ? `${id}-error` : undefined;
  const describedBy = [helperId, errorId].filter(Boolean).join(' ') || undefined;

  return (
    <div className={styles.field}>
      <label htmlFor={id} className={hideLabel ? 'visually-hidden' : styles.label}>
        {label}
        {required && (
          <span className={styles.required} aria-hidden="true">
            {' '}
            *
          </span>
        )}
      </label>
      {children({ id, 'aria-describedby': describedBy, 'aria-invalid': Boolean(errorText) })}
      {helperText && !errorText && (
        <span id={helperId} className={styles.helper}>
          {helperText}
        </span>
      )}
      {errorText && (
        <span id={errorId} role="alert" className={styles.error}>
          {errorText}
        </span>
      )}
    </div>
  );
}

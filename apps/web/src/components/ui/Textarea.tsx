import { TextareaHTMLAttributes, forwardRef } from 'react';
import styles from './Input.module.css';

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaHTMLAttributes<HTMLTextAreaElement>>(
  ({ className, ...rest }, ref) => (
    <textarea
      ref={ref}
      className={[styles.input, styles.textarea, className ?? ''].join(' ')}
      {...rest}
    />
  ),
);
Textarea.displayName = 'Textarea';

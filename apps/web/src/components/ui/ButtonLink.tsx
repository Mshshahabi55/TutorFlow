import Link, { LinkProps } from 'next/link';
import { AnchorHTMLAttributes } from 'react';
import styles from './Button.module.css';
import { ButtonSize, ButtonVariant } from './Button';

export interface ButtonLinkProps
  extends LinkProps,
    Omit<AnchorHTMLAttributes<HTMLAnchorElement>, keyof LinkProps> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  fullWidth?: boolean;
}

export function ButtonLink({
  variant = 'primary',
  size = 'md',
  fullWidth = false,
  className,
  children,
  ...rest
}: ButtonLinkProps) {
  return (
    <Link
      className={[
        styles.button,
        styles[variant],
        size !== 'md' ? styles[size] : '',
        fullWidth ? styles.fullWidth : '',
        className ?? '',
      ]
        .filter(Boolean)
        .join(' ')}
      {...rest}
    >
      {children}
    </Link>
  );
}

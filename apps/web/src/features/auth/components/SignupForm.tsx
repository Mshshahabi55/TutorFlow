'use client';

import { FormEvent, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { FormField } from '@/components/ui/FormField';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { useRegister } from '../useAuthMutations';
import { getAuthErrorMessage } from '../errorMessage';
import { PasswordInput } from './PasswordInput';
import styles from './AuthForm.module.css';

const EMAIL_PATTERN = /^\S+@\S+\.\S+$/;
const MIN_PASSWORD_LENGTH = 8;

export interface SignupFormProps {
  returnTo: string;
}

// No password-confirmation field — matches Preply's actual signup form (confirmed via live
// benchmark research this phase) and TutorFlow's existing backend contract (Register.Command
// has no ConfirmPassword). Only email + password are collected; profile data stays out of basic
// account creation (tutor onboarding is a separate flow — ADR-0002, ROLES_AND_PERMISSIONS.md).
export function SignupForm({ returnTo }: SignupFormProps) {
  const router = useRouter();
  const register = useRegister();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fieldErrors, setFieldErrors] = useState<{ email?: string; password?: string }>({});

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const errors: typeof fieldErrors = {};
    if (!email.trim()) {
      errors.email = 'Enter your email address.';
    } else if (!EMAIL_PATTERN.test(email.trim())) {
      errors.email = 'Enter a valid email address.';
    }
    if (!password) {
      errors.password = 'Create a password.';
    } else if (password.length < MIN_PASSWORD_LENGTH) {
      errors.password = `Password must be at least ${MIN_PASSWORD_LENGTH} characters.`;
    }
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) {
      return;
    }

    register.mutate(
      { email: email.trim(), password },
      { onSuccess: () => router.replace(returnTo) },
    );
  }

  return (
    <form className={styles.form} onSubmit={handleSubmit} noValidate>
      <FormField label="Email" required errorText={fieldErrors.email}>
        {(fieldProps) => (
          <Input
            {...fieldProps}
            type="email"
            autoComplete="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            disabled={register.isPending}
          />
        )}
      </FormField>

      <FormField
        label="Password"
        required
        helperText={`At least ${MIN_PASSWORD_LENGTH} characters.`}
        errorText={fieldErrors.password}
      >
        {(fieldProps) => (
          <PasswordInput
            {...fieldProps}
            autoComplete="new-password"
            value={password}
            onChange={setPassword}
            disabled={register.isPending}
          />
        )}
      </FormField>

      {register.isError && (
        <p className={styles.formError} role="alert">
          {getAuthErrorMessage(register.error)}
        </p>
      )}

      <Button type="submit" fullWidth loading={register.isPending}>
        Sign up
      </Button>

      <p className={styles.switchLine}>
        Already have an account?{' '}
        <Link href={returnTo === '/' ? '/login' : `/login?returnTo=${encodeURIComponent(returnTo)}`}>Log in</Link>
      </p>
    </form>
  );
}

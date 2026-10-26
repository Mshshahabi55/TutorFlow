'use client';

import { FormEvent, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { FormField } from '@/components/ui/FormField';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { useLogin } from '../useAuthMutations';
import { getAuthErrorMessage } from '../errorMessage';
import { buildAuthHref } from '../authHref';
import { ExternalLoginButtons } from './ExternalLoginButtons';
import { PasswordInput } from './PasswordInput';
import styles from './AuthForm.module.css';

const EMAIL_PATTERN = /^\S+@\S+\.\S+$/;

export interface LoginFormProps {
  returnTo: string;
  /** Relayed onward to the Sign up link only — Login's own heading/copy never changes by context
   * (it stays the plain unified page). Without this, a user bouncing Become-a-tutor -> Login ->
   * Sign up would lose the tutor-flavored Signup heading the moment they touched Login. */
  context?: 'tutor';
}

export function LoginForm({ returnTo, context }: LoginFormProps) {
  const router = useRouter();
  const login = useLogin();
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
      errors.password = 'Enter your password.';
    }
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) {
      return;
    }

    login.mutate(
      { email: email.trim(), password },
      { onSuccess: () => router.replace(returnTo) },
    );
  }

  return (
    <div className={styles.stack}>
      <ExternalLoginButtons returnTo={returnTo} />

      <form className={styles.form} onSubmit={handleSubmit} noValidate>
        <FormField label="Email" required errorText={fieldErrors.email}>
          {(fieldProps) => (
            <Input
              {...fieldProps}
              type="email"
              autoComplete="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              disabled={login.isPending}
            />
          )}
        </FormField>

        <FormField label="Password" required errorText={fieldErrors.password}>
          {(fieldProps) => (
            <PasswordInput
              {...fieldProps}
              autoComplete="current-password"
              value={password}
              onChange={setPassword}
              disabled={login.isPending}
            />
          )}
        </FormField>

        {login.isError && (
          <p className={styles.formError} role="alert">
            {getAuthErrorMessage(login.error)}
          </p>
        )}

        <Button type="submit" fullWidth loading={login.isPending}>
          Log in
        </Button>

        <p className={styles.switchLine}>
          Don&apos;t have an account?{' '}
          <Link href={buildAuthHref('/signup', returnTo, context)}>Sign up</Link>
        </p>
      </form>
    </div>
  );
}

'use client';

import { FormEvent, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { FormField } from '@/components/ui/FormField';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { useLogin } from '../useAuthMutations';
import { getAuthErrorMessage } from '../errorMessage';
import { PasswordInput } from './PasswordInput';
import styles from './AuthForm.module.css';

const EMAIL_PATTERN = /^\S+@\S+\.\S+$/;

export interface LoginFormProps {
  returnTo: string;
}

export function LoginForm({ returnTo }: LoginFormProps) {
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
        <Link href={returnTo === '/' ? '/signup' : `/signup?returnTo=${encodeURIComponent(returnTo)}`}>Sign up</Link>
      </p>
    </form>
  );
}

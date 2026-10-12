'use client';

import { useMutation } from '@tanstack/react-query';
import { becomeTutor, login, logout, register } from './api';
import { useSessionCache } from './useAuth';
import { LoginInput, RegisterInput } from './types';

export function useRegister() {
  const { setSession } = useSessionCache();
  return useMutation({
    mutationFn: (input: RegisterInput) => register(input),
    onSuccess: (session) => setSession(session),
  });
}

export function useLogin() {
  const { setSession } = useSessionCache();
  return useMutation({
    mutationFn: (input: LoginInput) => login(input),
    onSuccess: (session) => setSession(session),
  });
}

export function useLogout() {
  const { setSession } = useSessionCache();
  return useMutation({
    mutationFn: () => logout(),
    onSuccess: () => setSession(null),
  });
}

export function useBecomeTutor() {
  const { setSession } = useSessionCache();
  return useMutation({
    mutationFn: () => becomeTutor(),
    onSuccess: (session) => setSession(session),
  });
}

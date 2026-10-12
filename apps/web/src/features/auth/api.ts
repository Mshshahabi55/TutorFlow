import { apiGet, apiPost, ApiError } from '@/lib/api/client';
import { LoginInput, RegisterInput, Session } from './types';

export async function getCurrentUser(signal?: AbortSignal): Promise<Session | null> {
  try {
    return await apiGet<Session>('/api/identity/me', undefined, { signal });
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      return null;
    }
    throw error;
  }
}

export function register(input: RegisterInput): Promise<Session> {
  return apiPost<Session, RegisterInput>('/api/identity/register', input);
}

export function login(input: LoginInput): Promise<Session> {
  return apiPost<Session, LoginInput>('/api/identity/login', input);
}

export function logout(): Promise<void> {
  return apiPost<void>('/api/identity/logout');
}

/** Grants the caller's own account the Tutor role, idempotently (ADR-0026) — the only way any
 * account ever gains it; reissues the session, so the returned roles already include it. */
export function becomeTutor(): Promise<Session> {
  return apiPost<Session>('/api/identity/me/become-tutor');
}

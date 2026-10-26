/**
 * Typed data-access boundary, bottom layer:
 *
 *   UI components
 *     -> feature query layer (src/features/*\/api.ts + React Query hooks)
 *       -> this typed API client
 *         -> TutorFlow backend (TutorFlow.Host)
 *
 * No UI component talks to `fetch` directly. This is the only module that knows the base
 * URL, request/response envelope, and ProblemDetails shape — swapping transport details
 * later never touches a component.
 */

// Exported so the external (Google/Facebook) login links can build a full-page-navigation href
// to the backend's OAuth challenge endpoints (ADR-0031) — those are real redirects, not fetch
// calls, so they live outside the apiGet/apiPost helpers below but still need this same origin.
export const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5148';

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  // FluentValidation's ValidationProblem serializes this as a field-keyed object; Identity's
  // ToProblemDetails (business failures like "invalid credentials") serializes it as a flat
  // string array instead — both shapes are real, see features/auth/errorMessage.ts.
  errors?: Record<string, string[]> | string[];
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem: ProblemDetails,
  ) {
    super(problem.title ?? `Request failed with status ${status}`);
    this.name = 'ApiError';
  }

  /** True for validation-shaped 400s that carry field errors — distinct from a generic failure. */
  get isValidationError(): boolean {
    return this.status === 400 && Boolean(this.problem.errors);
  }
}

export interface ApiGetOptions {
  signal?: AbortSignal;
}

export async function apiGet<TResponse>(
  path: string,
  params?: Record<string, string | undefined>,
  options?: ApiGetOptions,
): Promise<TResponse> {
  const url = new URL(path, API_BASE_URL);
  if (params) {
    for (const [key, value] of Object.entries(params)) {
      if (value !== undefined && value !== '') {
        url.searchParams.set(key, value);
      }
    }
  }

  const response = await fetch(url, {
    method: 'GET',
    headers: { Accept: 'application/json' },
    // The httpOnly session cookie (ADR-0023) lives on the API's origin, not the frontend's —
    // without this, the browser never attaches it to a cross-origin request.
    credentials: 'include',
    signal: options?.signal,
  });

  if (!response.ok) {
    const problem: ProblemDetails = await response.json().catch(() => ({}));
    throw new ApiError(response.status, problem);
  }

  return (await response.json()) as TResponse;
}

export interface ApiMutationOptions {
  signal?: AbortSignal;
}

async function sendJson<TResponse>(
  method: 'PUT' | 'POST' | 'DELETE' | 'PATCH',
  path: string,
  body?: unknown,
  options?: ApiMutationOptions,
): Promise<TResponse> {
  const response = await fetch(new URL(path, API_BASE_URL), {
    method,
    headers: body === undefined ? { Accept: 'application/json' } : { Accept: 'application/json', 'Content-Type': 'application/json' },
    credentials: 'include',
    body: body === undefined ? undefined : JSON.stringify(body),
    signal: options?.signal,
  });

  if (!response.ok) {
    const problem: ProblemDetails = await response.json().catch(() => ({}));
    throw new ApiError(response.status, problem);
  }

  if (response.status === 204) {
    return undefined as TResponse;
  }

  return (await response.json()) as TResponse;
}

export function apiPut<TResponse, TBody = unknown>(
  path: string,
  body?: TBody,
  options?: ApiMutationOptions,
): Promise<TResponse> {
  return sendJson<TResponse>('PUT', path, body, options);
}

export function apiDelete<TResponse = void>(path: string, options?: ApiMutationOptions): Promise<TResponse> {
  return sendJson<TResponse>('DELETE', path, undefined, options);
}

export function apiPost<TResponse, TBody = unknown>(
  path: string,
  body?: TBody,
  options?: ApiMutationOptions,
): Promise<TResponse> {
  return sendJson<TResponse>('POST', path, body, options);
}

export function apiPatch<TResponse, TBody = unknown>(
  path: string,
  body?: TBody,
  options?: ApiMutationOptions,
): Promise<TResponse> {
  return sendJson<TResponse>('PATCH', path, body, options);
}

/**
 * For multipart/form-data bodies (file uploads) — deliberately does not set a Content-Type
 * header; the browser sets it itself with the correct multipart boundary, which `sendJson`'s
 * fixed `application/json` header can't express.
 */
export async function apiPostFormData<TResponse>(
  path: string,
  formData: FormData,
  options?: ApiMutationOptions,
): Promise<TResponse> {
  const response = await fetch(new URL(path, API_BASE_URL), {
    method: 'POST',
    headers: { Accept: 'application/json' },
    credentials: 'include',
    body: formData,
    signal: options?.signal,
  });

  if (!response.ok) {
    const problem: ProblemDetails = await response.json().catch(() => ({}));
    throw new ApiError(response.status, problem);
  }

  return (await response.json()) as TResponse;
}

import axios, { type AxiosInstance } from "axios";
import { ApiRequestError } from "@/services/api/ApiRequestError";
import type { ApiError } from "@/services/api/apiTypes";

const DEFAULT_BASE_URL = "http://localhost:5046";

function resolveBaseUrl(): string {
  const configured = import.meta.env.VITE_API_BASE_URL;
  return configured && configured.length > 0 ? configured : DEFAULT_BASE_URL;
}

function isApiError(value: unknown): value is ApiError {
  return (
    typeof value === "object" &&
    value !== null &&
    "code" in value &&
    "message" in value &&
    "type" in value
  );
}

function extractErrorPayload(body: unknown): unknown {
  if (body && typeof body === "object" && "error" in body) {
    return body.error;
  }

  return undefined;
}

/**
 * Converts an Axios error carrying the backend's ApiError body into a typed
 * ApiRequestError; any other error (network failure, timeout, malformed
 * response with no parseable body) is returned as a real Error rather than
 * fabricated into an ApiRequestError. Extracted as a pure function so it's
 * unit-testable without a real HTTP call.
 */
export function normalizeApiError(error: unknown): Error {
  if (axios.isAxiosError(error)) {
    const errorPayload = extractErrorPayload(error.response?.data);

    if (isApiError(errorPayload)) {
      return new ApiRequestError(errorPayload, error.response?.status ?? 0);
    }

    return error;
  }

  if (error instanceof Error) {
    return error;
  }

  return new Error(typeof error === "string" ? error : "An unknown error occurred.");
}

/**
 * The single Axios instance every feature module's API calls go through.
 * The bearer token (docs/adr/ADR-017-authentication-mechanism-decision.md)
 * is held in a plain module-level variable, not React state, so this
 * vanilla-JS request interceptor can read it without depending on React —
 * AuthProvider calls setAuthToken/clearAuthToken whenever the session
 * changes; nothing else should call them directly.
 */
let currentAuthToken: string | null = null;

export function setAuthToken(token: string): void {
  currentAuthToken = token;
}

export function clearAuthToken(): void {
  currentAuthToken = null;
}

export const apiClient: AxiosInstance = axios.create({
  baseURL: resolveBaseUrl(),
  headers: {
    "Content-Type": "application/json",
  },
});

apiClient.interceptors.request.use((config) => {
  if (currentAuthToken) {
    config.headers.Authorization = `Bearer ${currentAuthToken}`;
  }

  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error: unknown) => Promise.reject(normalizeApiError(error)),
);

/**
 * Extracts `.value` from a successful ApiResult&lt;T&gt;. Only ever called
 * with a response the interceptor above has already let through as
 * successful (a failure is thrown as ApiRequestError before reaching here),
 * so the `isSuccess` check is a defensive type-narrowing, not a live branch.
 */
export function unwrapValue<T>(result: { isSuccess: boolean; value?: T }): T {
  if (!result.isSuccess || result.value === undefined) {
    throw new Error("Expected a successful API result carrying a value.");
  }

  return result.value;
}

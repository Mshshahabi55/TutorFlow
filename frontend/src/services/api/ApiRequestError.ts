import { ErrorType, type ApiError } from "@/services/api/apiTypes";

/**
 * Normalizes a backend Domain Error or Infrastructure Failure into a thrown
 * error, so TanStack Query's own error state works idiomatically instead of
 * every caller manually checking `result.isFailure`. Carries exactly what
 * the backend already classified (ADR-008) — this class adds no new
 * business meaning of its own.
 */
export class ApiRequestError extends Error {
  readonly code: string;
  readonly type: ErrorType;
  readonly status: number;
  readonly traceId?: string | null;

  constructor(apiError: ApiError, status: number) {
    super(apiError.message);
    this.name = "ApiRequestError";
    this.code = apiError.code;
    this.type = apiError.type;
    this.status = status;
    this.traceId = apiError.traceId;
  }

  /** True for a 4xx business-rule rejection; false for a 5xx infrastructure failure. */
  get isDomainError(): boolean {
    return this.type === ErrorType.Domain;
  }
}

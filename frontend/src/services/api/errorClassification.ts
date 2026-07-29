import { ApiRequestError } from "@/services/api/ApiRequestError";

/** A lookup that failed because the id genuinely does not resolve (backend 404) — distinct from a network/server failure, which deserves a plain retry instead of "choose a different one." */
export function isNotFoundError(error: unknown): boolean {
  return error instanceof ApiRequestError && error.status === 404;
}

/** A write rejected because the resource it targeted changed state first (backend 409 — e.g. an Availability Slot another Student just booked) — distinct from a validation or infrastructure failure. */
export function isConflictError(error: unknown): boolean {
  return error instanceof ApiRequestError && error.status === 409;
}

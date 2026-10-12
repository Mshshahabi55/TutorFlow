import { ApiError } from '@/lib/api/client';

/**
 * The same `problem.errors` field is a flat string array for business failures (invalid
 * credentials, duplicate email — Identity's `ResultExtensions.ToProblemDetails`) but a
 * field-keyed object for FluentValidation failures (`TypedResults.ValidationProblem`). Both are
 * handled here so a raw ProblemDetails shape never reaches the user — only a plain sentence.
 */
export function getAuthErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const errors = error.problem.errors;
    if (Array.isArray(errors) && errors.length > 0) {
      return errors.join(' ');
    }
    if (errors && typeof errors === 'object') {
      const values = Object.values(errors).flat();
      if (values.length > 0) {
        return values.join(' ');
      }
    }
    return error.problem.detail ?? 'Something went wrong. Please try again.';
  }
  return 'Something went wrong. Please try again.';
}

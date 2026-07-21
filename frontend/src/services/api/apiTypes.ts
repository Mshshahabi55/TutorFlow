// Mirrors TutorFlow.Web's response envelope exactly
// (src/Web/Endpoints/ApiResponse.cs, src/Web/Endpoints/ResultMapping.cs).
// The backend is frozen for this sprint — these shapes are read from the
// actual C# source, not guessed. Two distinct success shapes exist because
// the backend itself returns two distinct C# types: ApiResponse (no value)
// for commands, ApiResponse<TValue> (with value) for queries — a failure of
// either kind is the same ApiResponse shape with no "value" key.

/** Mirrors TutorFlow.Application.Common.ErrorType. */
export enum ErrorType {
  None = 0,
  Domain = 1,
  Infrastructure = 2,
}

export interface ApiError {
  code: string;
  message: string;
  type: ErrorType;
  traceId?: string | null;
}

/** The envelope for an endpoint that returns no value on success (e.g. ApproveTutor). */
export type VoidApiResult =
  | { isSuccess: true; isFailure: false; error: null }
  | { isSuccess: false; isFailure: true; error: ApiError };

/** The envelope for an endpoint that returns a value on success (e.g. GetTutorById). */
export type ApiResult<T> =
  | { isSuccess: true; isFailure: false; error: null; value: T }
  | { isSuccess: false; isFailure: true; error: ApiError };

/** Mirrors TutorFlow.Application.Common.PagedResult&lt;T&gt;. */
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

import { describe, expect, it } from "vitest";
import { AxiosError, AxiosHeaders } from "axios";
import { normalizeApiError, unwrapValue } from "@/services/api/apiClient";
import { ApiRequestError } from "@/services/api/ApiRequestError";
import { ErrorType } from "@/services/api/apiTypes";

function makeAxiosError(status: number, data: unknown): AxiosError {
  const error = new AxiosError("Request failed", "ERR_BAD_REQUEST");
  error.response = {
    status,
    statusText: "",
    headers: {},
    config: { headers: new AxiosHeaders() },
    data,
  };
  return error;
}

describe("normalizeApiError", () => {
  it("converts a 404 response carrying an ApiError body into an ApiRequestError", () => {
    const axiosError = makeAxiosError(404, {
      isSuccess: false,
      isFailure: true,
      error: { code: "GetTutorByIdQuery.NotFound", message: "Tutor was not found.", type: ErrorType.Domain },
    });

    const normalized = normalizeApiError(axiosError);

    expect(normalized).toBeInstanceOf(ApiRequestError);
    const apiRequestError = normalized as ApiRequestError;
    expect(apiRequestError.code).toBe("GetTutorByIdQuery.NotFound");
    expect(apiRequestError.message).toBe("Tutor was not found.");
    expect(apiRequestError.status).toBe(404);
    expect(apiRequestError.isDomainError).toBe(true);
  });

  it("converts a 500 infrastructure failure and reports isDomainError as false", () => {
    const axiosError = makeAxiosError(500, {
      isSuccess: false,
      isFailure: true,
      error: {
        code: "Infrastructure.UnexpectedFailure",
        message: "The request could not be completed. Please try again.",
        type: ErrorType.Infrastructure,
        traceId: "trace-123",
      },
    });

    const normalized = normalizeApiError(axiosError) as ApiRequestError;

    expect(normalized).toBeInstanceOf(ApiRequestError);
    expect(normalized.isDomainError).toBe(false);
    expect(normalized.traceId).toBe("trace-123");
  });

  it("passes through a network error with no parseable ApiError body unchanged", () => {
    const axiosError = new AxiosError("Network Error", "ERR_NETWORK");

    const normalized = normalizeApiError(axiosError);

    expect(normalized).toBe(axiosError);
  });

  it("passes through a non-Axios error unchanged", () => {
    const genericError = new Error("boom");

    const normalized = normalizeApiError(genericError);

    expect(normalized).toBe(genericError);
  });
});

describe("unwrapValue", () => {
  it("returns the value from a successful result", () => {
    expect(unwrapValue({ isSuccess: true, value: 42 })).toBe(42);
  });

  it("throws when the result did not succeed", () => {
    expect(() => unwrapValue({ isSuccess: false })).toThrow();
  });
});

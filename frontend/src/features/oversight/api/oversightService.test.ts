import { afterEach, describe, expect, it, vi } from "vitest";
import { AxiosHeaders, type AxiosResponse } from "axios";
import { apiClient } from "@/services/api/apiClient";
import { fetchAllSessions } from "@/features/oversight/api/oversightService";

function mockResponse<T>(data: T): AxiosResponse<T> {
  return {
    data,
    status: 200,
    statusText: "OK",
    headers: {},
    config: { headers: new AxiosHeaders() },
  } as AxiosResponse<T>;
}

describe("oversightService", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("fetchAllSessions GETs /sessions with page/pageSize params", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue(
      mockResponse({
        isSuccess: true,
        isFailure: false,
        error: null,
        value: { items: [], totalCount: 0, page: 1, pageSize: 20 },
      }),
    );

    await fetchAllSessions(1, 20);

    expect(get).toHaveBeenCalledWith("/sessions", { params: { page: 1, pageSize: 20 } });
  });
});

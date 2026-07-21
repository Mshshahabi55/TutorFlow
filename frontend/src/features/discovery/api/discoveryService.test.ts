import { afterEach, describe, expect, it, vi } from "vitest";
import { AxiosHeaders, type AxiosResponse } from "axios";
import { apiClient } from "@/services/api/apiClient";
import { searchTutors } from "@/features/discovery/api/discoveryService";

function mockResponse<T>(data: T): AxiosResponse<T> {
  return {
    data,
    status: 200,
    statusText: "OK",
    headers: {},
    config: { headers: new AxiosHeaders() },
  } as AxiosResponse<T>;
}

describe("discoveryService", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("searchTutors GETs /tutors/search, omitting blank filters", async () => {
    const get = vi
      .spyOn(apiClient, "get")
      .mockResolvedValue(
        mockResponse({ isSuccess: true, isFailure: false, error: null, value: { items: [], totalCount: 0, page: 1, pageSize: 20 } }),
      );

    await searchTutors(
      { subject: "Mathematics", language: "", location: "", availableFrom: "" },
      1,
      20,
    );

    expect(get).toHaveBeenCalledWith("/tutors/search", {
      params: {
        subject: "Mathematics",
        language: undefined,
        location: undefined,
        availableFrom: undefined,
        page: 1,
        pageSize: 20,
      },
    });
  });

  it("passes every filter through when all are provided", async () => {
    const get = vi
      .spyOn(apiClient, "get")
      .mockResolvedValue(
        mockResponse({ isSuccess: true, isFailure: false, error: null, value: { items: [], totalCount: 0, page: 1, pageSize: 20 } }),
      );

    await searchTutors(
      {
        subject: "Mathematics",
        language: "English",
        location: "Remote",
        availableFrom: "2026-08-01T00:00:00Z",
      },
      2,
      10,
    );

    expect(get).toHaveBeenCalledWith("/tutors/search", {
      params: {
        subject: "Mathematics",
        language: "English",
        location: "Remote",
        availableFrom: "2026-08-01T00:00:00Z",
        page: 2,
        pageSize: 10,
      },
    });
  });
});

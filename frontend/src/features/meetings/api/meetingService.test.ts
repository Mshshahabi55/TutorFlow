import { afterEach, describe, expect, it, vi } from "vitest";
import { AxiosHeaders, type AxiosResponse } from "axios";
import { apiClient } from "@/services/api/apiClient";
import {
  fetchActiveMeetingForConversation,
  fetchMeetingBySession,
  startMeeting,
} from "@/features/meetings/api/meetingService";

function mockResponse<T>(data: T): AxiosResponse<T> {
  return {
    data,
    status: 200,
    statusText: "OK",
    headers: {},
    config: { headers: new AxiosHeaders() },
  } as AxiosResponse<T>;
}

function mockApiResult<T>(value: T) {
  return mockResponse({ isSuccess: true, isFailure: false, error: null, value });
}

describe("meetingService", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("startMeeting POSTs /sessions/{id}/meeting with no body", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue(mockApiResult({ meetingId: "m1" }));

    await startMeeting("s1");

    expect(post).toHaveBeenCalledWith("/sessions/s1/meeting");
  });

  it("fetchMeetingBySession GETs /sessions/{id}/meeting", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue(mockApiResult({ meetingId: "m1" }));

    await fetchMeetingBySession("s1");

    expect(get).toHaveBeenCalledWith("/sessions/s1/meeting");
  });

  it("fetchActiveMeetingForConversation GETs /conversations/{id}/active-meeting", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue(mockApiResult(null));

    const result = await fetchActiveMeetingForConversation("c1");

    expect(get).toHaveBeenCalledWith("/conversations/c1/active-meeting");
    expect(result).toBeNull();
  });
});

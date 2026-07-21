import { afterEach, describe, expect, it, vi } from "vitest";
import { AxiosHeaders, type AxiosResponse } from "axios";
import { apiClient } from "@/services/api/apiClient";
import {
  bookSession,
  cancelSession,
  completeSession,
  declareAvailability,
  fetchAvailabilitySlotById,
  fetchSessionById,
  fetchStudentSchedule,
  fetchTutorSchedule,
  markSessionNoShow,
  rescheduleSession,
} from "@/features/scheduling/api/schedulingService";
import { DeliveryMode } from "@/services/api/dtos";

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

function mockVoidResult() {
  return mockResponse({ isSuccess: true, isFailure: false, error: null });
}

describe("schedulingService", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("declareAvailability POSTs /availability-slots with the exact input", async () => {
    const post = vi
      .spyOn(apiClient, "post")
      .mockResolvedValue(mockApiResult({ availabilitySlotId: "a1" }));

    const input = {
      tutorId: "t1",
      startTimeUtc: "2026-08-01T14:00:00Z",
      duration: "01:00:00",
      deliveryMode: DeliveryMode.Online,
    };
    await declareAvailability(input);

    expect(post).toHaveBeenCalledWith("/availability-slots", input);
  });

  it("fetchAvailabilitySlotById GETs /availability-slots/{id}", async () => {
    const get = vi
      .spyOn(apiClient, "get")
      .mockResolvedValue(mockApiResult({ availabilitySlotId: "a1" }));

    await fetchAvailabilitySlotById("a1");

    expect(get).toHaveBeenCalledWith("/availability-slots/a1");
  });

  it("bookSession POSTs /sessions with the exact input", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue(mockApiResult({ sessionId: "s1" }));

    const input = { availabilitySlotId: "a1", studentId: "st1", parentGuardianId: null };
    await bookSession(input);

    expect(post).toHaveBeenCalledWith("/sessions", input);
  });

  it("rescheduleSession POSTs /sessions/{id}/reschedule with newScheduledTimeUtc", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue(mockVoidResult());

    await rescheduleSession("s1", "2026-08-02T14:00:00Z");

    expect(post).toHaveBeenCalledWith("/sessions/s1/reschedule", {
      newScheduledTimeUtc: "2026-08-02T14:00:00Z",
    });
  });

  it("cancelSession POSTs /sessions/{id}/cancel", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue(mockVoidResult());

    await cancelSession("s1");

    expect(post).toHaveBeenCalledWith("/sessions/s1/cancel");
  });

  it("completeSession POSTs /sessions/{id}/complete", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue(mockVoidResult());

    await completeSession("s1");

    expect(post).toHaveBeenCalledWith("/sessions/s1/complete");
  });

  it("markSessionNoShow POSTs /sessions/{id}/no-show", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue(mockVoidResult());

    await markSessionNoShow("s1");

    expect(post).toHaveBeenCalledWith("/sessions/s1/no-show");
  });

  it("fetchSessionById GETs /sessions/{id}", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue(mockApiResult({ sessionId: "s1" }));

    await fetchSessionById("s1");

    expect(get).toHaveBeenCalledWith("/sessions/s1");
  });

  it("fetchStudentSchedule GETs /students/{id}/schedule", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue(mockApiResult([]));

    await fetchStudentSchedule("st1");

    expect(get).toHaveBeenCalledWith("/students/st1/schedule");
  });

  it("fetchTutorSchedule GETs /tutors/{id}/schedule", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue(mockApiResult([]));

    await fetchTutorSchedule("t1");

    expect(get).toHaveBeenCalledWith("/tutors/t1/schedule");
  });
});

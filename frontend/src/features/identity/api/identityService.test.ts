import { afterEach, describe, expect, it, vi } from "vitest";
import { AxiosHeaders, type AxiosResponse } from "axios";
import { apiClient } from "@/services/api/apiClient";
import {
  approveTutor,
  confirmRelationship,
  fetchParentGuardianById,
  fetchPendingTutors,
  fetchRelationshipById,
  fetchRelationshipsForAccount,
  fetchStudentById,
  fetchTutorById,
  fetchTutorDirectory,
  inviteRelationship,
  registerParentGuardian,
  registerStudent,
  registerTutor,
  setTutorHourlyRate,
  setTutorLanguage,
  setTutorLocation,
  setTutorOfferedDurations,
  setTutorSubject,
  suspendTutor,
} from "@/features/identity/api/identityService";

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

describe("identityService", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("registerTutor POSTs /tutors with { email, password }", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue(mockApiResult({ tutorId: "t1" }));

    const result = await registerTutor("tutor@example.com", "Password123!");

    expect(post).toHaveBeenCalledWith("/tutors", {
      email: "tutor@example.com",
      password: "Password123!",
    });
    expect(result).toEqual({ tutorId: "t1" });
  });

  it("registerStudent POSTs /students with { email, password, isMinor }", async () => {
    const post = vi
      .spyOn(apiClient, "post")
      .mockResolvedValue(mockApiResult({ studentId: "s1", isMinor: true }));

    const result = await registerStudent("student@example.com", "Password123!", true);

    expect(post).toHaveBeenCalledWith("/students", {
      email: "student@example.com",
      password: "Password123!",
      isMinor: true,
    });
    expect(result.isMinor).toBe(true);
  });

  it("registerParentGuardian POSTs /parent-guardians with { email, password }", async () => {
    const post = vi
      .spyOn(apiClient, "post")
      .mockResolvedValue(mockApiResult({ parentGuardianId: "p1" }));

    await registerParentGuardian("parent@example.com", "Password123!");

    expect(post).toHaveBeenCalledWith("/parent-guardians", {
      email: "parent@example.com",
      password: "Password123!",
    });
  });

  it("approveTutor POSTs /tutors/{id}/approve", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue(mockVoidResult());

    await approveTutor("t1");

    expect(post).toHaveBeenCalledWith("/tutors/t1/approve");
  });

  it("suspendTutor POSTs /tutors/{id}/suspend", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue(mockVoidResult());

    await suspendTutor("t1");

    expect(post).toHaveBeenCalledWith("/tutors/t1/suspend");
  });

  it("setTutorHourlyRate PATCHes /tutors/{id}/hourly-rate with { amount }", async () => {
    const patch = vi.spyOn(apiClient, "patch").mockResolvedValue(mockVoidResult());

    await setTutorHourlyRate("t1", 45);

    expect(patch).toHaveBeenCalledWith("/tutors/t1/hourly-rate", { amount: 45 });
  });

  it("setTutorSubject PATCHes /tutors/{id}/subject with { subject }", async () => {
    const patch = vi.spyOn(apiClient, "patch").mockResolvedValue(mockVoidResult());

    await setTutorSubject("t1", "Mathematics");

    expect(patch).toHaveBeenCalledWith("/tutors/t1/subject", { subject: "Mathematics" });
  });

  it("setTutorLanguage PATCHes /tutors/{id}/language with { language }", async () => {
    const patch = vi.spyOn(apiClient, "patch").mockResolvedValue(mockVoidResult());

    await setTutorLanguage("t1", "English");

    expect(patch).toHaveBeenCalledWith("/tutors/t1/language", { language: "English" });
  });

  it("setTutorLocation PATCHes /tutors/{id}/location with { location }", async () => {
    const patch = vi.spyOn(apiClient, "patch").mockResolvedValue(mockVoidResult());

    await setTutorLocation("t1", "Remote");

    expect(patch).toHaveBeenCalledWith("/tutors/t1/location", { location: "Remote" });
  });

  it("setTutorOfferedDurations PATCHes /tutors/{id}/offered-durations with { durations }", async () => {
    const patch = vi.spyOn(apiClient, "patch").mockResolvedValue(mockVoidResult());

    await setTutorOfferedDurations("t1", ["00:30:00", "01:00:00"]);

    expect(patch).toHaveBeenCalledWith("/tutors/t1/offered-durations", {
      durations: ["00:30:00", "01:00:00"],
    });
  });

  it("fetchPendingTutors GETs /tutors/pending with page/pageSize params", async () => {
    const get = vi
      .spyOn(apiClient, "get")
      .mockResolvedValue(mockApiResult({ items: [], totalCount: 0, page: 2, pageSize: 10 }));

    await fetchPendingTutors(2, 10);

    expect(get).toHaveBeenCalledWith("/tutors/pending", { params: { page: 2, pageSize: 10 } });
  });

  it("fetchTutorById GETs /tutors/{id}", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue(mockApiResult({ tutorId: "t1" }));

    await fetchTutorById("t1");

    expect(get).toHaveBeenCalledWith("/tutors/t1");
  });

  it("fetchTutorDirectory GETs /tutors", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue(mockApiResult([]));

    await fetchTutorDirectory();

    expect(get).toHaveBeenCalledWith("/tutors");
  });

  it("fetchStudentById GETs /students/{id}", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue(mockApiResult({ studentId: "s1" }));

    await fetchStudentById("s1");

    expect(get).toHaveBeenCalledWith("/students/s1");
  });

  it("fetchParentGuardianById GETs /parent-guardians/{id}", async () => {
    const get = vi
      .spyOn(apiClient, "get")
      .mockResolvedValue(mockApiResult({ parentGuardianId: "p1" }));

    await fetchParentGuardianById("p1");

    expect(get).toHaveBeenCalledWith("/parent-guardians/p1");
  });

  it("inviteRelationship POSTs /relationships with { parentGuardianId, studentId }", async () => {
    const post = vi
      .spyOn(apiClient, "post")
      .mockResolvedValue(mockApiResult({ relationshipId: "r1" }));

    await inviteRelationship("p1", "s1");

    expect(post).toHaveBeenCalledWith("/relationships", { parentGuardianId: "p1", studentId: "s1" });
  });

  it("confirmRelationship POSTs /relationships/{id}/confirm", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue(mockVoidResult());

    await confirmRelationship("r1");

    expect(post).toHaveBeenCalledWith("/relationships/r1/confirm");
  });

  it("fetchRelationshipById GETs /relationships/{id}", async () => {
    const get = vi
      .spyOn(apiClient, "get")
      .mockResolvedValue(mockApiResult({ relationshipId: "r1" }));

    await fetchRelationshipById("r1");

    expect(get).toHaveBeenCalledWith("/relationships/r1");
  });

  it("fetchRelationshipsForAccount GETs /accounts/{id}/relationships", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue(mockApiResult([]));

    await fetchRelationshipsForAccount("a1");

    expect(get).toHaveBeenCalledWith("/accounts/a1/relationships");
  });
});

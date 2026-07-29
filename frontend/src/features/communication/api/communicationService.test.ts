import { afterEach, describe, expect, it, vi } from "vitest";
import { AxiosHeaders, type AxiosResponse } from "axios";
import { apiClient } from "@/services/api/apiClient";
import {
  fetchConversationMessages,
  fetchMyConversations,
  fetchMyNotifications,
  markAllNotificationsRead,
  markConversationRead,
  markNotificationRead,
  sendMessage,
  startConversation,
} from "@/features/communication/api/communicationService";

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

describe("communicationService", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("startConversation POSTs /conversations with targetAccountId", async () => {
    const post = vi
      .spyOn(apiClient, "post")
      .mockResolvedValue(mockApiResult({ conversationId: "c1" }));

    await startConversation("t1");

    expect(post).toHaveBeenCalledWith("/conversations", { targetAccountId: "t1" });
  });

  it("fetchMyConversations GETs /conversations/mine", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue(mockApiResult([]));

    await fetchMyConversations();

    expect(get).toHaveBeenCalledWith("/conversations/mine");
  });

  it("fetchConversationMessages GETs /conversations/{id}/messages", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue(mockApiResult([]));

    await fetchConversationMessages("c1");

    expect(get).toHaveBeenCalledWith("/conversations/c1/messages");
  });

  it("sendMessage POSTs /conversations/{id}/messages with the body", async () => {
    const post = vi
      .spyOn(apiClient, "post")
      .mockResolvedValue(mockApiResult({ messageId: "m1" }));

    await sendMessage("c1", "Hello!");

    expect(post).toHaveBeenCalledWith("/conversations/c1/messages", { body: "Hello!" });
  });

  it("markConversationRead POSTs /conversations/{id}/read", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue(mockVoidResult());

    await markConversationRead("c1");

    expect(post).toHaveBeenCalledWith("/conversations/c1/read");
  });

  it("fetchMyNotifications GETs /notifications/mine", async () => {
    const get = vi.spyOn(apiClient, "get").mockResolvedValue(mockApiResult([]));

    await fetchMyNotifications();

    expect(get).toHaveBeenCalledWith("/notifications/mine");
  });

  it("markNotificationRead POSTs /notifications/{id}/read", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue(mockVoidResult());

    await markNotificationRead("n1");

    expect(post).toHaveBeenCalledWith("/notifications/n1/read");
  });

  it("markAllNotificationsRead POSTs /notifications/mark-all-read", async () => {
    const post = vi.spyOn(apiClient, "post").mockResolvedValue(mockVoidResult());

    await markAllNotificationsRead();

    expect(post).toHaveBeenCalledWith("/notifications/mark-all-read");
  });
});

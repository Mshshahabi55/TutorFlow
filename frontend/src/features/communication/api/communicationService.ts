import { apiClient, unwrapValue } from "@/services/api/apiClient";
import type { ApiResult, VoidApiResult } from "@/services/api/apiTypes";
import type { ConversationDto, MessageDto, NotificationDto } from "@/services/api/dtos";

// Every function here is a direct, 1:1 mapping to one endpoint implemented
// by TutorFlow.Web.Endpoints.CommunicationEndpoints
// (docs/adr/ADR-022-communication-and-notifications-architecture.md). No
// field, endpoint, or DTO is added beyond what the backend already exposes.

/** POST /conversations — starts a new Conversation, or returns the existing one for this pair (idempotent). */
export async function startConversation(targetAccountId: string): Promise<ConversationDto> {
  const response = await apiClient.post<ApiResult<ConversationDto>>("/conversations", {
    targetAccountId,
  });
  return unwrapValue(response.data);
}

/** GET /conversations/mine — every Conversation the caller is a party to, most recently active first. */
export async function fetchMyConversations(): Promise<ConversationDto[]> {
  const response = await apiClient.get<ApiResult<ConversationDto[]>>("/conversations/mine");
  return unwrapValue(response.data);
}

/** GET /conversations/{id}/messages — every Message in a Conversation, oldest first. */
export async function fetchConversationMessages(conversationId: string): Promise<MessageDto[]> {
  const response = await apiClient.get<ApiResult<MessageDto[]>>(
    `/conversations/${conversationId}/messages`,
  );
  return unwrapValue(response.data);
}

/** POST /conversations/{id}/messages — sends a Message into an existing Conversation. */
export async function sendMessage(conversationId: string, body: string): Promise<MessageDto> {
  const response = await apiClient.post<ApiResult<MessageDto>>(
    `/conversations/${conversationId}/messages`,
    { body },
  );
  return unwrapValue(response.data);
}

/** POST /conversations/{id}/read — marks every unread Message addressed to the caller in a Conversation as read. */
export async function markConversationRead(conversationId: string): Promise<void> {
  await apiClient.post<VoidApiResult>(`/conversations/${conversationId}/read`);
}

/** GET /notifications/mine — every Notification addressed to the caller, most recent first. */
export async function fetchMyNotifications(): Promise<NotificationDto[]> {
  const response = await apiClient.get<ApiResult<NotificationDto[]>>("/notifications/mine");
  return unwrapValue(response.data);
}

/** POST /notifications/{id}/read — marks a single Notification as read. */
export async function markNotificationRead(notificationId: string): Promise<void> {
  await apiClient.post<VoidApiResult>(`/notifications/${notificationId}/read`);
}

/** POST /notifications/mark-all-read — marks every Notification addressed to the caller as read. */
export async function markAllNotificationsRead(): Promise<void> {
  await apiClient.post<VoidApiResult>("/notifications/mark-all-read");
}

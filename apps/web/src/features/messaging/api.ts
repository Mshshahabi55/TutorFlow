import { apiGet, apiPost } from '@/lib/api/client';
import {
  ConversationSummary,
  CreateOrGetConversationResponse,
  GetMessagesResponse,
  SendMessageCommand,
  SendMessageResponse,
} from './types';

export const MAX_CONVERSATIONS = 50;

export function listConversations(signal?: AbortSignal): Promise<ConversationSummary[]> {
  return apiGet<ConversationSummary[]>('/api/messaging/conversations', undefined, { signal });
}

export interface GetMessagesParams {
  before?: number;
}

export function getMessages(
  conversationId: string,
  params?: GetMessagesParams,
  signal?: AbortSignal,
): Promise<GetMessagesResponse> {
  return apiGet<GetMessagesResponse>(
    `/api/messaging/conversations/${conversationId}/messages`,
    { before: params?.before !== undefined ? String(params.before) : undefined },
    { signal },
  );
}

export function createOrGetConversation(tutorUserId: string): Promise<CreateOrGetConversationResponse> {
  return apiPost<CreateOrGetConversationResponse, { tutorUserId: string }>('/api/messaging/conversations', {
    tutorUserId,
  });
}

export function sendMessage(conversationId: string, command: SendMessageCommand): Promise<SendMessageResponse> {
  return apiPost<SendMessageResponse, SendMessageCommand>(
    `/api/messaging/conversations/${conversationId}/messages`,
    command,
  );
}

export function markConversationRead(conversationId: string): Promise<void> {
  return apiPost<void>(`/api/messaging/conversations/${conversationId}/read`);
}

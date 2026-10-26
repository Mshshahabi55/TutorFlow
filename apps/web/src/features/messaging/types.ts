/** Mirrors `TutorFlow.Modules.Messaging.TutorDisplaySummary` — the only three safe, already-public
 * fields about a Tutor this feature ever renders. Never add email/status/rejection fields here;
 * the backend contract deliberately excludes them (ADR-0030 Batch 69). */
export interface TutorDisplaySummary {
  displayName: string;
  profilePhotoReference: string | null;
  publicSlug: string | null;
}

/** Mirrors `ListConversations.ConversationSummary`. `tutor` is null only when the Tutor's profile
 * summary is genuinely unresolvable — callers must render a safe fallback, never an error. There
 * is deliberately no equivalent student-identity field (ADR-0030): the Student side of a
 * conversation is always rendered as the generic "Student" label, never a name. */
export interface ConversationSummary {
  id: string;
  studentUserId: string;
  tutorUserId: string;
  createdAtUtc: string;
  lastMessageAtUtc: string | null;
  myLastReadAtUtc: string | null;
  tutor: TutorDisplaySummary | null;
}

/** Mirrors `GetMessages.MessageDto`. `sequenceNumber` is the keyset-pagination cursor (ADR-0030) —
 * never treat it as a display value. */
export interface MessageDto {
  id: string;
  senderUserId: string;
  body: string;
  createdAtUtc: string;
  sequenceNumber: number;
}

/** Mirrors `GetMessages.Response`. `tutor` is resolved once per response, at conversation level —
 * never duplicated per message. `messages` arrives newest-first; `nextBeforeCursor` is the
 * `before` value to request the next older page, or null when there is no older page. */
export interface GetMessagesResponse {
  tutor: TutorDisplaySummary | null;
  messages: MessageDto[];
  nextBeforeCursor: number | null;
}

/** Mirrors `CreateOrGetConversation.Response` — unchanged this batch. */
export interface CreateOrGetConversationResponse {
  id: string;
  studentUserId: string;
  tutorUserId: string;
  createdAtUtc: string;
}

/** Mirrors `SendMessage.Command` — the request body field is `text`, not `body` (the backend's
 * own response renames it to `body` on the way out). */
export interface SendMessageCommand {
  text: string;
}

/** Mirrors `SendMessage.Response` — unchanged this batch, same shape as `MessageDto`. */
export type SendMessageResponse = MessageDto;

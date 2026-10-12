# Messaging Module — V1 Product Boundary

**Status:** Specified — first real specification for `Messaging`, not yet implemented. One item (Admin visibility) carries a recommended default pending explicit Product Owner sign-off — see §7.

## 1. Conversation model

A `Conversation` is keyed by exactly one `(StudentUserId, TutorUserId)` pair — unique constraint, created lazily on the first message between them. **No group messaging in V1** — a `Conversation` always has exactly two participants; this is not a design placeholder, it's the actual V1 shape (do not over-build for a hypothetical group case, per instruction).

A `Conversation` persists across the pair's entire relationship — it is not scoped to a single `Booking`. A message can optionally carry a `BookingId` (a plain value, not a cross-module FK — `Messaging` never validates it against `Bookings`' data) purely so the UI can render "regarding your lesson on Oct 5" context; omitting it is normal (e.g. a pre-trial question has no booking yet).

**No prior booking is required to start a conversation** — any authenticated `Student` can message any `Published` `Tutor` directly from the tutor's profile, consistent with the confirmed Preply pattern (`PREPLY_UI_UX_AUDIT.md` §19) of allowing pre-trial questions. `Guest` cannot message (`ROLES_AND_PERMISSIONS.md`) — attempting to triggers the existing auth gate, not a `Messaging`-specific one.

```csharp
internal sealed class Conversation
{
    public Guid Id { get; init; }
    public Guid StudentUserId { get; init; }
    public Guid TutorUserId { get; init; }
    public DateTimeOffset? StudentLastReadAtUtc { get; set; }
    public DateTimeOffset? TutorLastReadAtUtc { get; set; }
    public Guid? BlockedByUserId { get; set; } // null = not blocked
}

internal sealed class Message
{
    public Guid Id { get; init; }
    public Guid ConversationId { get; init; }
    public Guid SenderUserId { get; init; }
    public string Body { get; init; } = "";
    public string? AttachmentUrl { get; init; }
    public Guid? RelatedBookingId { get; init; } // display-only, not validated cross-module
    public DateTimeOffset SentAtUtc { get; init; }
}
```

## 2. Message types & attachments

Text + at most one attachment per message (image or PDF) for V1 — no voice/video messages, no multi-attachment messages. Attachment size limit (e.g. 10 MB) is an **engineering default, not a business policy** — adjust freely without a product decision; it exists only to bound storage/upload cost.

## 3. Read/unread state

Per-participant `LastReadAtUtc` timestamp on the `Conversation` (not per-message) — a message is unread for a participant if `SentAtUtc > {Student,Tutor}LastReadAtUtc`. Opening a conversation sets the reader's own timestamp to `now()`; this is the entire unread-count mechanism, no separate read-receipt table.

## 4. Realtime

SignalR, matching `DOMAIN_MODEL.md`'s existing assumption. **Durability-first ordering**: every message is persisted via the normal HTTP `POST /api/messaging/conversations/{id}/messages` call first; the SignalR broadcast to connected participants happens only after the write succeeds — never broadcast-then-persist. A scale-out backplane (Redis/Azure SignalR) is explicitly deferred to when horizontal scale-out actually happens, unchanged from `DOMAIN_MODEL.md`'s existing note — a single-instance SignalR hub is sufficient for V1.

## 5. Notifications

Sending a message when the recipient is not actively connected to that conversation's SignalR group publishes `MessageSent` (`TutorFlow.Shared`) for `Notifications` to pick up (email/push) — the same integration-event pattern already used everywhere else in this system, not a new mechanism.

## 6. Blocking & reporting

- **Block**: either participant can block the other (`Conversation.BlockedByUserId`). A blocked conversation keeps its full history visible to both, but the blocked party can no longer send new messages — enforced server-side on the send endpoint, `409` if attempted. Blocking is one-directional and reversible (unblock clears the flag); it is not a platform-wide ban (that's `Identity`/`Tutors` account-suspension territory, `OPEN_QUESTIONS.md` #25, unrelated).
- **Report**: a reported conversation sets a flag Admin can filter on — this is the only mechanism by which Admin sees conversation content by default (see §7). Report routing into a broader "Support" workflow is `OPEN_QUESTIONS.md` A12, still open — not invented further here.

## 7. Admin visibility — recommended default, needs Product Owner sign-off

`OPEN_QUESTIONS.md` #26 asked whether Admin can read any conversation or only escalated ones. **Recommended default** (privacy-safer, GDPR-conscious for an EU product): Admin can view a conversation's content **only once it has been reported** (§6) — there is no general "browse all conversations" admin capability. This is a recommendation this spec adopts provisionally, not a final decision — it has real legal/compliance weight that belongs with the Product Owner, not an architecture decision made unilaterally here. `OPEN_QUESTIONS.md` #26 stays open until explicitly confirmed, now narrowed to "confirm (or override) the report-triggered-only default."

## 8. States

| State | Behavior |
|---|---|
| Loading | Skeleton conversation list / message thread |
| Empty (no conversations) | "No messages yet" + CTA to find a tutor |
| Empty (conversation, no messages) | Composer visible, no history — normal for a brand-new conversation |
| Error | Standard `ProblemDetails` + retry; a failed send keeps the drafted text in the composer, never silently discards it |
| Blocked | Composer disabled with an explicit reason, history still visible |
| Unauthenticated | `Message` CTA on a tutor profile triggers the existing login/signup gate before opening a composer, not a `Messaging`-specific gate |

## 9. What this spec does not decide

- Final Admin-visibility policy (§7) — needs explicit Product Owner sign-off.
- "Support"-as-ticketing vs. Messaging-with-Admin-participant (`OPEN_QUESTIONS.md` A12).
- SignalR scale-out backplane — deferred until horizontal scale-out is real.

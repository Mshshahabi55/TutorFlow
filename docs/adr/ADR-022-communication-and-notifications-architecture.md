**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001` through `ADR-021` (all Accepted, except `ADR-021`, which remains itself Proposed). This ADR records the business-owner's explicit decision (2026-07-28) to bring text-based in-platform messaging into v1 scope, and designs the resulting Communication bounded context. It is an implementation authorization, not a design-only proposal — see Status.

---

# ADR-022: Communication & Notifications Architecture

## Status

**Accepted — 2026-07-28.** Business decision, made explicitly by the project owner through the Constitution's Decision-Making Process (RC5.1), after being shown that messaging was explicitly out of scope and choosing to widen scope rather than defer or design-only. `PROJECT_CONSTITUTION.md` Project Scope and `PRODUCT_REQUIREMENTS.md` Section 9 are amended accordingly (see those documents' own diffs, dated 2026-07-28). Unlike `ADR-021`, this ADR authorizes implementation.

## Context

Every role already in scope (Student, Tutor, Parent/Guardian, Admin/Staff) currently has no way to communicate inside the platform — `MessagesPage` has stood as an honest "coming soon" placeholder since RC2 specifically because no messaging capability existed. RC5.1 asks for a real Conversation/Message model, an Inbox, a Conversation screen, and a Notification system surfacing booking/cancellation/reply/relationship/availability events. This ADR designs the minimum real system that satisfies that request without introducing new infrastructure (no message broker, no WebSocket/SignalR hub) and without touching the three capabilities RC5.1 itself excludes: payment gateways, Google Meet/video, and the Learning Plans backend (`ADR-021`, still Proposed, untouched here).

## Decision

### New bounded context: Communication

A fifth bounded context, alongside Scheduling & Booking, Identity & Relationship, Discovery, and Marketplace Oversight (`ADR-002`). Owns `Conversation`, `Message`, and `Notification` — three independent aggregate roots, no shared aggregate, mirroring the small-aggregate/identity-reference heuristics `ADR-015` already established for `AvailabilitySlot`/`Session`.

**Upstream/downstream:** Communication reads Identity & Relationship (to know who a caller and a target Account are) and Scheduling & Booking (only through already-existing Domain Events, never direct table access — see "Notification triggers" below). No other context depends on Communication. This is the same shape `ADR-021` proposed for Enrollment & Billing → Scheduling & Booking, extended here for real.

### Conversation aggregate

Fields: `ParticipantAId`, `ParticipantBId` (both `AccountId`, referenced by identity only, per `ADR-002`), `CreatedAtUtc`, `LastMessageAtUtc` (nullable, updated when a Message is sent). Two distinct participants required; no group conversations (not requested, not built). `HasParticipant(accountId)` and `OtherParticipant(accountId)` are the aggregate's own query methods — every authorization/notification decision in Application/Infrastructure derives from these, never re-implemented ad hoc.

**Who may start one, per RC5.1 Step 1:** Student↔Tutor, Parent/Guardian↔Tutor, Admin/Staff↔any Account. Enforced in the Application handler (`StartConversationCommandHandler`): the caller must be a Student/Parent/Guardian targeting a Tutor, a Tutor targeting an existing conversation only (a Tutor never unilaterally starts one — no "browse students" capability exists to pick a target from), or an Admin/Staff targeting anyone. Starting a conversation between the same two participants twice returns the existing one (idempotent), enforced by a unique index on the unordered participant pair — the same unique-constraint-based concurrency pattern `ADR-014` already established, reused rather than reinvented.

### Message aggregate

A separate aggregate root (not a child collection of Conversation), for the same reason `ADR-015` kept `Session` separate from `AvailabilitySlot`: an ever-growing collection loaded as part of its parent on every mutation is exactly the aggregate-sizing problem that heuristic exists to avoid. References `ConversationId` by identity. Fields: `SenderId`, `RecipientId` (both `AccountId` — denormalized onto the Message itself, not re-derived from the Conversation on every read, since the sender is always one of exactly two participants and this avoids a join for the common "my messages" read), `Body` (plain text, length-limited, never HTML/markup — no rich content requested or built), `SentAtUtc`, `ReadAtUtc` (nullable — the only "delivery state" this system honestly has; see below).

**Delivery/read state (RC5.1 Step 3):** this is a request/response system (see "No new infrastructure," below) — there is no real transport-level "delivered to device" signal to report, so a fabricated third tick would be dishonest. `Sent` (the row exists, `ReadAtUtc` null) and `Read` (`ReadAtUtc` set, by the recipient viewing the conversation) are the only two real states surfaced. **Typing indicator:** RC5.1 itself says "Typing placeholder," not "typing indicator" — read literally and implemented literally: a static, non-functional UI element, never wired to any real signal, since a genuine one requires exactly the real-time transport this ADR declines to introduce.

### Notification aggregate

Fields: `RecipientId` (`AccountId`), `Type` (enum: `BookingConfirmed`, `LessonCancelled`, `TutorReplied`, `NewMessage`, `ParentConfirmed`; **`AvailabilityChanged` is named in the enum but has no trigger wired — see Non-Goals**), `Summary` (a short, pre-composed, honest sentence — never the message body itself, see "Audit & privacy," below), `RelatedEntityId` (nullable `Guid` — a `SessionId` or `ConversationId` for deep-linking, whichever the notification concerns), `CreatedAtUtc`, `ReadAtUtc` (nullable).

**Notification triggers — built only where an existing Domain Event already carries (or can honestly be enriched to carry) the recipient:**

| Trigger | Event | Recipient(s) | Enrichment needed |
|---|---|---|---|
| Booking confirmed | `SessionBooked` | Student; Parent/Guardian if one booked | `SessionBooked` gains an optional trailing `ParentGuardianId?` field (default `null`) — `Session.Book(...)` already receives this value, it just wasn't on the event. Additive only; every existing positional construction (`AuditDomainEventHandlerTests`) still compiles unchanged. |
| Lesson cancelled | `SessionCancelled` | Student; Parent/Guardian if one exists | `SessionCancelled` gains optional trailing `TutorId?`, `StudentId?`, `ParentGuardianId?` fields — `Session.Cancel()` already holds all three on `this`, it just wasn't putting them on the event. Same additive-only guarantee. |
| Tutor replied / New message | `MessageSent` | The conversation's other participant | None — `MessageSent` already carries `RecipientId` (see Message aggregate, above). `TutorReplied` vs `NewMessage` is decided by the sender's role, read from `ICurrentUserProvider.Role` at the moment the message is sent (the same request scope as the sender). |
| Parent confirmed | `RelationshipConfirmed` | The party who invited (`Relationship.InvitedByAccountId`) | None — already on the aggregate; not yet on the event. `RelationshipConfirmed` gains an optional trailing `InvitedByAccountId?` field, sourced the same additive way. |

**Deliberately not built: `AvailabilityChanged`.** `AvailabilityDeclared` carries only `AvailabilitySlotId`/`TutorId` — no natural single recipient exists (no "follow this Tutor" or subscription concept anywhere in this domain), and deriving "students who might care" would mean querying Scheduling & Booking's own Session data directly from a Communication-context listener — exactly the direct cross-context table access `ADR-002`'s Integration Rules forbid. Building a real recipient-resolution mechanism for this one trigger is a genuinely separate, larger decision (a subscription/follow model) not authorized by this ADR. The enum value is named for forward-compatibility (mirroring `ADR-019`'s "named but not yet meaningfully populated" pattern for Currency) but nothing raises it today.

**Reactive creation, same mechanism as the audit trail:** a new `NotificationDomainEventHandler` (`Infrastructure/Communication/`), registered as a second `IDomainEventHandler` alongside the existing `AuditDomainEventHandler` (`DomainEventDispatcher` already fans out to every registered handler — no change to that mechanism). Stages new `Notification` rows into the same `DbContext` the triggering mutation is about to commit — same same-transaction durability `ADR-016` already established for audit entries, reused rather than reinvented. Reads only fields already present on the event; never queries another context's tables.

### No new infrastructure

Per this project's standing "no new infrastructure without separately revisiting that constraint" posture (`ADR-016`'s treatment of the Outbox pattern is the precedent this ADR follows): no WebSocket/SignalR hub, no message broker, no push-notification service. The Inbox, Conversation screen, and Notification bell are all ordinary `GET` endpoints; the frontend achieves a "live-ish" feel via TanStack Query polling (a `refetchInterval`, the same tool every other "did anything change" concern in this app already uses), not a persistent connection. This is a real, deliberate trade-off, not an oversight: true real-time delivery (a message appearing on the recipient's screen without a refresh or poll tick) is out of scope for this ADR and would need its own, separate infrastructure decision.

### Authorization

One new coarse-grained permission, `UseMessaging`, granted to all four roles (Student, Tutor, ParentGuardian, AdminStaff) — every role may participate in messaging once authenticated, mirroring `CancelSession`'s "every role shares this one" precedent in `RolePermissionCatalog`. All finer-grained rules (only a conversation's own two participants may read/send within it; only the recipient may mark their own notification read) are enforced inside the handlers via `OwnershipExtensions`-style checks (`Conversation.HasParticipant(...)`), the same two-layer coarse+fine model `ADR-003`/`ConfirmRelationshipCommandHandler` already establish — no new authorization mechanism invented.

### Audit & privacy

`ConversationStarted` and `MessageSent` are added to `AuditDomainEventHandler`'s existing switch (metadata only: who/when/which conversation) — both are governance-relevant state changes per `CLAUDE.md`'s own non-negotiable rule. **The message body is never written to `AuditEntry`** — `AuditEntry`'s shape is fixed to `(Id, OccurredOnUtc, Action, SubjectId, ActorId, ActorRole)` with no free-text payload field, and this ADR does not add one, consistent with CONST-4's GDPR-grade protection standard already applied to every other audit record (recording that a message was sent is accountability; duplicating its content into a second store is a data-protection liability this ADR does not take on). `NotificationCreated`/`NotificationRead` are not audited — they are themselves derivative of an already-audited or intentionally-not-audited triggering event, the same reasoning `ADR-016` already applied to not auditing every read-side projection.

## API Shape (design-level; exact routes/DTOs finalized in implementation)

`POST /conversations` (start-or-return-existing), `GET /conversations/mine`, `GET /conversations/{id}/messages`, `POST /conversations/{id}/messages`, `POST /conversations/{id}/read`, `GET /notifications/mine`, `POST /notifications/{id}/read`, `POST /notifications/mark-all-read`. Every one gets a non-Open row in `docs/api/AUTHORIZATION_MATRIX.md` before being considered done, per `CLAUDE.md`'s standing Definition of Done — not a new rule.

## Consequences

**Becomes possible:** real messaging and a real notification feed, both genuinely working, neither fabricated.

**Becomes harder / newly constrained:** `SessionBooked`, `SessionCancelled`, and `RelationshipConfirmed` each gain an additive, optional, trailing field — every future consumer of these events must be aware more data now rides along, though nothing existing breaks. `AuditDomainEventHandler`'s switch grows by two cases.

**Now forbidden (restated, not new):** no video/call capability, no message-body storage in the audit trail, no real-time transport, no Learning Plans backend — all outside this ADR's authorization.

## Non-Goals

Does not implement `AvailabilityChanged` notifications (no recipient-resolution mechanism exists; a genuinely separate, future decision). Does not implement group conversations. Does not implement message editing/deletion, attachments, or read receipts beyond a single `ReadAtUtc` timestamp. Does not implement real-time delivery (WebSockets/push) — polling only. Does not implement Google Meet, payments, or the Learning Plans backend (`ADR-021` remains untouched and still Proposed).

## Supersedes / Relates To

- **Amends** `PROJECT_CONSTITUTION.md` Project Scope and `PRODUCT_REQUIREMENTS.md` Section 9 (2026-07-28) — messaging is brought into scope; video and materials delivery remain excluded.
- **Extends** `ADR-002` with a fifth bounded context (Communication), the same category of extension `ADR-021` proposed (not yet accepted) for Enrollment & Billing.
- **Reuses, does not revise,** `ADR-014`'s unique-constraint concurrency pattern, `ADR-015`'s small-aggregate/identity-reference heuristics, `ADR-016`'s same-transaction durability mechanism, and `ADR-003`'s coarse+fine authorization model.
- **Does not touch** `ADR-021` (Learning Plans/Enrollment, still Proposed) or any Scheduling & Booking business rule — `SessionBooked`/`SessionCancelled`'s enrichment is additive-only, verified by the existing positional-construction test continuing to compile and pass unchanged.

---

*Status: Accepted — 2026-07-28. Mechanism: three new aggregates (Conversation, Message, Notification) in a new Communication bounded context; reactive Notification creation via a second `IDomainEventHandler`; no new infrastructure (REST + client polling); message-body content never enters the audit trail.*

# ADR-0030: Messaging V1 — Student-Initiated, No-Booking Conversations

**Status:** Accepted

## Context

`Messaging` has been a pure stub since this project's start — no persistence, no endpoints, a frontend "Coming Soon" placeholder. Two other roadmap items are explicitly blocked on it: Reviews' "Post review" entry point from a conversation (`12-classroom-messaging-reviews-notifications.md`), and the tutor-message notification email (`18-screenshot-evidence.md` Part C, SS49–50, OBSERVED but unbuilt — ADR-0029 explicitly deferred it pending this module).

Preply evidence for standalone messaging (as distinct from in-Classroom chat, which ADR-0006 already scopes out of this module entirely) is limited. **DOCUMENTED** (`help.preply.com/en/articles/4179497-how-to-send-a-message-to-your-tutor`, re-fetched directly for this ADR): *"please avoid sharing your contact details or any external links with your tutor **before booking a trial lesson**"* — this warning presupposes messaging occurs before any booking exists; a student is not required to have booked anything first. The article does not describe the exact UI mechanism (e.g. whether every tutor profile has a visible "Message" action) — that remains **NOT VERIFIED**; this ADR does not invent it. Blocking mechanics, attachment support outside Classroom, and unread-list UI are likewise **NOT VERIFIED** and are not addressed here.

## Decision

### Scope: a narrow first vertical slice, not Preply parity

One-to-one, text-only conversations between a Student and a Tutor. Explicitly out of scope for this slice, to be revisited as separate, later decisions:

- Attachments (standalone-Messenger attachment support has no DOCUMENTED evidence).
- Classroom chat synchronization — ADR-0006 already settled this: in-session chat is ephemeral and lives in the video provider's own session, intentionally never synced to these persistent conversations.
- Reviews' "Post review from a conversation" entry point.
- Blocking/reporting integration (tracked as its own, separate roadmap item).
- A `MessageSent`-style integration event for the tutor-message notification email — the data model does not preclude adding one later, but none is published by this slice.

### Conversation eligibility: Student-initiated, no Booking required, published Tutor only

A Student may create a conversation with any currently-bookable (published) Tutor — no Booking, trial or otherwise, is a prerequisite, consistent with the DOCUMENTED pre-booking-contact evidence above. **Tutor-initiated first contact is out of scope for V1** (no evidence either direction was found; a Tutor may only reply to a conversation where they are already a participant). A conversation, once created, is retrievable by both participants regardless of the Tutor's eligibility at read time — a Tutor later becoming unpublished does not retroactively hide the Student's own conversation history; eligibility gates creating a *new* relationship, not access to an existing one.

**Eligibility signal: `ITutorAvailabilityQuery.IsTutorBookableAsync(tutorUserId, ct)`, a new method on the existing ADR-0014 contract, implemented by `Availability` against its own `TutorBookabilityRecords` projection** — the same signal `Bookings` already trusts for "is this tutor live," reused rather than duplicated. This was a deliberate choice against two alternatives considered and rejected:

- **`ITutorProfileSummaryQuery` (already used by `Notifications`)** — inspected and found unsuitable: its implementation returns a non-null summary for *any* `TutorApplication` row regardless of `Status`, never checking for Draft/Suspended/Rejected. Using it here would have silently let an unpublished "tutor" receive messages.
- **A new, `Tutors`-owned "is published" check** — rejected because it would create a second source of truth for the same underlying fact `TutorBookabilityRecords` already tracks (driven by the same `TutorPublished`/`TutorUnpublished` events), risking drift between what Messaging considers eligible and what Bookings does.

**Fail-closed, inherited from ADR-0014 unchanged:** absence of a `TutorBookabilityRecords` row (a tutor that has never been published) returns `false`, never `true`. A genuine query failure (e.g. a transient Availability database error) is **not** caught and converted into "not eligible" — it propagates as an unhandled failure, the same "never silently misrepresent an infrastructure problem as a business decision" discipline ADR-0029's Notifications work established. `IsTutorBookableAsync` and the pre-existing `ValidateSlotAsync` now share this check internally (the latter calls the former) rather than duplicating the null/flag logic twice.

A Student-vs-nonexistent-Tutor and a Student-vs-unpublished-Tutor request receive the identical generic rejection (`400`, same body) — the eligibility query gives the handler no way to distinguish them anyway, so there is nothing more specific to leak.

**Accepted limitation — publication-projection propagation lag (Batch 64 independent audit).** Eligibility relies entirely on `Availability`'s own `TutorBookabilityRecords` projection, which is updated by consuming the `TutorPublished`/`TutorUnpublished` integration events (ADR-0011/ADR-0024) — an asynchronous hand-off, not a synchronous write within the same request that triggered publish or suspend. A recently suspended or unpublished Tutor can therefore remain eligible to receive a new conversation for a short interval, until the consumer processes the transition and flips `IsBookable` to `false`. This project's own prior investigation has directly observed Wolverine first-dispatch latency on the order of several seconds on a freshly-started host; that is reported here only as an observed data point, not a guaranteed upper bound or SLA on this propagation delay. The fail-closed design above addresses a different case (no record / a failed query) and does not and cannot close this one — a stale `true` value from a projection that has not yet been told about a very recent change looks identical, from this query's point of view, to a genuinely current `true`. **Accepted for V1.** No second eligibility source and no change to runtime behavior is introduced to close this gap; closing it, if ever required, would be a product decision about whether messaging needs stronger real-time consistency than booking creation already settles for.

### Data model

`Conversation`: `Id` (Guid), `StudentUserId`, `TutorUserId`, `CreatedAtUtc`, `LastMessageAtUtc` (nullable until the first message), `StudentLastReadAtUtc`/`TutorLastReadAtUtc` (nullable, two plain timestamps rather than a per-message receipts table — the simplest model consistent with real unread-badge mechanics being NOT VERIFIED anywhere in this project's evidence, an explicit engineering default). DB-unique index on `(StudentUserId, TutorUserId)` — one conversation per relationship, matching Reviews' identical uniqueness shape.

`Message`: `Id` (Guid, the only public-facing identifier), `ConversationId`, `SenderUserId` (set exactly once at creation, directly from the authenticated principal — `SendMessage.Command` carries no sender field at all, so there is nothing for a client to spoof), `Body` (max 5000 chars), `CreatedAtUtc`, and `SequenceNumber` — a PostgreSQL `GENERATED BY DEFAULT AS IDENTITY` column (`.UseIdentityColumn()`), **not** an application-computed `MAX()+1`, which would race under concurrent inserts (Postgres' own internal sequence assigns this atomically). Global across all conversations, not reset per conversation — the only ordering guarantee this module needs, `WHERE ConversationId = x ORDER BY SequenceNumber`, is deterministic regardless of gaps shared with other conversations.

### Concurrency: create-or-get

Reuses this codebase's established idiom exactly (`SubmitReview.cs`): check for an existing row first, attempt an optimistic insert if none exists, catch the real Postgres unique-violation (`DbUpdateException { InnerException: PostgresException { SqlState: UniqueViolation } }`) and re-query on conflict. Both concurrent callers for the same pair converge on the identical conversation row — proven against real Postgres, not simulated (`MessagingConcurrencyTests`).

### API surface, pagination, and authorization

| Route | Caller | Notes |
|---|---|---|
| `POST /api/messaging/conversations` | Student only | `{ tutorUserId }`; create-or-get; rate-limited |
| `GET /api/messaging/conversations` | Student or Tutor | Filtered server-side by the caller's own claim |
| `GET /api/messaging/conversations/{id}/messages?before={seq}&take=N` | Participant only | Keyset pagination on `SequenceNumber`; newest-first; `take` clamped to [1, 100] |
| `POST /api/messaging/conversations/{id}/messages` | Participant only | `{ text }`; rate-limited |
| `POST /api/messaging/conversations/{id}/read` | Participant only | Mutates only the caller's own read-state column |

Cursor pagination is newest-first: the first page (no `before`) is the most recent messages; `NextBeforeCursor` pages backward into older history. This is the one contract for every caller. Ordering is deterministic *within a single query snapshot*, and the strict `<` cursor on an immutable, monotonic `SequenceNumber` avoids the classic offset-pagination bug (rows shifting position as new ones are inserted at the front, causing a later page to skip or repeat rows relative to an earlier one). **This is narrower than a claim that concurrent traversal can never miss a message.** PostgreSQL's identity/sequence allocation order is not the same as transaction commit order — a transaction can allocate a lower `SequenceNumber` and still commit after a transaction that allocated a higher one. If a page has already advanced past a given sequence value by the time that lower-numbered message commits, that message is omitted from *that particular* backward-pagination traversal — it is never lost (the row is persisted and visible via a fresh first-page fetch), only absent from an in-progress historical scroll that happened to straddle the commit. The current implementation does not guarantee snapshot-consistent traversal across separate page requests spanning concurrent commits, and this has not been eliminated or specifically tested for (Batch 64 independent audit) — accepted as a V1 limitation given the narrow window involved (a single `SaveChangesAsync` round-trip) and the absence of a stated product requirement for strict cross-transaction ordering.

**A non-participant request for any conversation-scoped endpoint, and a request for a conversation that does not exist at all, both return a uniform `404`** — a deliberate deviation from `Bookings.GetBooking`'s distinguished `Forbidden`/`NotFound` (which this codebase otherwise uses), chosen specifically for this more private, 1:1 data to reduce conversation-ID enumeration. `SenderUserId`/read-state mutations are never derived from a route or body value the caller could manipulate — always the authenticated principal's own claim, matching `ClaimsPrincipalExtensions.TryGetUserId`'s existing convention.

### Abuse controls

Reuses the existing `Microsoft.AspNetCore.RateLimiting` infrastructure (`Program.cs`'s `auth-register`/`auth-login` fixed-window policies) — no new package. Two new named policies, `messaging-create-conversation` (10/hour) and `messaging-send-message` (30/minute), both partitioned by the caller's authenticated `NameIdentifier` claim rather than IP (unlike the pre-existing auth policies) — the real abuse scenario here is one account spamming many tutors, not an unauthenticated source hammering an endpoint. This requires `UseRateLimiter()` to run *after* `UseAuthentication()`/`UseAuthorization()` so `HttpContext.User` is populated when the partition-key factory runs — **the middleware pipeline was reordered for this reason**, re-verified not to affect the pre-existing IP-partitioned auth policies (`AuthRateLimitingTests`, 100% pass after the reorder).

**Known limitation, inherited, not new:** these policies are process-local (in-memory `FixedWindowRateLimiterOptions` counters). If `TutorFlow.Host` ever runs as more than one instance behind a load balancer, the effective limit becomes `PermitLimit × instance count`, not a true shared cap — identical to the pre-existing auth-register/auth-login limitation, acceptable for the current single-instance deployment topology. A distributed counter (e.g. Redis-backed) would be required before horizontal scaling and is not built here.

### What this does not guarantee

No claim of Preply UI/UX parity — the exact conversation-initiation mechanism (a profile button, a quiz flow, something else) was not independently observed and is not reproduced here; this slice only implements the underlying access rule the help article documents. No claim about attachments, blocking, read-receipt fidelity, or notification delivery — all explicitly deferred.

## Consequences

- `TutorFlow.Shared.ITutorAvailabilityQuery` gains `IsTutorBookableAsync`; `Availability`'s implementation is the only one (`Messaging.csproj` gains no new project reference beyond `TutorFlow.Shared`, per the established cross-module query pattern).
- `Messaging`: real module for the first time — `MessagingDbContext` (new schema `messaging`), `Conversation`/`Message` domain entities, five feature slices, `MessagingModule.cs` registration/mapping.
- `TutorFlow.Host/Program.cs`: two new rate-limit policies; `UseRateLimiter()` moved after `UseAuthentication()`/`UseAuthorization()`.
- No frontend change in this batch — the existing `ComingSoon` placeholder is left as-is; a minimal conversation/message UI is a separate, deferred follow-up (see the implementation report for this batch).
- Discovered and fixed, as a narrow test-infrastructure correction unrelated to this feature's logic: `Availability`'s own test project was missing the established `[Collection]` guard against two `WebApplicationFactory<Program>`-based test classes racing on Serilog's static bootstrap logger — the same class of fix already applied to `Identity.Tests`/`TutorFlow.Host.Tests`, now also applied here.

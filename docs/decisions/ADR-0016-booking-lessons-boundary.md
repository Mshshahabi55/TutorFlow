# ADR-0016: Booking ↔ Lessons Boundary

**Status:** Proposed — requires Architect sign-off before `Bookings` is scaffolded. Does not implement `Lessons`.

## Context

`ADR-0010` (Accepted) settled the cardinality: one confirmed `Booking` produces exactly one `Lesson`, and `Bookings` "publishes an integration event... `Lessons` consumes it to create exactly one `Lesson`." It did not settle the mechanics: is creation synchronous or event-driven, what data crosses the boundary, how is a duplicate `Lesson` prevented, what happens if creation fails, and does `Booking` confirmation wait for it. `Lessons` does not exist yet, so nothing here is implemented.

## Decision

### 1. Lesson creation is event-driven, not synchronous, and does not block Booking confirmation

`Bookings` publishes a `BookingConfirmed` event immediately upon the `PendingPayment` → `Confirmed` transition (or, for subscription-driven occurrences, immediately upon hold confirmation without a payment step — `ADR-0010`). The HTTP/webhook-handling request that triggered confirmation returns as soon as the `Booking` row itself is updated and the event is durably queued for publication (outbox pattern, once Wolverine is introduced — see §4) — it does **not** wait for `Lessons` to actually create the `Lesson` row.

This is required because: `Payment` confirmation is itself webhook-driven and must respond quickly and reliably; `Lessons` creating its own row is `Lessons`' responsibility and failure domain, not something a payment webhook handler should be coupled to; and this is consistent with "Booking and Payment are separate state machines" extending naturally to "Booking and Lesson are separate state machines" (`A6`, already accepted).

### 2. Event payload

```csharp
// TutorFlow.Shared (shape only — not implemented until Wolverine is introduced, see §4)
public sealed record BookingConfirmed(
    Guid BookingId,
    Guid StudentUserId,
    Guid TutorUserId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    int DurationMinutes,
    Guid? SubscriptionReferenceId,
    Guid EventId,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent;
```

This carries everything `Lessons` needs to create a `Scheduled` `Lesson` (`docs/domain/STATE_MACHINES.md`) without querying `Bookings`' schema. `SubscriptionReferenceId` is passed through opaquely (an external reference, no FK) so `Lessons`/`Subscriptions` can later correlate subscription-driven lessons if needed — its presence does not change `Lessons`' creation logic.

### 3. Duplicate-creation prevention is `Lessons`' responsibility, not `Bookings`'

`Lessons` owns a `BookingId` column (`Guid`, plain external reference — no FK into `Bookings`' schema) with a **unique index**. On receiving `BookingConfirmed`, `Lessons`' handler attempts to create a `Lesson` keyed by `BookingId`; a unique-violation on retry (at-least-once delivery) is caught and treated as idempotent success, following the exact pattern already established for unique-violation handling in `Identity`, `Tutors`, and `Availability`. `Bookings` does not need its own deduplication for this — it is free to re-publish `BookingConfirmed` on retry without coordination.

### 4. `Booking` does not store a `LessonId`

Consistent with `ADR-0011`'s stable-identity philosophy, `Booking` never stores or exposes a `LessonId`. If a caller needs "the Lesson for Booking X," they query `Lessons` by `BookingId` directly — `Lessons` owns that correlation from its own side. This keeps `Bookings` from needing to reach back into `Lessons`' data to answer a question `Lessons` is already positioned to answer.

### 5. Lesson-creation failure does not roll back the Booking

If `Lessons` fails to create a `Lesson` after receiving `BookingConfirmed` (bug, validation failure, outage), the `Booking` remains `Confirmed` — `Bookings` does not and cannot know about `Lessons`-side failures, and must not be designed to. Retriable delivery (via whatever messaging infrastructure is introduced) and `Lessons`' own error handling are the recovery path; this is explicitly `Lessons`' concern, not specified further here.

### 6. Cancellation and reschedule also publish events `Lessons` is expected to consume

`BookingCancelled` (carrying `BookingId`, `Reason`, `CancelledAtUtc`) and the reschedule pair described in `ADR-0017` are published for the same reason `BookingConfirmed` is — so `Lessons` can cancel or re-point its own `Lesson` row without `Bookings` knowing anything about `Lessons`' internal state. The exact `Lessons`-side behavior on receiving these (e.g. does a cancelled Lesson need a distinct sub-reason) is not decided here — out of scope per "do not implement Lessons."

## Messaging infrastructure

Wolverine is **not** added by this ADR, for the same reason as `ADR-0015`: `Lessons` (the consumer) does not exist yet, so there is nothing to wire. `BookingConfirmed`/`BookingCancelled` are documented here as the *shape* future implementers need, not implemented. Whichever module is built second (`Bookings` already exists as a stub today; `Lessons` does not) is responsible for introducing Wolverine when both sides genuinely coexist.

## Consequences

- The first `Bookings` implementation slice can define and unit-test the `BookingConfirmed`/`BookingCancelled` record shapes and the transition logic that would publish them, without actually publishing anything (no outbox, no Wolverine) until `Lessons` exists — mirroring how `Availability`'s `TutorBookabilityProjector` exists and is tested today without a live event source.
- `Lessons`, when built, is responsible for its own `BookingId` uniqueness constraint and idempotent-create logic; `Bookings` carries no obligation to deduplicate on `Lessons`' behalf.
- No `Booking.LessonId` column exists; no code should be written assuming one.

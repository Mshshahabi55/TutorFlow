# ADR-0029: Email Infrastructure & Abandoned-Checkout Recovery

**Status:** Accepted

## Context

`18-screenshot-evidence.md` Part C (SS51–SS52) is **OBSERVED** evidence — a real email Preply sent to a real inbox — of an automated abandoned-checkout recovery email, triggered by a real, uncompleted trial-booking checkout (Part B, SS38–SS40). `Notifications` was a pure stub; zero email infrastructure existed anywhere in this codebase.

Preply's evidence establishes the **external behavior**: a real, dated recovery email exists, references the exact abandoned slot ("Abandoned slot", date/time, "Time is based on your location"), and carries a CTA back to the tutor. It does **not** establish Preply's **internal implementation** — exact trigger delay, retry policy, scheduler mechanism, or whether push/SMS/in-app equivalents also fire. Those remain **NOT VERIFIED** and nothing in this ADR claims otherwise.

`Bookings` already has real, tested hold-expiry infrastructure (`Booking.HoldExpiresAtUtc`, `BookingWrites.ExpireStaleHoldsAsync`) — but that sweep is **lazy and per-tutor-scoped**: it only runs when a *different* student requests a new hold with the *same* tutor (ADR-0013 §2). It cannot serve as a reliable, booking-independent notification trigger — a tutor with no other booking traffic would never have their stale holds swept at all.

## Decision

### 1. `IEmailSender` — provider-neutral transactional abstraction, owned by `Notifications`

```csharp
public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct);
}
public sealed record EmailMessage(string ToEmail, string Subject, string HtmlBody, string? PlainTextBody = null);
```

No vendor type appears in this signature. Transactional only — no marketing/campaign features, no templating engine, matching the task's explicit "do not over-engineer a complete marketing-email platform."

### 2. Email provider: **Postmark**

No prior provider decision existed in this codebase (checked: no ADR, no `OPEN_QUESTIONS.md` entry, no `IEmailSender`/SMTP config anywhere). Postmark is chosen:

- **Transactional-first reputation.** Unlike general-purpose ESPs, Postmark's entire product and IP-reputation model is built around transactional mail deliverability — the exact use case here (never marketing/bulk).
- **Trivial REST surface.** A single `POST /email` call with a server-token header — small enough that a typed `IHttpClientFactory` client (mirroring `Bookings.PaymentsApiClient`'s exact shape) avoids adding a vendor SDK dependency at all, consistent with keeping the vendor surface this thin behind `IEmailSender`.
- **No SMTP.** Explicitly rejected per this task's own instruction — SMTP would be an accidental, unreviewed architecture choice; Postmark's REST API is a deliberate one.
- **EU-suitable.** Supports EU-region sending infrastructure for a European-market marketplace (the same market-fit reasoning `ADR-0004` already used for Stripe), though exact regional configuration is an operational setting, not an architectural one.

Selected exactly like `Payments.IPaymentProviderClient`: `NotificationsOptions.PostmarkServerToken` sourced from .NET user-secrets/environment variables, **never** `appsettings.json` (matching `Jwt:Key`/`Payments:StripeSecretKey`). When unset (local dev/test), `NotificationsModule` selects `TestEmailSender` — a deterministic in-memory fake — instead. No production credentials are required to run any test in this codebase.

### 3. `BookingHoldExpired` — a new Shared integration event, and its real trigger

```csharp
public sealed record BookingHoldExpired(
    Guid BookingId, Guid StudentUserId, Guid TutorUserId,
    DateTimeOffset StartUtc, DateTimeOffset EndUtc, int DurationMinutes, bool IsTrial,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent;
```

Carries only what `Bookings`' own domain already knows — no HTML, no subject line, no presentation content (that is entirely `Notifications`' concern, in `AbandonedCheckoutEmailTemplate`).

**New trigger: `BookingHoldExpirySweepService`**, a standard .NET `BackgroundService` driven by a `PeriodicTimer` (default 60s, `BookingsOptions.HoldExpirySweepIntervalSeconds`), added to `Bookings`. This is **not** a duplicate of Booking's state machine — it reuses the exact same conditional-`ExecuteUpdateAsync` guard the existing lazy sweep already uses (`BookingWrites.ExpireSingleHoldIfStaleAsync`, a single-row sibling of the existing `ExpireStaleHoldsAsync`), just scoped globally and time-driven instead of per-tutor-and-request-driven. A row only ever transitions `Held`/`PendingPayment` → `Expired` once; the event publishes only when the guarded update actually affected a row (`affected == 1`), the same "only publish on a genuine transition" convention `ConfirmBooking`/`CheckoutBooking` already use.

**Why a `BackgroundService`, not Wolverine's own scheduled-message delivery or a job library:** no job-scheduling library (Hangfire/Quartz) exists anywhere in this codebase. Wolverine's own delayed/scheduled delivery has no durable backing store in this project (`OPEN_QUESTIONS.md` A44 — no transactional outbox yet) and would silently lose any in-flight scheduled notification across a process restart. `BackgroundService` + `PeriodicTimer` is the .NET framework's own standard building block for this — not a hand-rolled polling loop — and its result (a real DB row transition, re-evaluated from durable state on every tick) survives a restart cleanly: a tick that never ran simply re-finds the same still-stale candidates next time.

**Sweep interval is an INTENTIONAL ENGINEERING DECISION, not a Preply-verified timing.** Preply's real delay between abandonment and the observed recovery email is explicitly **NOT VERIFIED** (`18-screenshot-evidence.md` Part C: "the exact delay between abandonment and send... is NOT VERIFIED"). 60 seconds keeps detection latency low without meaningful DB load; end-to-end delay in TutorFlow is therefore approximately *(the 10-minute hold TTL) + (up to ~60s sweep latency)* — never claimed as matching Preply's own (unknown) number.

**Exclusions are structural, not special-cased.** The sweep's `WHERE Status IN (Held, PendingPayment)` filter alone excludes Confirmed/Cancelled/Completed/NoShow/Refunded/Rescheduled bookings — no extra guard logic needed, verified by regression test against a pathological Confirmed row with a deliberately stale `HoldExpiresAtUtc` still attached.

### 4. The recovery handler — idempotent via a real DB-unique row, not an in-memory guard

`Notifications` gains `SentAbandonedCheckoutEmail` (new entity, new schema `notifications`), with a **DB-unique index on `BookingId`**. `SendAbandonedCheckoutRecoveryEmail.Handler` claims this row (insert + `SaveChangesAsync`, catching `UniqueViolation`) **before** attempting to send — a redelivered event, a retried handler invocation, or a process restart all land on the same already-claimed row and short-circuit. This is the exact "claim-then-act" pattern `Payments.ProcessStripeWebhook` already established for its own event-level idempotency.

Recipient resolution is entirely server-side: `IUserEmailQuery` (new Shared contract, implemented by `Identity`) resolves `StudentUserId` → email; the client, the event, and the email body never carry a recipient address directly. `ITutorProfileSummaryQuery` (new Shared contract, implemented by `Tutors`) resolves the tutor's display name/photo/public slug for the CTA — both following `ADR-0014`'s existing cross-module read-contract pattern exactly, with no new project reference between any of the three modules (confirmed via Roslyn `get_project_graph`).

A missing student email, an unavailable tutor profile, or a provider send failure are all handled by marking the claim row `WasSent = false` with a fixed, non-PII-bearing classification (`"No email on file for this student."` / `"Tutor profile unavailable."` / `"Provider send failed."`) — never the raw provider error text (which can echo request fields including the recipient address) and never logged with the recipient's address attached.

### 5. Email content — original TutorFlow copy, the observed information architecture

`AbandonedCheckoutEmailTemplate` reproduces the OBSERVED structure (tutor identity, slot date/time, a CTA back to the tutor, a plain footer) with original wording — Preply's actual copy ("X is ready for your lesson", "Give it a try") is never reused. Two gaps vs. the evidence are deliberately not papered over:

- **Time zone.** Preply's email states the time "based on your location" (implying a per-recipient time zone). TutorFlow stores no per-student time zone preference anywhere — the template renders the slot in UTC with an explicit "(UTC)" label rather than fabricating a personalization that doesn't exist.
- **Lesson/topic context.** `Booking` has no subject/topic field at all (confirmed by inspection) — none is rendered.

The CTA links to `{AppBaseUrl}/tutors/{slug}` — a real, already-supported TutorFlow route (the tutor's public profile, where a new trial hold can be started), never a resumption of the now-expired booking itself, and never an invented URL.

### 6. Module boundaries — Composed, not a god `Notifications` module

Only the abandoned-checkout slice is built. The tutor-message notification email (SS49–SS50, also OBSERVED) is explicitly **not** built here — a separate roadmap item, per this task's own scope boundary. `Notifications` does not reach into `Bookings`', `Identity`'s, or `Tutors`' schemas directly; all cross-module reads go through the two new `ADR-0014`-style contracts above.

## Consequences

- `TutorFlow.Shared`: new `BookingHoldExpired` event, new `IUserEmailQuery`/`ITutorProfileSummaryQuery` contracts.
- `Bookings`: new `BookingHoldExpirySweepService` (+ `BookingsOptions.HoldExpirySweepIntervalSeconds`), new `BookingWrites.ExpireSingleHoldIfStaleAsync`. No schema change.
- `Identity`: new `UserEmailQuery` (implements `IUserEmailQuery`). No schema change.
- `Tutors`: new `TutorProfileSummaryQuery` (implements `ITutorProfileSummaryQuery`). No schema change. Both this and `UserEmailQuery` are `public` (not the usual `internal` convention) — Wolverine's codegen generates direct-construction code for a consumer's transitive dependency chain and cannot see an `internal` type from another assembly (the same reason `Bookings.ActiveBookingIntervalsQuery` is already `public`).
- `Notifications`: real module for the first time — `NotificationsDbContext` (new schema `notifications`), `IEmailSender`/`PostmarkEmailSender`/`TestEmailSender`, `NotificationsOptions`, `SendAbandonedCheckoutRecoveryEmail` feature, `BookingHoldExpiredConsumer`.
- `TutorFlow.Host/Program.cs`: Notifications' assembly added to Wolverine discovery; `NotificationsDbContext`/`IdentityDbContext`/`TutorsDbContext` added to the existing `AlwaysUseServiceLocationFor` allow-list (the latter two reachable from a Wolverine handler chain for the first time, via this feature's cross-module reads).
- No frontend change — no CTA route needed building (the tutor profile page already exists); no notification-preferences surface was in scope and none was built.
- Resolves the P1 roadmap item in `15-dependency-graph-and-roadmap.md` ("abandoned-checkout recovery email... now a concrete, well-evidenced, directly buildable candidate feature").

## Noted but not fixed here

- Tutor-message notification email (SS49–SS50) — separate roadmap item, not built.
- Durable Wolverine outbox (`OPEN_QUESTIONS.md` A44) — still not built; a crash between the sweep's publish call and Wolverine's in-process delivery of that specific message remains possible, same pre-existing, already-flagged limitation every other Wolverine publisher in this codebase shares today. Not made worse by this feature; not silently fixed either.
- Per-student time zone preference — does not exist; the email renders UTC, documented as a known gap above.
- Reinstating a permanently-failed send is not built (the claim row is honestly marked unsent but nothing retries it later) — acceptable for this slice, not a silent gap.

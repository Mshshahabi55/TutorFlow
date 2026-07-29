**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001` through `ADR-019` (all Accepted). This ADR is a **design proposal, not an implementation authorization.** It records architecture for a capability — payments — that `PROJECT_CONSTITUTION.md`'s Project Scope, `PRODUCT_REQUIREMENTS.md` Section 9, and `ARCHITECTURE.md` line 60 all currently and explicitly exclude from v1 ("Payments, billing, and payouts... not modeled anywhere in this architecture"). This ADR does not amend any of those three documents and does not itself authorize writing implementation code — see Governance Note below and Non-Goals.

---

# ADR-020: Payments Architecture

## Status

**Proposed — 2026-07-22. Superseded in full by `ADR-021` (Proposed, 2026-07-27)** — the business model moved from pay-per-session to a Learning Plan/Enrollment purchase, replacing this ADR's `Payment`-per-`Session` design with `ADR-021`'s `EnrollmentPayment`-per-`Enrollment` design. This ADR's `IPaymentGateway` port shape, idempotency design, and amount-fixing discipline are carried forward into `ADR-021` by explicit reuse, not lost — see `ADR-021`'s "Relationship to ADR-020" section. This document is retained for its historical reasoning, not as an active proposal.

Sections marked **DECIDED** below restate the owner's own input to this design phase (Phase 5A), verbatim in substance. Sections marked **AWAITING OWNER DECISION** are this ADR's proposals, not yet ratified — see `docs/phases/PHASE-05A-REPORT.md` Section 6 for the complete, numbered list of what requires sign-off before any implementation phase may begin.

## Governance Note (read first)

Every currently-approved scope document — `PROJECT_CONSTITUTION.md` (Project Scope), `PRODUCT_REQUIREMENTS.md` (Section 9, "Out of Scope for v1": *"Payments, billing, and payouts"*, restated as unchanged by `ADR-018`'s own addendum), and `ARCHITECTURE.md` (line 60: payments "**not** modeled anywhere in this architecture") — currently excludes payments from v1. This ADR does not reopen, contradict, or silently narrow that exclusion. It exists because the owner directed a design-only phase (Phase 5A: "Write no production code... Implementation follows in later phases only after those decisions are made") to prepare an architecture *in case* v1's scope is later, separately, explicitly widened — the same governance posture `ADR-018` itself already took for the payment-provider constraint it recorded without building anything. **Before any implementation phase referenced in this ADR's Phase Plan may begin, the owner must make an explicit scope decision** (widening v1, or declaring this a "v1.x"/v2 item) through the same Decision-Making Process that approved Section 9 in the first place. This ADR's own approval, if it happens, ratifies the *design*; it does not, by itself, ratify writing any of it.

## Context

`DOMAIN_MODEL.md` Business Rule (Hourly Rate section) already states "no payment is processed within this domain" for v1; `PRODUCT_REQUIREMENTS.md` DISC-2 restates it. `ADR-018` (Accepted) recorded the *constraint* a future payment integration must satisfy — Iranian PSP, redirect + callback + server-side-verify — without building anything. `ADR-019` (Accepted, + Addendum 1) resolved currency/pricing representation (Rial storage, Toman display, whole-Rial invariant), a direct prerequisite for any payment amount to be meaningful. Phase 5A is the next planned step: study the existing Scheduling & Booking model, propose (not decide) how a payment fits it, and record owner decisions already made about the shape of that fit.

The existing model, summarized (full detail with file references: `docs/phases/PHASE-05A-REPORT.md` Section 3):
- `Session` and `AvailabilitySlot` are two separate aggregate roots (`ADR-015`, Accepted, ratified), coordinated only through `AvailabilitySlot.Book(...)`. `Session.Book(...)` is `internal` — the only path to a `Session` is through the `AvailabilitySlot` that produced it.
- `AvailabilitySlot.IsConsumed` is set `true` the instant `Book()` succeeds, in the same transaction as `Session` creation, and is **never reset by any existing Domain method** — `Session.Cancel()` changes `Session.Status` only.
- The sole storage-layer enforcement of "never bookable twice" (CONST-1) is an **unconditional** unique index on `Sessions.AvailabilitySlotId` (`ADR-014`) — it is not filtered by `Session.Status`, so a *Cancelled* Session still permanently occupies that slot's unique key today. `DOMAIN_MODEL.md` Open Question 7 ("does cancelling reopen the slot?") is formally unresolved, and the code's actual, structural answer today is "no, and it cannot without a schema change."
- Domain Events are dispatched synchronously, in-process, before the triggering `SaveChangesAsync` call (`ADR-012`); the audit trail stages an `AuditEntry` into the same `DbContext` inside that dispatch, so the audit write and the business mutation commit or roll back together (`ADR-016`) — but only for a `switch`-listed set of event types (currently: `SessionBooked`, `SessionRescheduled`, `SessionCancelled`, `AvailabilityDeclared`, `TutorApproved`, `TutorSuspended`, plus three authentication events added since `ADR-016`'s original six). A new event type is silently *not* audited until explicitly added to that `switch`.
- `Session` carries no cost/price field today. The only monetary fact in the system is `Tutor.HourlyRate` (Identity & Relationship), which can change at any time independent of any specific booking.

## Decision — What the Owner Has Already Decided (DECIDED)

Restated from this phase's own Context, verbatim in substance, not reopened by this ADR:

1. **No gateway account exists yet.** Design around an `IPaymentGateway` port (Application layer) with a `MockPaymentGateway` adapter (Infrastructure layer) for Development/Testing. Adding a real Iranian PSP later must require **zero** changes to Domain or Application — only a new Infrastructure adapter and DI registration.
2. **Single currency, whole Toman amounts.** Governed entirely by `ADR-019` + Addendum 1; not reopened here. A `Payment`'s amount is a whole-Rial figure with the same `HourlyRate.CurrencyCode`/divisibility discipline.
3. **Refunds are recorded, not executed.** No gateway refund API call exists in v1. An Admin records that a refund occurred (presumably by some out-of-band/manual mechanism); the Domain models the fact, not the mechanism.
4. **Payouts to Tutors and platform commission are out of MVP scope.** This ADR notes, but does not design, where they would attach (Forward Compatibility, below).
5. **Never trust a callback.** Verification is always a server-initiated call from our backend to the gateway; the amount stored at initiation is re-checked server-side against what the gateway confirms; the entire flow is idempotent against a duplicate or replayed callback.

## Proposal — The Payment Aggregate (AWAITING OWNER DECISION)

**A new aggregate root, `Payment`, distinct from `Session` — two aggregates, not one.** This is consistent with, not a departure from, `ADR-015`'s already-ratified reasoning for `AvailabilitySlot`/`Session`: "favor small aggregates sized to their true invariant; reference other aggregates by identity; one aggregate mutated per transaction." `Payment`'s state machine (money) and `Session`'s state machine (scheduling) are genuinely independent concerns — forcing a payment-status write to also touch/lock the `Session` row would coupling two lifecycles that have no shared invariant, exactly the coupling `ADR-015`'s heuristics warn against. `Payment` references `SessionId` by identity only, the same pattern `Session` already uses for `TutorId`/`StudentId`/`AvailabilitySlotId` (no navigation properties, per `ADR-002`).

**Bounded context: a new context, "Payments," not folded into Scheduling & Booking.** `ADR-002`'s Context Ownership table assigns Scheduling & Booking exactly `Session`, `AvailabilitySlot`, and their value objects — nothing money-shaped. A new context is downstream of Scheduling & Booking (reads `Session` existence/identity through Scheduling & Booking's own interface, per `ADR-002`'s Integration Rules — never direct data access), the same relationship Discovery and Marketplace Oversight already have. This itself needs the same explicit ratification `ADR-015` required for `AvailabilitySlot`/`Session` — it is proposed, not decided, here.

### States and Transitions

```
Initiated ──VerifyAndMarkPaid(gatewayAmount)──▶ Paid
Initiated ──MarkFailed(reason)────────────────▶ Failed
Initiated ──Expire()───────────────────────────▶ Expired
Paid ──RecordRefund(amount, adminId, reason)──▶ Paid (adds a child record; Status itself does not change)
```

- **`Initiated`** — the only non-terminal state. Created when a Session (or, contingent on the Task 2 decision below, an intent-to-book) requests payment. The amount is fixed here (see Idempotency/Amount below) and never changes again.
- **`Paid`** — terminal-success. Reached only via a successful *server-initiated* verify call whose gateway-confirmed amount exactly equals the amount fixed at `Initiated`. A mismatch never silently "corrects" to the stored amount — it is rejected as `Failed` (`PROJECT_CONSTITUTION.md` Engineering Principle 4: fail safely and visibly).
- **`Failed`** — terminal-failure. Reached via an explicit gateway-reported failure, or a server-side amount mismatch. Distinguished from `Expired` deliberately: Tutor/Admin support should be able to tell "the gateway declined this" from "the customer never came back," even though both end with no money collected.
- **`Expired`** — terminal, no-callback-ever-arrived. *What triggers this transition, and when, is contingent on the Task 2 (slot-reservation) decision below — not fully specified by this ADR alone.*
- **No resurrection.** A retry after `Failed`/`Expired` is a **new** `Payment` row referencing the same `SessionId`, never a mutation of the dead one — every real attempt is its own auditable record.
- **Refunds are a child record on `Payment`, not a separate aggregate root, and not attached to `Session`.** A refund has no independent lifecycle beyond "recorded" and no invariant `Payment` cannot itself enforce (total refunded ≤ amount paid; only recordable against a `Paid` payment) — by the same small-aggregate heuristic, it does not need root status. `Session`'s own lifecycle (`Cancel`/`Complete`/`MarkNoShow`) stays completely independent of whether a refund was later recorded — a refund is a fact about money, `Payment`'s concern, not `Session`'s.

### Invariants

- At most one `Payment` may be `Initiated` for a given `SessionId` at a time — enforced by a **filtered/partial unique index** on `(SessionId)` `WHERE Status = Initiated`, the identical mechanism `ADR-014` already established for `Sessions.AvailabilitySlotId` (unconditional there; filtered here, since unlike a booking, a *sequence* of dead payment attempts against the same Session is legitimate — only one *live* attempt at a time).
- `Amount` is immutable once `Initiated`. No Domain method accepts an amount parameter that overwrites it — `VerifyAndMarkPaid` accepts only the *gateway's reported* amount, for comparison, never assignment.
- Total recorded refund amount never exceeds `Amount`.
- A refund may only be recorded against a `Paid` payment.

### Idempotency

- **Primary lookup key: our own `PaymentId`**, encoded by us into the gateway's callback/return URL at `Initiate` time — entirely under our control, never gateway-supplied, so it cannot be spoofed by pointing a forged callback at a different payment.
- **Secondary cross-check: `GatewayReferenceId`**, a provider-opaque string returned by the gateway's own `Initiate` response and stored on `Payment` at that time. The callback re-presents it; the handler confirms it matches what was stored, guarding against a replayed/forged callback for a *different* payment being routed at our endpoint.
- **Duplicate/replayed callback handling: a status check, not a new mechanism.** The callback handler's first action is loading `Payment` by `PaymentId` and checking `Status`. If it is already `Paid` or `Failed` (i.e., not `Initiated`), the handler returns success/no-op **without** calling the gateway's verify endpoint again or mutating state again. This reuses the aggregate's own state machine for idempotency — no new idempotency-key table, consistent with this project's standing "no new infrastructure" posture (`ADR-016`'s treatment of the Outbox pattern is the precedent for taking that posture seriously).

### How the Authoritative Amount Is Fixed

`Amount` is computed once, at `Payment.Initiate(...)`, from the `Session`'s already-committed `Duration` and the Tutor's `HourlyRate` **at that moment** — then stored, and never re-derived. **Corollary finding, not resolved by owner Context:** `Tutor.HourlyRate` can change at any time (Identity & Relationship), independent of any specific booking, and `Session` carries no price field today. This ADR proposes "fix at `Payment.Initiate` time, from whatever `HourlyRate` reads at that instant" as the simplest rule consistent with "fixed at initiation so a later callback cannot change it" — but does **not** resolve whether a Tutor changing their rate *between booking and payment* should affect an in-progress booking, a genuinely new question this design surfaces. Listed in `docs/phases/PHASE-05A-REPORT.md` Section 6.

### Domain Events

`PaymentInitiated`, `PaymentVerified` (→ Paid), `PaymentFailed`, `PaymentExpired`, `PaymentRefundRecorded` — named as past-tense occurrences, mirroring `DOMAIN_MODEL.md`'s existing convention. **All five are governance-relevant state (money changing hands) and must be added to `AuditDomainEventHandler`'s explicit `switch`** (`CLAUDE.md`'s own non-negotiable rule: "Every new Domain Event that mutates governance-relevant state must be covered by the audit trail") — a required implementation-phase task, not optional, and arguably a stronger candidate for audit coverage than several of the six `ADR-016` originally scoped in.

## Proposal — The Slot-Reservation Problem (AWAITING OWNER DECISION)

Full three-option analysis with worked failure/abandonment/race behavior: `docs/phases/PHASE-05A-REPORT.md` Section 4. Summarized:

- **Option A — consume immediately (today's exact behavior), zero new Domain machinery, pure-ops manual recovery on abandonment.** Cheapest possible code change. Structurally blocked today: no Domain method un-consumes a slot, and the unconditional unique index means even a Cancelled Session permanently occupies its slot — "manual admin intervention" would in practice mean declaring a brand-new, duplicate `AvailabilitySlot`, not truly recovering the original. Honestly viable for a pre-launch MVP with no real traffic, exactly as the phase brief anticipated — but it is a real product cost (a Tutor's slot is unusable by anyone until an Admin notices and acts), not a free option.
- **Option B — a time-boxed reservation hold, `Session` creation deferred until payment succeeds.** Requires a genuinely new state dimension (a TTL/expiry concept) and moves `Session` creation later than it happens today — the largest behavior change of the three. Cleanly composable with either a lazy on-read expiry check (no new infrastructure) or a background sweep (new infrastructure — same governance flag `ADR-016` already raised for the Outbox pattern).
- **Option C (recommended) — consume immediately (today's behavior unchanged), but give the slot a real release valve: a new `AvailabilitySlot.Reopen()` Domain method plus a filtered unique index on `Sessions.AvailabilitySlotId` (`WHERE Status <> Cancelled`).** An explicit gateway-reported failure triggers an immediate, same-transaction `Session.Cancel()` + slot reopen — no waiting, no TTL. True silent abandonment (user never returns, no callback ever fires) still needs a policy, cheapest as a lazy on-read check rather than a background sweep. This resolves `DOMAIN_MODEL.md` Open Question 7 as a direct, motivated byproduct, changes the least about today's proven booking flow, and needs no new `SessionStatus` value under any of the three options (worth noting: none of A/B/C requires touching `Session`'s already-ratified state machine at all).

**This ADR recommends Option C, with reasoning, but does not decide it — Task 2's own instruction, restated: this is the owner's call.**

## The Port — `IPaymentGateway` (AWAITING OWNER DECISION)

**Layer: `Application`, interface only** (e.g. `Application/Payments/Interfaces/IPaymentGateway.cs`) — the identical convention `IUnitOfWork` and every repository interface (`ISessionRepository`, `ITutorRepository`, ...) already follow: Application defines the port it needs, Infrastructure implements it, Domain never references Infrastructure (`ADR-005`). `MockPaymentGateway` and, later, a real provider adapter both live in `Infrastructure/Payments/` — nothing provider-specific is reachable from Domain or Application by construction, the same guarantee `IUnitOfWork`'s Npgsql-vs-SQLite split already demonstrates in this codebase.

```csharp
public interface IPaymentGateway
{
    Task<GatewayInitiateResult> InitiateAsync(GatewayInitiateRequest request, CancellationToken cancellationToken = default);
    Task<GatewayVerifyResult> VerifyAsync(GatewayVerifyRequest request, CancellationToken cancellationToken = default);
}
```

- `GatewayInitiateRequest` — amount (whole Rial), our own `PaymentId` (so the gateway echoes a merchant reference and our callback URL can embed it), a callback/return URL, an optional description. No provider-specific field.
- `GatewayInitiateResult` — `Success(RedirectUrl, GatewayReferenceId)` or `Failure(reason)`. `RedirectUrl` is what Presentation sends the browser to; `GatewayReferenceId` is what the aggregate's idempotency design stores.
- `GatewayVerifyRequest` — `GatewayReferenceId`.
- `GatewayVerifyResult` — `Success(VerifiedAmount)` or `Failure(reason)`. Deliberately returns the amount the gateway confirms rather than a boolean "matches" — the actual match-or-reject business rule stays in Application/Domain, never delegated to the adapter, mirroring the same "no business rule leaks into Infrastructure" discipline `ADR-005`/`ADR-007` already establish elsewhere.
- **Failure-mode split, per `ADR-008`:** a business-shaped outcome (card declined, user cancelled at the gateway, amount mismatch) is a `Result`-style return, handled as a Domain-Error-equivalent outcome — exactly how `ADR-014`'s unique-constraint conflict is already classified. A genuine technical fault (gateway unreachable, malformed response, timeout) throws and is handled by the existing Infrastructure Failure path (`GlobalExceptionHandler`) — no new error-handling mechanism.

### What `MockPaymentGateway` Must Simulate

Success (verified amount matches); gateway failure (`Initiate` or `Verify` returns `Failure`); user abandonment (`Initiate` succeeds, `Verify` is simply never invoked by the test — not a distinct mock behavior, a test scenario); duplicate callback (the *test* invokes verify/the callback path twice — the mock needs no special handling, since idempotency lives in `Payment`'s own state check, not the gateway; the mock only needs to answer consistently both times); amount mismatch (mock configurable to report a different verified amount than requested, to prove Application's own rejection works); a slow/timed-out gateway (mock configurable with an artificial delay or a timeout-shaped exception, to prove caller-side cancellation/timeout handling works).

### Preventing `MockPaymentGateway` Outside Development

Mirrors two patterns this codebase already has: `Program.cs`'s `IsEnvironment("Testing")` branch (Npgsql services never added in that branch) and the Development-only seed-data guard (`app.Environment.IsDevelopment()`). Proposed: DI registration branches on `builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing")` — the mock only inside that branch; the `else` branch either registers a real adapter or fails fast at startup with a clear configuration error if none is configured, mirroring the existing `REPLACE_ME` connection-string fail-fast check. **Proof, described not written** (this phase writes no code): a test that constructs the DI container with `Environment = "Production"` and asserts either a clear startup configuration exception, or that resolving `IPaymentGateway` never yields `MockPaymentGateway` — the same shape as this codebase's existing `CorsConfigurationTests.cs`/connection-string-guard tests.

## Forward Compatibility

- **A second currency (v2):** already governed entirely by `ADR-019`'s own Forward Compatibility clause — `Payment.Amount`/`CurrencyCode` reads through the same seam `HourlyRate` already established. Nothing payment-specific to add here.
- **A real PSP (v1.x or v2):** a new `Infrastructure/Payments/` adapter implementing `IPaymentGateway`, plus a DI registration change. Zero Domain/Application changes, by the port's own design (owner Decision 1).
- **Payouts to Tutors (v2):** would attach as a new aggregate (e.g., `Payout`) in the same Payments context, referencing one or more `Payment`s by identity — structurally the same shape as `Payment`→`Session` today. Not designed further here (out of MVP scope, owner Decision 4).
- **Platform commission (v2):** a derived computation over `Payment.Amount` (a percentage, a flat fee, or a rate-card lookup) — most naturally a value carried on `Payment` itself (e.g., a `PlatformFeeAmount` field fixed at the same `Initiate` moment as `Amount`, for the same "never changes after the fact" reason) rather than a new aggregate. Not designed further here (out of MVP scope, owner Decision 4).
- **Real gateway refund execution (v2, if ever authorized):** `Payment.RecordRefund(...)`'s Domain shape (amount, reason, who recorded it) does not need to change to eventually be *triggered by* a gateway call instead of purely recorded — the aggregate already separates "the fact of a refund" from "how it was carried out."

## Consequences

**Becomes possible:** a concrete, reviewable implementation plan exists (Phase Plan, `docs/phases/PHASE-05A-REPORT.md` Section 7) for the first time; the currency/pricing prerequisite (`ADR-019`) and this design together mean no further design-only phase is needed before implementation, once the owner decides Section 6's open items.

**Becomes harder / newly constrained:** any implementation phase must extend `AuditDomainEventHandler`'s `switch` (a real, easy-to-forget step — flagged explicitly above); the slot-reservation decision (Task 2) gates whether `Payment` references `SessionId` alone or also needs `AvailabilitySlotId` during a pre-booking hold, so Domain implementation cannot fully start until that decision lands.

**Now forbidden (restated, not new):** no payment gateway SDK or package may be added by this ADR or by reading it as authorization (it authorizes a design, not a dependency); no payout/commission/automated-refund implementation under cover of this ADR (owner Decision 4, restated); no implementation code at all until the Governance Note's scope decision is made.

## Supersedes / Relates To

- **Depends on** `ADR-019` (+ Addendum 1) for currency/amount representation — not reopened here.
- **Extends** `ADR-018`'s payment-integration constraint (Iranian PSP, redirect + callback + server-verify) into a concrete port shape; does not alter that constraint.
- **Follows the precedent of** `ADR-015` for aggregate-boundary reasoning (small aggregates, identity references, one-aggregate-per-transaction) and explicitly requests the same category of ratification `ADR-015` itself required.
- **Depends on** `ADR-014`'s unique-constraint-based concurrency pattern, reused (filtered, not unconditional) for `Payment`'s own idempotency invariant.
- **Restates, does not alter,** `PRODUCT_REQUIREMENTS.md` Section 9 / `PROJECT_CONSTITUTION.md` Project Scope's exclusion of payments from v1 — see Governance Note.
- **Relates to, does not resolve,** `DOMAIN_MODEL.md` Open Question 7 (does cancelling reopen the slot) — Option C's design would resolve it as a byproduct, contingent on the owner choosing Option C.

## Non-Goals

Does not implement any code (this phase's own absolute rule). Does not add any package or gateway SDK. Does not design payouts, platform commission, or automated gateway-executed refunds (owner Decision 4 — noted in Forward Compatibility only). Does not choose a specific Iranian PSP (`ADR-018` already declined to, and this ADR does not revisit that). Does not amend `PRODUCT_REQUIREMENTS.md` Section 9, `PROJECT_CONSTITUTION.md`'s Project Scope, or `ARCHITECTURE.md` line 60 — see Governance Note. Does not decide the slot-reservation option (Task 2) or the Payments-as-new-bounded-context question — both are listed, with a recommendation, in `docs/phases/PHASE-05A-REPORT.md` Section 6.

## Questions Requiring Approval

See `docs/phases/PHASE-05A-REPORT.md` Section 6 for the complete, numbered list with recommendations. Restated here by title only: (1) the v1-scope Governance Note itself; (2) which slot-reservation option; (3) Payments as a new bounded context vs. folded into an existing one; (4) the HourlyRate-drift-between-booking-and-payment question; (5) lazy on-read expiry check vs. a background sweep (and whether a background sweep counts as "new infrastructure" needing separate governance); (6) exact wording/shape of the audit-trail extension; (7) the implementation phase breakdown itself.

---

*Status: Proposed — 2026-07-22. Superseded in full by `ADR-021` — 2026-07-27. No section of this ADR was ever Accepted.*

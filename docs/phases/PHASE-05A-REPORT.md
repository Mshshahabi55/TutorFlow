# Phase 5A Report — Payments: Design and ADR Only

Branch: `develop` | Commits this phase: one, docs-only (`docs/adr/ADR-020-payments-architecture.md`
+ this report). **Zero `backend/src/` or `frontend/src/` changes** — this
was a design phase; that constraint was honored throughout, not merely at
the end.

**Status: complete.** Studied the existing Scheduling & Booking model in
full before proposing anything (Task 1). Laid out three viable
slot-reservation designs and recommended one without deciding it (Task 2).
Proposed a `Payment` aggregate and an `IPaymentGateway` port (Tasks 3–4).
Wrote `docs/adr/ADR-020-payments-architecture.md`, Proposed status, and a
six-phase implementation plan (Task 5). One governance finding surfaced
immediately and shaped everything after it: **payments remain explicitly
out of v1 scope in three separately-approved documents**, unchanged by
anything in this phase — see Section 1.

## 1. Clean Architecture validation

No `backend/src/` or `frontend/src/` file was read-then-modified, created,
or deleted this phase — verified by `git status`/`git diff` showing only
`docs/adr/ADR-020-payments-architecture.md` and this report as new files
at every commit point. The *proposal itself* was checked against this
project's own layer rules as it was written, not as an afterthought:
`IPaymentGateway` is proposed as an `Application`-layer interface only
(mirroring `IUnitOfWork`/every repository interface), `Payment` as a pure
Domain aggregate with zero outward dependency, and `MockPaymentGateway`/any
real adapter confined to Infrastructure — the same Domain ← Application ←
Infrastructure/Web direction this codebase already enforces everywhere
else, not a new pattern invented for payments.

**Governance finding, surfaced by Task 1 and carried through every
subsequent task:** `PROJECT_CONSTITUTION.md`'s Project Scope,
`PRODUCT_REQUIREMENTS.md` Section 9 ("Payments, billing, and payouts" —
Out of Scope for v1), and `ARCHITECTURE.md` line 60 ("Payments... **not**
modeled anywhere in this architecture") all currently, explicitly exclude
payments from v1 — and `ADR-018`'s own addendum restates Section 9 as
"unchanged." This phase's brief directs payments *design* work, framed as
input from the owner; nothing in that brief amends any of the three
scope-excluding documents. Per `CLAUDE.md`'s own stop condition
("A governing document appears to contradict another... report the
contradiction; don't silently pick a side"), this is flagged explicitly
rather than quietly worked around: `ADR-020` is written as a **design
proposal only**, mirroring exactly how `ADR-018` recorded a
payment-integration constraint without building anything while the same
Section 9 exclusion stood. Before any implementation phase in Section 7's
plan begins, the owner must make an explicit, separate scope decision —
this is Section 6, Item 1.

## 2. Repository convention validation

One docs-only commit this phase (plus this report as a second), Conventional
Commits, no `bin/`/`obj/`/`node_modules`/`dist` staged, no secret touched.
No database read or write of any kind — this phase never opened a
connection. No test run, weakened, or deleted, since no code changed
(`scripts/verify.ps1` was not run for this phase — see Section 7 for why
that's the right call for a design-only phase).

## 3. Task 1 — the existing model, studied before proposing anything

**`Session`'s full lifecycle** (`backend/src/Domain/Scheduling/Session.cs`):
one status at a time — `Scheduled → Completed | Cancelled | NoShow`
(`SessionStatus.cs`), each a one-way transition guarded by
`if (Status != Scheduled) throw`. `Session.Book(...)` is `internal` —
`BookSessionCommandHandler` never calls it directly; it calls
`AvailabilitySlot.Book(...)`, which is the *only* caller. Every transition
raises exactly one Domain Event (`SessionBooked`/`Rescheduled`/`Cancelled`/`Completed`/`MarkedNoShow`)
and is triggered by its own single-purpose handler
(`BookSessionCommandHandler`, `RescheduleSessionCommandHandler`,
`CancelSessionCommandHandler`, `CompleteSessionCommandHandler`,
`MarkSessionNoShowCommandHandler` — all in `backend/src/Application/Scheduling/Handlers/`),
each doing: load → authorize (party-to-the-Session, or Admin/Staff,
per `AUTHORIZATION_MATRIX.md` §4.3) → call the one Domain method → `IUnitOfWork.SaveChangesAsync`.
**No handler currently has any notion of payment** — booking succeeds or
fails purely on slot availability and authorization.

**`AvailabilitySlot.Book()` and `IsConsumed`**
(`backend/src/Domain/Scheduling/AvailabilitySlot.cs`): `IsConsumed` is a
plain `bool`, `false` by construction, set `true` inside `Book()` in the
same in-memory call that constructs the `Session` — both persisted in one
`SaveChangesAsync` in `BookSessionCommandHandler`. **No Domain method
resets it** — `Session.Cancel()` never reaches back into
`AvailabilitySlot`. The backing unique index
(`SessionConfiguration.cs`: `builder.HasIndex(s => s.AvailabilitySlotId).IsUnique()`)
is **unconditional** — not filtered by `Status` — so a Cancelled Session's
row still permanently occupies that unique key. This is the single most
important fact this study surfaced for Task 2: **a Session, once created,
can never be structurally "undone" from its slot today**, regardless of
its status. `DOMAIN_MODEL.md` Open Question 7 ("does cancelling reopen
the slot?") is formally open, and the code's real, structural answer is
currently "no, and it cannot without a schema change."

**How `Cancel`/`Reschedule`/`Complete`/`MarkNoShow` work:** all four
handlers share one shape (Section above); the only difference is which
role may call each, per `AUTHORIZATION_MATRIX.md` §4.3 — Admin/Staff or
any party may Cancel/Reschedule, but only the assigned Tutor or Admin/Staff
may Complete/mark No-Show (`ADR-003` Addendum, Decision 1). None of the
four currently has any interaction with money, since none exists yet.

**`DOMAIN_MODEL.md` open questions touching booking or money:**
Question 7 (slot reopening on cancel — directly load-bearing for Task 2,
above); Question 8 (min/max lead time for booking — not directly relevant
to payments, noted for completeness); Question 18 (Hourly Rate
currency/format — resolved by `ADR-019`, not reopened). **New finding,
not a pre-existing open question:** `Session` carries no price/cost field
at all today — the only monetary fact anywhere is `Tutor.HourlyRate`
(Identity & Relationship), readable and mutable independent of any
specific booking. This directly affects Task 3's "how is the amount
fixed" design — see Section 5 and Section 6 Item 4.

**How `EfUnitOfWork`, Domain Events, and the audit trail cooperate**
(`backend/src/Infrastructure/Persistence/EfUnitOfWork.cs`,
`Infrastructure/Common/DomainEventDispatcher.cs`,
`Infrastructure/Audit/AuditDomainEventHandler.cs`): `SaveChangesAsync`
dispatches every touched aggregate's Domain Events **synchronously,
in-process, before** calling `_dbContext.SaveChangesAsync()` (`ADR-012`).
The only consumer currently registered is `AuditDomainEventHandler`, which
stages an `AuditEntry` into the *same* `DbContext` — so the audit write
and the business mutation commit or roll back together, with no second
transaction and no new infrastructure (`ADR-016`). Critically,
`AuditDomainEventHandler.TryCreateAuditEntry` is an explicit `switch`
over **named event types only** — `SessionBooked`, `SessionRescheduled`,
`SessionCancelled`, `AvailabilityDeclared`, `TutorApproved`,
`TutorSuspended`, plus three authentication events added since `ADR-016`'s
original six (`ADR-016`'s Scope Note lists the original six by name). **A
new event type is silently not audited until explicitly added to this
switch** — a real, easy-to-miss step any payments implementation phase
must not forget (restated in `ADR-020` and Section 6). A payment event is
a strong audit candidate: it is squarely "governance-relevant state"
under `CLAUDE.md`'s own non-negotiable rule, arguably more so than several
of the six `ADR-016` originally scoped in.

## 4. Task 2 — the slot-reservation problem: three designs, one recommendation

The core tension: `BookSessionCommandHandler` today creates `Session` and
consumes the slot **atomically, with no payment step**. Once a real
payment exists, there is a real window — initiate, redirect, [gateway],
callback, server-verify — during which *something* must represent the
slot's state. Section 3's finding (no slot-reopen mechanism exists; the
unique index is unconditional) constrains every option below.

### Option A — consume immediately, zero new machinery, manual admin recovery

**Slot representation during the window:** identical to today — `Session`
created and `IsConsumed = true` at booking time, before payment even
starts. **On success:** nothing changes; a `Payment` record simply
reaches `Paid`. **On explicit failure:** `Session` remains `Scheduled`,
slot remains consumed — no automatic recovery. **On abandonment:**
identical to failure — permanently stuck without human action. **Two
students racing:** resolved exactly as today (the unique index), unaffected
by payment at all — the race is over before payment starts.

**New machinery required: in the strictest reading, none at all** — this
is the literal "do nothing clever" option. But recovering a stuck slot is
**not actually possible today** without new machinery: no Domain method
un-consumes a slot, and the unconditional unique index means even
Cancelling the stuck `Session` would not free the slot for a new booking
(the old row still holds the unique key). "Manual admin intervention," as
literally available today, means an Admin declaring a **new, duplicate**
`AvailabilitySlot` at the same time and separately contacting the Tutor —
a real operational workaround, not a bug fix. `ADM-4` ("resolve booking
conflicts") is exactly this class of feature and is itself currently
unimplemented (`Permission.ResolveBookingConflict` exists; no handler
does) — so even "manual" recovery has no admin tooling to perform it
through today, only direct database access.

**Interaction with the unique index:** none — no schema change.

**Cost/risk:** the cheapest possible option by far, and — honestly
assessed, as asked — **a real contender for a pre-launch MVP with no
gateway account and no live traffic yet.** The cost is a real, ongoing
one once traffic exists: a Tutor's slot is unusable by anyone, tutor
included, until a human notices and works around it by hand. This cost
scales with abandonment rate and traffic, both currently zero.

### Option B — time-boxed reservation hold, Session creation deferred

**Slot representation during the window:** a new, genuinely transient
concept — the slot is neither "open" nor "permanently consumed," but
"held until `<timestamp>`." `Session` is **not created** until payment is
verified successfully (a real behavior change from today). **On success:**
atomically mark the slot consumed and create `Session` — the same shape
`BookSessionCommandHandler` uses today, just gated behind verification
instead of triggering it. **On failure:** the hold is released immediately;
slot becomes bookable again. **On abandonment:** the hold's TTL elapses;
recovery is automatic, either via a lazy check performed the next time
anyone attempts to book that slot (no new infrastructure), or a scheduled
background sweep (new infrastructure — the same class of governance flag
`ADR-016` already raised and declined by default for the Outbox pattern).
**Two students racing:** whoever successfully creates a *live* hold first
wins; the second is rejected explicitly — requires a **new** guard (a
filtered unique index on whatever holds the reservation, keyed to
"currently live" holds only), since the existing `Sessions.AvailabilitySlotId`
index cannot help here — no `Session` exists yet to protect with it.

**New machinery required:** a TTL/expiry concept (new Domain state); a
policy decision for exactly how long a hold lasts (too short: a slow-but-
legitimate payer loses their slot mid-payment; too long: an abandoning
user blocks the slot almost as long as Option A's stuck-forever case,
just automatically recovered instead of permanently stuck); `Session`
creation timing moves later in the flow than every existing test and the
current UX assumes.

**Interaction with the unique index:** the *existing* `Sessions.AvailabilitySlotId`
index still does its current job unchanged (only one *paid* booking can
ever exist per slot); a **new**, separate uniqueness mechanism is needed
for "who currently holds the live reservation," structurally analogous to
today's but one layer earlier and on a different aggregate/table.

**Cost/risk:** the most complex of the three, and the largest behavior
change — real users would see a materially different booking flow (a
slot only becomes "theirs" after payment, not at click-to-book time), a
genuine product/UX question beyond this ADR's own scope.

### Option C — consume immediately (unchanged), add a proper release valve — **recommended**

**Slot representation during the window:** identical to today, same as
Option A — `Session` created and slot consumed immediately, no booking-flow
change at all. The difference from Option A is entirely in what happens
*after*: a new `AvailabilitySlot.Reopen()` Domain method, and the unique
index on `Sessions.AvailabilitySlotId` becomes **filtered**
(`WHERE "Status" <> Cancelled`) instead of unconditional. **On success:**
identical to Option A. **On explicit gateway-reported failure:** the
verification callback handler, in the same transaction, calls
`Session.Cancel()` **and** `AvailabilitySlot.Reopen()` — immediate,
deterministic, no waiting, since an explicit failure is already a definite
signal with nothing to wait for. **On abandonment:** the harder half of
the problem — still needs *something* to eventually notice a `Session`
whose `Payment` never resolved. Cheapest answer: a lazy check (when the
Tutor's own slot list or an Admin conflict-resolution view is next read,
surface/offer-to-resolve any `Scheduled` Session whose `Payment` is still
`Initiated` past some age) rather than a background sweep — but this
still requires *a* policy decision (Section 6, Item 5). **Two students
racing:** unchanged from today — still resolved by the unique index,
before payment ever starts, exactly as Option A.

**New machinery required:** one new Domain method (`Reopen()`); one
migration (index becomes filtered); an abandonment-detection policy for
the true-silence case only (the explicit-failure case needs no new
policy — it's handled synchronously and deterministically).

**Interaction with the unique index:** directly resolves the structural
gap Section 3 identified — this option *is*, in large part, the fix for
"a Cancelled Session permanently blocks its slot," which is true today
independent of payments entirely.

**Cost/risk:** moderate, and meaningfully lower than Option B's, because
the happy-path booking UX is completely unchanged from what is already
built, tested, and proven. The explicit-failure half of the problem is
solved with no policy question at all (unlike Option B, which needs a TTL
policy for every abandonment, successful or not). The one remaining risk
is identical in kind to Option B's hardest case — true silent
abandonment — just narrower in scope (it's the only case needing a
policy, not every non-explicit-failure outcome).

### Recommendation

**Option C**, for four reasons: it changes the least about today's
proven, tested booking flow (lowest implementation risk); it turns
Option A's structural blocker (no way to ever recover a slot) into a
real, permanent Domain capability instead of an ops-only workaround with
no admin tooling to perform it; it resolves `DOMAIN_MODEL.md` Open
Question 7 as a direct, motivated consequence rather than a separate
future decision; and it avoids new infrastructure for the *common* case
(explicit failure), leaving only true silent abandonment as a narrower,
separately-decidable policy question. **This is a recommendation, not a
decision** — Section 6, Item 2 lists it explicitly for the owner, per
this phase's own instruction to stop short of assuming it.

## 5. Tasks 3 & 4 — the Payment aggregate and the port, in brief

Full detail, invariants, and exact method signatures:
`docs/adr/ADR-020-payments-architecture.md`. Summary:

- **`Payment`** is proposed as a **third, independent aggregate root** —
  not merged into `Session` — directly consistent with `ADR-015`'s
  already-ratified reasoning for `AvailabilitySlot`/`Session` (small
  aggregates sized to their true invariant, referenced by identity, one
  aggregate mutated per transaction). States: `Initiated → Paid | Failed | Expired`,
  with refunds modeled as a child record on `Payment` (not a separate
  aggregate, not attached to `Session`) since a refund's own invariant
  (≤ amount paid; only against a `Paid` payment) is fully enforceable by
  `Payment` itself.
- **Idempotency** is a status check on `Payment`'s own state machine
  (already-`Paid`/`Failed` short-circuits a repeated callback), keyed
  primarily by our own `PaymentId` (embedded in the callback URL we
  control) with the gateway's `GatewayReferenceId` as a secondary
  cross-check — no new idempotency-key table.
- **Amount** is fixed once, at `Payment.Initiate(...)`, from `Session.Duration`
  × `Tutor.HourlyRate` read at that instant, and never re-derived — a
  callback can only *confirm* it, never supply or change it. This
  surfaced a genuine new question (`HourlyRate` can drift between booking
  and payment; `Session` has no price field today) not resolved by owner
  Context — Section 6, Item 4.
- **`IPaymentGateway`** lives in `Application` (interface only), the
  identical convention `IUnitOfWork`/every repository already follows;
  `MockPaymentGateway` and any real adapter live in `Infrastructure`. Two
  methods, `InitiateAsync`/`VerifyAsync`, both provider-agnostic by
  construction — the match-or-reject amount check happens in
  Application/Domain, never delegated to the adapter. Business-shaped
  failures (declined, mismatch) are `Result`-style Domain-Error-equivalent
  outcomes (`ADR-008`); genuine technical faults throw through the
  existing Infrastructure Failure path — no new error-handling mechanism.
  Mock-outside-Development prevention mirrors this codebase's existing
  `IsEnvironment("Testing")`/Development-seed-guard pattern exactly.

## 6. Decisions Awaiting the Owner

Every item below blocks at least one implementation phase in Section 7.
None is assumed, defaulted, or silently resolved by this report or
`ADR-020`.

1. **Scope.** Payments remain excluded from v1 in `PROJECT_CONSTITUTION.md`,
   `PRODUCT_REQUIREMENTS.md` Section 9, and `ARCHITECTURE.md` line 60 —
   unchanged by this phase or `ADR-020`. **Question:** does the owner
   widen v1 scope to include payments, designate this a "v1.x"/v2 item
   with a defined trigger to begin, or leave it fully undecided for now?
   **Recommendation:** make this decision explicitly, in writing, through
   the same Decision-Making Process that approved Section 9 — before
   Phase 5C (Section 7) is scheduled, not as a side effect of scheduling
   it.
2. **Slot-reservation design.** Option A, B, or C (Section 4).
   **Recommendation: Option C** — lowest risk, resolves a real standing
   open question, avoids new infrastructure for the common case.
3. **Payments as a new bounded context, vs. folded into Scheduling &
   Booking.** `ADR-020` proposes a new context ("Payments"), consistent
   with `ADR-002`'s Context Ownership table naming nothing money-shaped
   under Scheduling & Booking today. **Recommendation:** ratify a new
   context — the same category of explicit ratification `ADR-015`
   required for `AvailabilitySlot`/`Session`, not a decision to make by
   default.
4. **HourlyRate drift between booking and payment.** `Tutor.HourlyRate`
   can change independent of any specific booking; `Session` carries no
   price field today. **Question:** should a Tutor's rate change affect
   an in-progress (not-yet-paid) booking, or should the rate be captured
   earlier than `Payment.Initiate` time (e.g., at booking time, on
   `Session` itself)? **Recommendation:** capture the rate on `Session`
   at booking time (a small, additive Domain change, likely a Phase 5C
   task), so `Payment.Amount` derives from an already-fixed `Session`
   value rather than a live, mutable Tutor attribute — removes the drift
   question entirely rather than deciding how to handle it.
5. **Abandonment detection mechanism.** Lazy on-read check vs. a
   background sweep, for Option C's (or any option's) true-silent-
   abandonment case. A background sweep is "new infrastructure" in the
   same sense `ADR-016` already flagged for the Outbox pattern, and by
   this project's own standing rule would need that constraint
   separately revisited before adoption. **Recommendation:** lazy
   on-read check first (zero new infrastructure); revisit only if it
   proves operationally insufficient once real traffic exists.
6. **Audit-trail extension wording.** `ADR-020` proposes five new events
   (`PaymentInitiated`, `PaymentVerified`, `PaymentFailed`,
   `PaymentExpired`, `PaymentRefundRecorded`) all added to
   `AuditDomainEventHandler`'s `switch`. **Question:** does the owner
   want all five audited, or a narrower subset (mirroring how `ADR-016`
   scoped only six of `DOMAIN_MODEL.md`'s eleven named events)?
   **Recommendation:** audit all five — money changing hands is squarely
   the kind of governance-relevant fact `CLAUDE.md`'s own non-negotiable
   rule and `PROJECT_CONSTITUTION.md`'s CONST-2 exist to capture, more so
   than several already-audited events.
7. **The implementation phase breakdown itself** (Section 7).
   **Recommendation:** approve as proposed, or amend — either way, no
   phase in it should start before Items 1–2 above are resolved, since
   both gate what Phase 5C/5D can actually build.

## 7. Task 5 — ADR-020 status and the proposed phase plan

`docs/adr/ADR-020-payments-architecture.md`: **Proposed**, not Accepted —
by design. It will not become Accepted until Section 6's items are
resolved; this report does not treat writing the ADR as equivalent to
ratifying it.

**Proposed implementation phases** (none started; each gated on Section 6
being resolved for the decisions it depends on):

1. **Phase 5B — Scope ratification & design decisions.** Pure
   decision-making, no code. Exit criterion: Section 6 Items 1–3 resolved;
   `ADR-020` updated from Proposed to Accepted with its "AWAITING OWNER
   DECISION" sections replaced by the owner's actual decisions.
2. **Phase 5C — Payment Domain & schema.** `Payment` aggregate, migration,
   repository interface + implementation, `Session`-rate-capture (Item 4,
   if approved) — mirrors the `AvailabilitySlot`/`Session` build pattern
   exactly, no gateway/HTTP concern yet. Exit criterion: Domain/
   Infrastructure/Web (persistence-only) tests green; migration applied
   and verified against real Postgres (`tutorflow_test` at minimum,
   `tutorflow_dev` per this repo's own established practice); audit-trail
   `switch` extended per Item 6.
3. **Phase 5D — `IPaymentGateway` port, `MockPaymentGateway`, and the
   slot-reservation mechanics.** The port, the mock, the Development-only
   DI guard and its proving test, and whichever slot-reservation design
   Item 2 selects (e.g., `Reopen()` + filtered index, if Option C). Exit
   criterion: full initiate → verify → paid/failed/expired flow proven
   end-to-end against the mock via real HTTP endpoint tests; idempotency
   proven by a duplicate-callback test; slot correctly releases or stays
   blocked per the decided design, proven by test.
4. **Phase 5E — Presentation (frontend) for the payment flow.** Booking
   redirects to the (mock) gateway; a return/callback page triggers
   verification; Tutor/Student/Admin can see a Session's payment status.
   Exit criterion: `docs/MANUAL-SMOKE-TEST.md` updated with a payment
   walkthrough against the mock gateway, verified manually in a browser
   end to end, per this repo's own established phase-closing practice.
5. **Phase 5F — Real PSP integration.** Only after a gateway account
   exists (owner Context Item 1's own stated precondition). A new
   `Infrastructure/Payments/` adapter implementing `IPaymentGateway` for
   the chosen provider. Exit criterion, and the actual test of whether
   this design succeeded: **zero Domain/Application diff** from Phase 5D.
6. **Phase 5G (v2, not scheduled) — Payouts & commission.** Explicitly
   deferred; begins only after its own separate, future owner scope
   decision, per `ADR-020`'s Non-Goals. Not part of this plan's exit
   criteria.

**`scripts/verify.ps1` was not run this phase.** No code exists to build
or test — running it would have produced the same 9/9 result as the last
recorded run (Phase 4.5) with zero information value, so it was skipped
rather than run as a formality. It will matter again starting Phase 5C.

**Merge recommendation:** safe to merge `ADR-020` and this report to
`main` as documentation — they authorize nothing by themselves (Section 1's
Governance Note). The one action item for the owner before any further
payments work: resolve Section 6.

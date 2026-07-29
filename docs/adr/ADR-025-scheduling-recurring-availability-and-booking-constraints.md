**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001` through `ADR-024` (all Accepted except `ADR-003`, `ADR-020`, and `ADR-021`, which remain themselves Proposed). This ADR is a **design proposal, not an implementation authorization.** It responds to a "Scheduling & Booking Foundation" phase request whose scope — weekly recurring Tutor availability, date-specific overrides/blocked dates, a working-hours concept, buffer time between lessons, and minimum/maximum booking notice — is either explicitly out of v1 scope (`PRODUCT_REQUIREMENTS.md` SCH-4) or an unresolved Open Question (`DOMAIN_MODEL.md` Open Question 8) in every governing document. This ADR does not amend any of those documents and does not itself authorize writing implementation code — see Governance Note and Questions Requiring Approval. The month-calendar UX and the same-Tutor time-range overlap guard from that same phase request were **not** gated behind this ADR — they were built directly (Phase 8a), since neither touches an open product question or an Accepted-document conflict; only the concepts below do.

---

# ADR-025: Scheduling — Recurring Availability, Buffer Time & Booking Notice Constraints

## Status

**Partially Accepted — 2026-07-29.** Questions 7, 8, and 9 (minimum booking notice, maximum booking horizon, and reschedule enforcement) are **Accepted** — see the Addendum below for the owner's actual decision and the implementation it authorizes. Every other proposal in this ADR (recurring availability, blocked dates, "working hours," buffer time — Questions 1–6) remains **Proposed**; the owner can still evaluate and either accept, amend, or reject each of those independently before any implementation phase touching them begins, exactly as this ADR's own Recommendation/Next Steps section anticipated ("the owner may accept... the notice/horizon proposal... without yet deciding on recurring availability at all").

## Governance Note (read first)

This ADR surfaces the same recurring-availability tension `ADR-021` already flagged, plus three additional concepts that are simply absent from every governing document (not merely unresolved — never mentioned at all):

1. **A recurring weekly availability *rule* sits directly on top of `PRODUCT_REQUIREMENTS.md` SCH-4** (Accepted, Must priority): *"Recurring/standing bookings are out of scope for v1. Only discrete, single-session bookings are supported... no option to create a recurring series is presented."* `ADR-021` (Proposed, not Accepted) already identified this exact ambiguity for its own "Choose Weekly Schedule" checkout concept and left it as its own unresolved Question 2. This ADR restates the same tension from the *supply side* (a Tutor authoring a recurring availability pattern) rather than `ADR-021`'s *demand side* (a Student purchasing a recurring booking) — the two are related but not identical, and neither is resolved by the other.
2. **Date-specific overrides / blocked dates and a "working hours" concept are not mentioned anywhere** — not in `PRODUCT_REQUIREMENTS.md`, not in `DOMAIN_MODEL.md`, not in any Accepted ADR, not even as an Open Question. They are pure silence, and both are only meaningful once (1) above is resolved — there is nothing to "override" or "generate from" without a base recurring rule existing first.
3. **Buffer time between lessons is not mentioned anywhere**, not even as an Open Question — the same pure-silence status as (2).
4. **Minimum/maximum booking notice is an explicitly unresolved Open Question**, not silence: `DOMAIN_MODEL.md` Open Question 8 / `PRODUCT_REQUIREMENTS.md` Section 10.2 Item 8, confirmed still open as of the most recent phase reports (`docs/phases/PHASE-046-REPORT.md`, `PHASE-047-REPORT.md`). `AvailabilitySlot.Book()`, `AvailabilitySlot.Reopen()`, and `Session.Reschedule()` each carry an explicit in-code comment stating they deliberately do not guard against a past or too-near start time, precisely because no such rule exists yet.

**Before any implementation phase referenced in this ADR may begin, the owner must resolve the Questions Requiring Approval below.** None may be silently decided by implementation. Questions 7–9 (booking notice/horizon) were resolved this way — see the Addendum — and their implementation is authorized. Questions 1–6 remain unresolved and remain off-limits to implementation.

## Context

Phase 8a ("Scheduling & Booking Foundation," 2026-07-29) audited the existing Scheduling bounded context and found it unchanged from the shape `ADR-002`/`ADR-014`/`ADR-015`/`ADR-021` already established: a Tutor declares one concrete `AvailabilitySlot` at a time (`AvailabilitySlot.Declare`), a Student books it (`AvailabilitySlot.Book`), and — as of Phase 8a itself — two *different* declared slots for the same Tutor may no longer overlap in time (a Domain guard added directly, since it enforces the Constitution's own already-Accepted "scheduling integrity is non-negotiable" principle rather than inventing new product behavior). Phase 8a also built a real month-calendar UX for both the Tutor's availability view and the Student's booking flow, entirely on top of that existing, unchanged data model.

What Phase 8a explicitly did **not** build — because doing so would mean inventing product decisions this repository's own governance rules forbid — is everything a Tutor would recognize from a mature marketplace's scheduling tools: a recurring weekly pattern instead of one-slot-at-a-time authoring, a way to block out a specific date without deleting individual slots, a "working hours" template distinct from concrete slots, buffer time between back-to-back lessons, and a floor/ceiling on how soon or how far ahead a booking can be made. This ADR is where those get designed for review, not built.

## Problem Statement

Given a request for Tutor-side recurring availability authoring, date overrides, buffer time, and booking-notice constraints — while `AvailabilitySlot`/`Session` remain two ratified, unchanged aggregate roots (`ADR-015`), while `PRODUCT_REQUIREMENTS.md` SCH-4 forbids recurring bookings and `ADR-021` already left an adjacent version of this exact question open, and while a minimum/maximum lead time has been an acknowledged Open Question since `DOMAIN_MODEL.md` was first written — what Domain shape, validation placement, and default policy would each of these concepts require, such that the owner can evaluate and either accept or amend them before any code is written?

## Proposal — Recurring Weekly Availability

**Option A (recommended): a Tutor-side authoring convenience only, materializing into ordinary discrete `AvailabilitySlot` rows — no new recurring Domain concept.** A Tutor defines a pattern (e.g. "Tuesdays & Thursdays, 17:00–18:00, repeat for the next 8 weeks"); the Application layer resolves that pattern into N individual calls to the existing `AvailabilitySlot.Declare(...)` — including Phase 8a's own overlap guard, applied identically to each generated slot — rather than introducing any new "recurring rule" aggregate or event. Once created, each resulting slot is indistinguishable from one declared by hand: no new field, no new state, nothing for a Student's booking flow to treat differently. This mirrors `ADR-021`'s own "Cross-Context Integration" resolution for its adjacent problem (resolving a weekly pattern against pre-declared individual slots, never inventing a new recurring-availability Domain concept) — the same reasoning applied to the supply side instead of the demand side. Because the *only* persisted fact is N ordinary `AvailabilitySlot` rows, this option does not, on its own, create anything resembling a "recurring booking" from SCH-4's perspective — no Student ever sees or purchases a series; they still book one already-concrete slot at a time, exactly as today.

**Option B: a first-class `RecurrenceRule` aggregate that *generates* slots lazily and can itself be edited/cancelled**, changing every future not-yet-materialized occurrence at once (e.g. "cancel all Tuesdays from here on"). This is a materially larger Domain change — a new aggregate, its own bounded-context placement question (mirroring `ADR-021`'s own "new bounded context" governance step for Enrollment & Billing), and a real question of what happens to a `Session` already booked against a since-edited/cancelled occurrence. Not recommended as a first step, but named here as the option to reach for if the owner wants true "edit the whole series" semantics rather than Option A's one-time generation.

**Interaction with SCH-4:** Option A is recommended specifically because it keeps the *booking* side completely unchanged — a Student still books one discrete, already-existing slot, with no series concept ever exposed to them. Whether that fully satisfies SCH-4's intent, or whether SCH-4 itself needs revisiting now that two separate ADRs (this one and `ADR-021`) both lean on the same "materialize into discrete slots" pattern to route around it, is not this ADR's call — see Questions Requiring Approval.

## Proposal — Date-Specific Overrides / Blocked Dates

Meaningful only once a recurring rule (above) exists to override. Two shapes, not mutually exclusive:

- **Skip/cancel a single future occurrence** of a recurring pattern (e.g. "not teaching this Thursday") — under Option A above, this is simply deleting or never-generating the one `AvailabilitySlot` row for that date. Since this API currently has **no delete/cancel-availability capability at all** (noted independently in `docs/design/PREPLY_UX_GAP_ANALYSIS.md` and confirmed by Phase 8a's own audit), this proposal implies that capability must be designed too — itself a new mutation on `AvailabilitySlot` (`Cancel`/`Withdraw`, guarded so a Tutor can never withdraw an already-consumed slot out from under a booked Student) that does not exist today and is not authorized by any Accepted document yet.
- **Block out a whole date** (a Tutor-wide "not available this day," independent of any specific slot) — this is a different concept from the above: not a fact about one `AvailabilitySlot`, but a fact about a date that should suppress generation (under Option A, simply skip that date when materializing the pattern) and/or visually mark the date as unavailable in the calendar UI Phase 8a already built (a presentation-only change, since `MonthCalendarGrid` already accepts arbitrary per-day meaning through its `renderDay` callback with no modification needed).

## Proposal — "Working Hours" vs. the Current Discrete-Slot Model

**Recommended: do not introduce a separate "working hours" concept — Option A's recurring-authoring convenience (above) already covers the same need without a second, competing representation of a Tutor's availability.** A "working hours" template (e.g. "Mon–Fri, 09:00–17:00") that is distinct from concrete slots would mean two representations of the same fact could disagree (the template says available, no slot exists yet, or vice versa) — exactly the kind of "no role-specific copies that can drift" problem `PROJECT_CONSTITUTION.md`'s Architecture Principle 2 (Single source of truth for schedule state) already forbids. If the owner wants a "working hours" *label* purely as a UI convenience for authoring a recurring pattern (Option A), that's a Presentation-layer framing of the same generation step, not a new Domain concept — worth clarifying in the Questions below so this doesn't get built twice under two different names.

## Proposal — Buffer Time Between Lessons

**Recommended shape: a new optional `BufferTime` (a `TimeSpan`, reusing the existing `SessionDuration`-style value-object pattern rather than a raw primitive) recorded per Tutor, not per slot** — a Tutor's general preference ("always leave 15 minutes between lessons"), applied uniformly when generating or validating slots, rather than a per-slot field every declaration would need to repeat. Enforcement point: Phase 8a's own overlap guard on `AvailabilitySlot.Declare` is the natural place to extend — instead of only rejecting a strict time-range overlap, it would also reject a new slot starting less than `BufferTime` after an existing one ends (or ending less than `BufferTime` before an existing one starts) for the same Tutor. This is a straightforward extension of an already-Accepted-in-spirit invariant (Phase 8a's own guard), not a new invariant family — but the actual *default value* (zero, i.e. opt-in only, vs. some non-zero platform default) is a real product decision this ADR does not make.

**Not recommended:** a per-slot buffer field — it would let a Tutor set inconsistent buffers across their own schedule with no clear meaning for the resulting gaps, and every generation path (Option A above) would need to remember to copy it forward.

## Proposal — Minimum / Maximum Booking Notice

Resolves `DOMAIN_MODEL.md` Open Question 8. **Recommended shape: two new optional platform-level (Admin-configured) values, not per-Tutor** — `MinimumBookingNoticeMinutes` and `MaximumBookingHorizonDays` — since a per-Tutor version of either is a materially larger design (another field every Tutor profile screen and every generation/validation path must account for) with no signal in any governing document that per-Tutor variation was ever intended.

**Enforcement point:** `BookSessionCommandHandler`, at the moment of booking (not at `AvailabilitySlot.Declare`, which stays "am I even a valid slot" only) — checked against the slot's own `StartTimeUtc` relative to "now" at the instant of booking. This is deliberately **not** proposed as a guard inside `AvailabilitySlot.Book()` itself: unlike Phase 8a's overlap guard (a fact fully knowable from the slot's own data plus its Tutor's other slots), a notice window is a booking-time policy check against the current instant, which fits the existing `BookSessionCommandHandler`'s own validation step more naturally than a new Domain-aggregate dependency on wall-clock time.

**Not decided here — the actual numbers.** This ADR proposes the *mechanism*, not a default minimum-notice or maximum-horizon value; recommending an arbitrary number (e.g. "2 hours," "90 days") would be exactly the kind of invented business rule this ADR exists to avoid. The owner must supply both figures, or explicitly decide "no floor" / "no ceiling" for either.

**Interaction with `Reopen`/`Reschedule`:** both already carry an explicit comment noting the same absence of a lead-time rule; once this ADR's numbers are set, the same check should apply uniformly to a reschedule's *new* time, not only an initial booking — otherwise a notice window could be trivially bypassed by rescheduling instead of booking fresh.

## Interaction With Phase 8a's Existing Work

Nothing in this ADR revises Phase 8a's overlap guard or the month-calendar UX — both are already-shipped, unconditional (buffer time, above, is proposed as an *extension* of the overlap guard, not a replacement of it) and were not gated behind this ADR because neither touches an open product question. Any implementation that follows from this ADR's eventual acceptance should extend `AvailabilitySlot.Declare`'s existing guard clause and `MonthCalendarGrid`'s existing `renderDay` callback (for blocked-date/buffer visualization), not replace either.

## CQRS / API / Frontend Implications (design-level — no code written)

**Illustrative only, not a committed contract, per `ADR-021`'s own convention for this section:**

- New commands (if Option A + a delete capability are both accepted): `DeclareRecurringAvailabilityCommand` (Tutor-facing, generates N `AvailabilitySlot`s via the existing `Declare` path), `WithdrawAvailabilitySlotCommand` (new — guarded against withdrawing an already-consumed slot), `BlockDateCommand`/`UnblockDateCommand` (Tutor-facing, if the whole-date-block shape is accepted). New Admin-configuration commands for `MinimumBookingNoticeMinutes`/`MaximumBookingHorizonDays`, if platform-level and Admin-editable is accepted over a fixed configuration value.
- New/changed endpoints, each requiring its own non-Open row in `docs/api/AUTHORIZATION_MATRIX.md` before being considered done, restating `CLAUDE.md`'s own Definition of Done, not a new rule: `POST /availability-slots/recurring`, `DELETE /availability-slots/{id}` (or `POST .../withdraw`), `POST/DELETE /tutors/{id}/blocked-dates`, `PATCH /scheduling-settings` (Admin, notice/horizon values).
- Frontend: a recurring-authoring form alongside (not replacing) `AddTeachingTimeDialog`; blocked dates rendered via `MonthCalendarGrid`'s existing `renderDay` callback (no change to that component itself, per Phase 8a's own domain-agnostic-reusability design goal); a notice/horizon-aware disabled state on `BookSessionPage`'s calendar day cells, extending the same "disable a day with zero effectively-bookable slots" pattern Phase 8a already built, once the actual numbers exist to compute it from.

## Testing Strategy (outline — no tests written)

Mirrors the three-layer discipline every prior ADR-then-implemented phase in this repository has followed: a Domain test per new invariant (recurring generation produces N valid, non-overlapping, buffer-respecting slots; a booking-notice check rejects a too-soon or too-far booking at the exact boundary, accepts one minute inside it); an Application test per new handler's authorization/orchestration; a Web test proving persistence (or rejection, with no partial write) by re-reading from a fresh `DbContext`, per `CLAUDE.md`'s own non-negotiable rule.

## Consequences

**Becomes possible, if accepted:** a Tutor-side authoring experience closer to what was originally requested, without contradicting SCH-4's booking-side restriction; a genuine floor/ceiling on booking timing, closing a five-Open-Question-report-old gap; buffer time as a real, enforced Tutor preference.

**Becomes harder / newly constrained:** every slot-generation path must now also honor buffer time and (once numbers exist) the notice/horizon window, not just the overlap guard; a new delete/withdraw capability on `AvailabilitySlot` needs its own careful guard (never allow withdrawing a consumed slot) that doesn't exist anywhere in this codebase today.

**Now forbidden (restated, not new):** no implementation code under cover of this ADR for Questions 1–6 (recurring availability, blocked dates, working hours, buffer time — no invented default numbers for buffer time, since Question 6 remains open); no new recurring-*booking* capability exposed to a Student (Option A's entire point is that the Student-facing contract is unchanged). Booking notice/horizon (Questions 7–9) are no longer in this forbidden set — see the Addendum for the owner-supplied numbers this ADR itself declined to invent.

## Supersedes / Relates To

- **Relates to, does not resolve,** `ADR-021`'s own unresolved Question 2 (recurring bookings vs. SCH-4) — this ADR's Option A is the same "materialize into discrete slots" pattern applied to the supply side; accepting one does not automatically resolve the other, since they address different actors (Tutor authoring vs. Student purchasing).
- **Extends, does not revise,** Phase 8a's `AvailabilitySlot.Declare` overlap guard (buffer time is a widened version of the same check) and `MonthCalendarGrid` (blocked-date/buffer visualization uses its existing, unmodified `renderDay` callback).
- **Resolves, pending acceptance,** `DOMAIN_MODEL.md` Open Question 8 (minimum/maximum booking lead time).
- **Restates, does not alter,** `PROJECT_CONSTITUTION.md`'s Architecture Principle 2 (single source of truth for schedule state) — the reasoning behind rejecting a separate "working hours" representation.

## Non-Goals

Does not implement any code. Does not choose actual buffer-time, minimum-notice, or maximum-horizon default values. Does not design Option B's `RecurrenceRule` aggregate in full (named only as an alternative). Does not resolve `ADR-021`'s own Question 2. Does not revisit SCH-4 itself — only the owner can decide whether it needs revisiting.

## Questions Requiring Approval

1. **Recurring availability shape.** Option A (authoring convenience, materializes into ordinary discrete slots) or Option B (a first-class, independently-editable `RecurrenceRule` aggregate)?
2. **SCH-4 interaction.** Does Option A's "Student still books one already-concrete slot, no series ever exposed" design satisfy SCH-4's intent, or does SCH-4 itself need revisiting given this ADR and `ADR-021` both lean on the same pattern to route around it?
3. **Slot withdrawal.** Should a Tutor be able to delete/withdraw an unconsumed `AvailabilitySlot` (a capability that does not exist anywhere in this API today), and if so, is "never allow withdrawing an already-consumed slot" the only guard, or are there others (e.g. a minimum-notice floor on withdrawal too)?
4. **Blocked dates.** Skip-a-single-occurrence, block-a-whole-date, or both?
5. **"Working hours" framing.** Confirm: no new Domain concept, purely a UI label over Option A's generation step — or does the owner want an actual, independently-stored template distinct from concrete slots (contrary to this ADR's recommendation)?
6. **Buffer time default.** Per-Tutor optional value as recommended (vs. per-slot); what is the platform default (zero/opt-in, or some non-zero value) if the Tutor never sets one?
7. ~~**Minimum booking notice.** Is there a floor at all, and if so, what is it?~~ **Resolved 2026-07-29:** yes — 24 hours before the slot's own start time. See Addendum.
8. ~~**Maximum booking horizon.** Is there a ceiling at all, and if so, what is it? Platform-wide or per-Tutor?~~ **Resolved 2026-07-29:** yes — 90 days ahead, platform-wide (not per-Tutor, as recommended). See Addendum.
9. ~~**Notice/horizon enforcement scope.** Should the same check apply to `RescheduleSessionCommand`'s new time, or only to initial booking?~~ **Resolved 2026-07-29:** yes, applies identically to both — see Addendum.

## Recommendation / Next Steps

No implementation phase should begin until Questions 1–2 are resolved (they determine whether anything else here can be built at all, and in what shape). Questions 3–5 gate the overrides/working-hours surface specifically. Questions 6–9 gate buffer time and notice/horizon independently of the recurring-availability questions — the owner may accept, say, the notice/horizon proposal (closing a long-open Domain Model question) without yet deciding on recurring availability at all, since the two are independent proposals bundled into one ADR only because they came from the same phase request.

**2026-07-29 update:** this is exactly what happened. Questions 7–9 (notice/horizon, not buffer time — Question 6 remains open) were accepted independently of Questions 1–5; see the Addendum below for the resulting implementation.

## Addendum — Booking Notice & Horizon, 2026-07-29

The owner accepted this ADR's own recommended mechanism and shape (Questions 7–9) and supplied the two numbers this ADR deliberately declined to invent:

- **Minimum booking notice: 24 hours.** A Session may not be booked (or rescheduled into) an Availability Slot whose `StartTimeUtc` is less than 24 hours from the current instant.
- **Maximum booking horizon: 90 days, platform-wide.** A Session may not be booked (or rescheduled into) an Availability Slot whose `StartTimeUtc` is more than 90 days from the current instant. Platform-wide, not per-Tutor, as this ADR recommended — no per-Tutor override exists.
- **Applies identically to reschedule.** `RescheduleSessionCommand`'s check against the *new* slot's `StartTimeUtc` uses the same two constants, closing the bypass-via-reschedule gap this ADR flagged.

**Enforcement point (per this ADR's own recommended shape, Proposal — Minimum/Maximum Booking Notice, above):** `BookSessionCommandHandler` and `RescheduleSessionCommandHandler`, at the moment of booking/rescheduling — never inside `AvailabilitySlot.Declare`/`AvailabilitySlot.Book`/`Session.Reschedule` themselves, since a notice/horizon window is a booking-time policy check against the current instant, not a fact knowable from the slot's own data alone. Both constants are configuration-driven (`SchedulingConstraints:MinimumBookingNoticeHours`, `SchedulingConstraints:MaximumBookingHorizonDays`), not hardcoded literals, so a future Admin-configuration UI (out of scope for this pass) can change them without a code change — the same "config over hardcoding" convention `RateLimitingSettings`/`MeetingProviderSettings` already established.

**Still forbidden:** Questions 1–6 (recurring availability, blocked dates, working hours, buffer time) remain Proposed, not Accepted — no implementation code exists for any of them, and this Addendum authorizes none.

**Resolves:** `DOMAIN_MODEL.md` Open Question 8 and `PRODUCT_REQUIREMENTS.md` Section 10.2 Item 8 — both updated to reflect this decision.

---

*Status: Partially Accepted — 2026-07-29. Questions 7–9 (booking notice/horizon) are Accepted — see Addendum. Questions 1–6 (recurring availability, blocked dates, working hours, buffer time) remain Proposed until the owner resolves them.*

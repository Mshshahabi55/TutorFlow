**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001` through `ADR-018` (all Accepted, except `ADR-003`, which remains itself Proposed). This ADR resolves `DOMAIN_MODEL.md` Open Question 18 ("In what currency or format is a Tutor's Hourly Rate expressed?") and makes concrete the seam `ADR-018`'s Forward Compatibility section named but deliberately left open ("Whether and how to make [currency] concrete is a Task 2/Task 3 finding... not decided further here"). It chooses no payment gateway, implements no payment integration, and introduces no invoice/order concept — this ADR is pricing *representation* only.

---

# ADR-019: Currency and Pricing Representation

## Status

**Accepted — 2026-07-22.**

**Business decision, made explicitly by the Project Director through the Constitution's Decision-Making Process:**

| # | Area | Decision |
|---|---|---|
| 1 | Currency | **Single currency: Iranian Rial (IRR).** No multi-currency, no FX conversion, anywhere in v1. |
| 2 | Storage unit | **Stored in Rial.** Iranian payment gateways take amounts in Rial; storing Rial means the persisted amount equals the eventual gateway amount with no arithmetic at the point of payment, and arithmetic is where money bugs are born. |
| 3 | Display/entry unit | **Displayed and entered in Toman** (1 Toman = 10 Rial) — what Iranian users actually think and speak in. |
| 4 | Minor unit | **Rial has no practical minor unit.** Amounts are whole numbers — scale 0. No fractional Rial exists anywhere in the model. |
| 5 | Forward compatibility | v1 is single-currency, but currency is an **explicit, named fact in the model**, not an unwritten assumption, so a v2 multi-currency capability is an additive change, not a rewrite. |

## Context

`DOMAIN_MODEL.md`'s Hourly Rate value object has carried an open currency/format question since that document's approval: "Hourly Rate — a Tutor's rate; currency/format not specified (see Open Questions)" (line 79), formalized as Open Question 18: "In what currency or format is a Tutor's Hourly Rate expressed? Not specified in the source documents." `PRODUCT_REQUIREMENTS.md` DISC-2 already fixed that the rate is shown to Students/Parents and that no payment is processed on the platform in v1 — this ADR does not reopen that scope exclusion.

`ADR-018` (Accepted, 2026-07-21) named "Single currency — Iranian Rial/Toman. No multi-currency, no FX conversion" as market-scope Decision 4, and its Forward Compatibility section required that "wherever a monetary amount is handled (currently: `HourlyRate`), the concept of 'which currency' must remain a distinct, nameable fact even if v1 only ever populates it with one value — not an assumption baked into formatting or arithmetic with no seam to change it later," explicitly deferring the concrete mechanism to "a Task 2/Task 3 finding." This ADR is that finding.

The as-built `HourlyRate` value object (`backend/src/Domain/Identity/ValueObjects/HourlyRate.cs`) is a bare `decimal Amount`, persisted as `numeric(10,2)` (`TutorConfiguration.cs`). Nothing in the code, schema, or API contract states which currency `Amount` is denominated in, nor whether it is Rial or Toman — a live ambiguity: a stored `500000` is equally readable as 500,000 Rial (50,000 Toman) or 500,000 Toman (5,000,000 Rial), a 10x discrepancy with no field anywhere to resolve it. This phase (Phase 4) exists specifically to remove that ambiguity before any payment-adjacent code is written, per this project's standing practice of deciding architecturally-significant constraints ahead of the feature they gate (`ADR-018`'s own precedent, itself following `ADR-013`/`ADR-014` for persistence and concurrency).

## Decision

Each of the five points is binding, effective immediately:

1. **Currency.** TutorFlow v1 prices exclusively in Iranian Rial (IRR). No other currency is representable, selectable, or convertible to/from, anywhere in the system.
2. **Storage unit: Rial.** Every persisted and API-boundary-crossing monetary amount (`Tutor.HourlyRate` today; any future monetary field) is expressed in Rial. This is deliberate, not incidental: Iranian payment gateways (`ADR-018` Decision 5's PSP integration model, whenever built) accept and settle in Rial, so storing Rial means the persisted figure *is* the gateway figure — no unit conversion is ever needed on the path from "what's stored" to "what's charged," which is exactly the path where an off-by-10 error would be most costly and least visible.
3. **Display/entry unit: Toman.** Every user-facing surface — display and input alike — presents amounts in Toman (1 Toman = 10 Rial), because that is the unit ordinary Iranian users think, speak, and compare prices in; presenting Rial to a user would be as unnatural (and error-inviting) as presenting UTC timestamps with no timezone conversion was for scheduling (`ADR-018` Decision 3, Phase 3's resolution of it).
4. **No minor unit.** Rial amounts are whole numbers. No fractional Rial is valid at any layer — Domain, persistence, or wire contract. The persisted column type changes from a 2-decimal-scale numeric to a scale-0 numeric type (Task 2), and `HourlyRate.Of` rejects a non-integer amount as a Domain-level invariant violation, the same enforcement mechanism already used for "amount must be positive" (`ADR-007`: Domain is the sole and final authority on every business rule).
5. **Forward compatibility via a named fact, not a rewrite seam.** Currency is made an explicit, nameable property of the money concept in code (Task 2 chooses and justifies the concrete mechanism) rather than left as an implicit assumption baked into formatting or arithmetic. v1 populates this fact with exactly one value (IRR) and enforces that no other value can be constructed — consistent with this phase's own prohibition on building a multi-currency abstraction now. A v2 currency addition changes this one, already-named seam; it does not require finding and rewriting every scattered place that assumed Rial.

## Decision Rationale

- **Single currency, no multi-currency abstraction:** `ADR-018` Decision 4 already settled this at the market-scope level; building a currency-selection or FX-conversion capability now, for a single-currency v1 with no payment integration at all, is exactly the premature-generality `PROJECT_CONSTITUTION.md` Architecture Principle 7 forbids — infrastructure for a capability with no approved requirement.
- **Store Rial, not Toman:** the alternative (storing Toman, converting ×10 at the payment boundary whenever that's eventually built) would introduce exactly one more arithmetic step on the critical path where money changes hands — the same reasoning `ADR-018` Decision 2 (storage stays UTC, `Asia/Tehran` is presentation-only) already applied to time: keep the unit that the eventual external system of record (a PSP; for time, the database and API contract) uses, and confine the human-facing unit conversion to the presentation edge.
- **Display/enter in Toman:** mirrors Phase 3's Tehran-time precedent exactly — `frontend/src/shared/time/` is the single conversion seam for UTC↔Tehran; Task 3 of this phase creates the analogous single seam for Rial↔Toman, for the same reason (a unit mismatch between what's stored and what's shown is a correctness risk if conversion is inlined in more than one place).
- **Scale 0, whole Rial only:** Rial's smallest circulating unit was withdrawn from practical use decades before this system was designed; no Iranian PSP quotes or settles in fractional Rial. Modeling a minor unit that does not exist in practice would be inventing precision the domain does not have, the same category of error as `ADR-018`'s prohibition on assuming a whole-hour timezone offset — both are "the real-world unit has a property software must not assume away."
- **Currency as a named fact, not a rewrite seam:** directly satisfies `ADR-018`'s Forward Compatibility requirement, using the lightest mechanism that still makes "this is IRR" true in code rather than in a comment (Task 2 selects and justifies the specific mechanism — a value object versus a constant is a Task 2, not Task 1, decision, since it depends on what already exists in `HourlyRate`).

## Consequences

**Becomes easier:**
- A stored monetary amount is now unambiguous: `numeric` scale-0 Rial, full stop. No reader of the schema, the API contract, or the code needs to guess or ask which unit `500000` means.
- Any future payment integration (`ADR-018` Decision 5, still unimplemented) receives amounts already in the unit Iranian PSPs expect, with zero conversion code to write, test, or get wrong at that boundary.
- Task 3's single frontend conversion module gives every future money-displaying page one place to get Rial↔Toman right, rather than reinventing `× 10` or `÷ 10` inline per page (and risking one of them being backwards).

**Becomes harder / newly constrained:**
- Every existing test or fixture that asserted a fractional `HourlyRate.Amount` (the old `numeric(10,2)` precision) must be corrected, in its own commit, per Task 2 — a fractional Rial is no longer representable at all, by design.
- Any future code touching `HourlyRate` must go through whatever currency-designating mechanism Task 2 introduces; a bare `decimal` can no longer be constructed as a rate without that fact being present.

**Now forbidden:**
- Introducing a second currency, a currency-selection UI, or FX-conversion logic anywhere in v1 (restates `ADR-018` Decision 4 — not a new prohibition, but now cross-referenced from the pricing-representation decision that also governs the shape any future multi-currency work must take).
- Persisting or transmitting a fractional Rial amount at any layer.
- Building any payment integration, invoice, or order concept under cover of this ADR — this ADR is representation only; `ADR-018` Decision 5's payment-integration constraint stands unchanged and unaddressed here.

## Forward Compatibility

Consistent with `ADR-018`'s Forward Compatibility clause, naming Rial as v1's only currency must not make the system currency-*locked*:

- **Currency is a named fact of the money concept** (Task 2's concrete mechanism — see `docs/phases/PHASE-04-REPORT.md` Section 3 for which one was chosen and why), constructible today with exactly one value. Adding a second currency in v2 means adding a second constructible value to this same seam, not introducing the concept for the first time.
- **The Rial↔Toman conversion lives in exactly one place per tier** — `HourlyRate`'s own scale-0 invariant in Domain, and `frontend/src/shared/time/`'s sibling module (Task 3) on the frontend — so a v2 multi-currency UI would extend one conversion module per tier, not hunt for inline `× 10`/`÷ 10` scattered across pages.
- **The wire contract stays the stored unit (Rial)**, not the display unit (Toman) — exactly as `ADR-018` kept the wire contract UTC rather than Tehran-local. This means a future API consumer (a v2 mobile client, a future payment integration) never has to guess which unit a JSON `hourlyRate` field is in; it is always Rial, unconditionally, today and after any future currency addition.

## Supersedes / Relates To

- **Resolves** `DOMAIN_MODEL.md` Open Question 18 (currency/format of Hourly Rate).
- **Makes concrete** the currency seam `ADR-018`'s Forward Compatibility section named and deliberately deferred ("a Task 2/Task 3 finding... not decided further here").
- **Restates, does not alter,** `ADR-018` Decision 4 (single currency, no multi-currency, no FX) — this ADR specifies *how* that decision is represented in the model; it does not revisit *whether* v1 is single-currency.
- **Does not reopen** `PRODUCT_REQUIREMENTS.md` Section 9's exclusion of payments from v1 scope, or DISC-2's "no payment is processed through the platform" — both stand unchanged; this ADR governs representation only.
- **Follows the precedent** `ADR-018`/Phase 3 established for the analogous Tehran-time problem: store in the system-of-record unit, convert at exactly one presentation seam, never assume the conversion inline.
- **Relates to, does not amend,** `DOMAIN_MODEL.md`'s own Hourly Rate text — per this project's established addendum-only convention for correcting an approved document (`ADR-003`'s Second Addendum, Correction 1 precedent), this ADR resolves the open question without editing `DOMAIN_MODEL.md`'s prose directly.

## Non-Goals

Does not implement any payment gateway, invoice, order, or billing concept — `PRODUCT_REQUIREMENTS.md` Section 9's standing exclusion is untouched. Does not introduce multi-currency support, FX conversion, or a currency-selection UI — forbidden by this phase's own rules and by `ADR-018` Decision 4. Does not choose a specific Iranian PSP or payment integration mechanism (`ADR-018` Decision 5 already named the constraint; no PSP decision is made here or by this phase). Does not change any Domain, Application, Infrastructure, or Web source file itself — this ADR is the governance decision Task 1 requires be written *before* any such code changes (Task 2 onward); the concrete mechanism and migration it authorizes are recorded and justified in `docs/phases/PHASE-04-REPORT.md`, not in this document.

---

*Status: Accepted — 2026-07-22. Single currency (Iranian Rial), stored in Rial as whole numbers (no minor unit), displayed and entered in Toman (1 Toman = 10 Rial), currency represented as an explicit named fact for forward compatibility, no payment integration introduced.*

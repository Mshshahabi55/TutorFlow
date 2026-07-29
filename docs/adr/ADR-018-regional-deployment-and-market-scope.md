**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001` through `ADR-017` (all Accepted, including `ADR-003`). This ADR resolves `PRODUCT_REQUIREMENTS.md` Section 10.5's open launch-market question (Item 16) and formalizes a market-scope assumption `ADR-017` already relied on informally in its own Decision Rationale. It chooses no specific hosting provider, no specific PSP vendor, and implements no payment module — those remain future, separately-approved decisions; this ADR records the constraint they must satisfy whenever they are made.

---

# ADR-018: Regional Deployment & Market Scope

## Status

**Accepted — 2026-07-21.**

**Business decision, made explicitly by the Project Director through the Constitution's Decision-Making Process:**

| # | Area | Decision |
|---|---|---|
| 1 | Market | **Iran only** for v1. International users are deferred to v2. |
| 2 | Language | **English only.** No i18n framework, no translation files, no RTL support in v1 (restates and makes explicit the corollary of `PRODUCT_REQUIREMENTS.md` DATA-2, already Accepted). |
| 3 | Timezone | **`Asia/Tehran`, UTC+03:30, no daylight saving since 2022.** |
| 4 | Currency | **Single currency** — Iranian Rial/Toman. No multi-currency, no FX conversion. |
| 5 | Payments | **Constraint recorded only, not implemented.** Whenever payment processing is built (a future decision — payments remain out of scope for v1, `PRODUCT_REQUIREMENTS.md` Section 9), it must use an Iranian PSP under a redirect + callback + server-side-verify model. |
| 6 | Hosting | **Inside Iran.** Specific provider not yet chosen — a separate, future decision. |
| 7 | External dependencies | The system **must not depend on** Stripe, PayPal, Twilio, SendGrid, or any US-hosted cloud SDK, for any capability. |
| 8 | Forward compatibility | v1 is **Iran-first, not Iran-locked.** Currency, timezone conversion, and payment provider must remain replaceable without a rewrite — see Forward Compatibility below. |

## Context

`PRODUCT_REQUIREMENTS.md` Section 10.5 has carried "What is the initial launch/target market for v1?" as an open question since that document's approval (Item 16), alongside the related, still-open "Are there jurisdiction-specific regulatory requirements beyond general GDPR-equivalent compliance, once a launch market is designated?" (Item 19, partially addressed here, not fully resolved — see Non-Goals). `ADR-017` (Accepted, 2026-07-20) already reasoned about a specific market informally, in its own Decision Rationale: "The target launch market (Iran) carries material reliability risk for both international OAuth/OIDC identity providers and, to a lesser extent, cross-border SMS/email delivery — a risk that matters far less for Password [authentication]." That reasoning was directionally correct but was never itself ratified as a binding scope decision — it was an assumption smuggled into a different ADR's rationale, exactly the kind of implicit decision-through-implementation this project's own governance discipline (`ADR-011`'s revert of "Phase 16") exists to prevent. This ADR closes that gap: it makes the market assumption explicit, binding, and traceable, rather than leaving it as an unstated premise other decisions quietly depended on.

`PRODUCT_REQUIREMENTS.md` DATA-3 already requires that "the product must be designed so that future international expansion does not require re-architecting the data or compliance model" (Should priority). This ADR's Decision point 8 and its Forward Compatibility clause are the direct architectural response to that requirement, now that a concrete v1 market is named — DATA-3 could not be fully honored while the market itself remained an open question, since "does not require re-architecture" cannot be verified against an unnamed target.

`DOMAIN_MODEL.md`'s Hourly Rate value object already flagged its own currency and format as unspecified ("currency/format not specified — see Open Questions"), and `PRODUCT_REQUIREMENTS.md` DISC-2 already fixed that "no payment is processed within this domain" for v1 — this ADR does not reopen or reverse that scope exclusion (payments remain unimplemented in v1); it records the constraint a future payment integration must satisfy, consistent with the Constitution's general practice of deciding architecturally-significant constraints ahead of the feature they gate (the same pattern `ADR-013`/`ADR-014` used for persistence and concurrency before any repository code existed).

## Decision

Each of the eight points is binding, effective immediately:

1. **Market.** TutorFlow v1 serves users located in Iran only. No functionality, copy, or design decision may assume or require a non-Iranian user base for v1. International users and multi-market operation are explicitly deferred to v2.
2. **Language.** English is the sole UI language for v1. No internationalization/localization framework is introduced, no translation resource files exist, and no right-to-left layout support is built. This is not a new decision — it restates the binding consequence of `PRODUCT_REQUIREMENTS.md` DATA-2 ("All product UI, content, and documentation are in English for v1"), which this ADR does not alter.
3. **Timezone.** All scheduling-facing business logic must treat `Asia/Tehran` (UTC+03:30, no daylight saving since Iran discontinued DST in 2022) as the operating timezone for display and user-facing time interpretation. Internally, every persisted and API-boundary-crossing timestamp remains UTC (unchanged from existing practice — see Task 2 audit in `docs/phases/PHASE-005-REPORT.md` for current-state verification); `Asia/Tehran` is a presentation/interpretation concern layered on top of that, never a storage format. Any arithmetic that assumes a whole-hour UTC offset is incorrect for this market and is forbidden.
4. **Currency.** TutorFlow v1 operates in a single currency (Iranian Rial/Toman). No multi-currency selection, storage, or FX conversion logic exists anywhere in the system.
5. **Payments.** Not implemented in v1 — `PRODUCT_REQUIREMENTS.md` Section 9's exclusion of "Payments, billing, and payouts" stands unchanged. This decision records the constraint binding on whichever future phase implements payments: it must integrate an Iranian Payment Service Provider (PSP) using a redirect-based flow (the caller is redirected to the PSP, redirected back with a callback, and the payment's success is confirmed by a server-side verification call to the PSP — never trusted from the client-side redirect alone). No payment module, SDK, or dependency is added by this ADR.
6. **Hosting.** Production hosting is inside Iran. The specific hosting provider is not chosen by this ADR and remains a separate, future infrastructure decision.
7. **External dependencies.** No component of the system may take on a dependency — package, SDK, API integration, or third-party service call — on Stripe, PayPal, Twilio, SendGrid, or any other US-hosted cloud service, for any capability (payments, SMS, email, or otherwise). This is a standing constraint on all future dependency additions, not only payment-related ones.
8. **Forward compatibility.** v1 being Iran-first must not become Iran-locked by construction. See Forward Compatibility, below, for exactly which seams this requires staying abstract.

## Decision Rationale

- **Iran-only market, named explicitly:** removes an assumption that was previously implicit in `ADR-017`'s reasoning and nowhere else; every future decision that depends on "who is the user" now has an explicit, binding answer to point to instead of re-deriving or re-guessing it.
- **English-only, no i18n:** `PRODUCT_REQUIREMENTS.md` DATA-2 already settled this; introducing an i18n framework now, for a single-language v1, would be exactly the kind of premature-generality Architecture Principle 7 warns against — infrastructure for a capability (multi-language) that has no approved requirement yet.
- **`Asia/Tehran`, UTC+03:30, no DST:** stated explicitly because it is the single highest-risk fact for a scheduling application entering this market — a half-hour offset, and the absence of DST since 2022, are both easy to get wrong if assumed rather than stated, and getting them wrong directly threatens CONST-1 (no double-booking) and CONST-3 (one non-contradictory schedule view) if any component's arithmetic silently assumes a different offset than another's.
- **Single currency:** `DOMAIN_MODEL.md`'s own Hourly Rate value object already flagged currency/format as unspecified; naming Rial/Toman now removes that ambiguity without expanding scope, since no payment processing occurs in v1 regardless of which currency is named.
- **Payment constraint recorded, not built:** avoids the trap of building payment integration speculatively (forbidden by this phase's own rules and by `PRODUCT_REQUIREMENTS.md` Section 9) while still ensuring nothing else built in the meantime — pricing display, hourly rate precision, session cost fields, if any are added later — accidentally assumes a payment model incompatible with the one eventually required.
- **Hosting inside Iran, no US-hosted dependencies:** a direct, mechanical consequence of point 1 — a system whose entire user base is inside Iran gains nothing from, and takes on unnecessary external risk from, a dependency that may be unreachable or unreliable from that market.
- **Forward compatibility over Iran-locking:** consistent with `PRODUCT_REQUIREMENTS.md` DATA-3 (already Accepted, Should priority) and this project's Architecture Principle 7 (evolvability over premature generality) — the correct balance is naming the v1 constraint precisely while keeping the seams that would need to change for v2 identifiable and narrow, not hardcoding the v1 assumption everywhere it happens to be convenient.

## Consequences

**Becomes easier:**
- Every future decision involving locale, currency, timezone, or third-party service selection has one document to check first, instead of re-deriving market assumptions from scattered context.
- `PRODUCT_REQUIREMENTS.md` Section 10.5 Item 16 (target market) is resolved; Item 19 (jurisdiction-specific regulatory requirements) is partially informed but not fully resolved — see Non-Goals.
- Dependency-addition review has a concrete, checkable rule (`CLAUDE.md`: "Never add a dependency on a service unreachable from Iran") rather than a vague "keep it reasonable" standard.

**Becomes harder / newly constrained:**
- Any future package addition involving payments, SMS, email delivery, or a cloud SDK now requires an explicit check against point 7 before it can be added — this is a deliberate friction, not an oversight.
- A future v2 international-expansion effort must go through the seams named in Forward Compatibility below, rather than being free to assume the current single-timezone, single-currency shape holds indefinitely.

**Now forbidden:**
- Adding any i18n/localization framework, translation resource files, or RTL layout support in v1 (point 2).
- Adding a dependency on Stripe, PayPal, Twilio, SendGrid, or any other US-hosted cloud SDK, for any purpose (point 7).
- Introducing multi-currency logic, FX conversion, or a currency-selection UI in v1 (point 4).
- Implementing a payment module in v1 at all (restates `PRODUCT_REQUIREMENTS.md` Section 9 — not a new prohibition, but now cross-referenced from the market-scope decision that also governs the shape any future payment work must take).

## Forward Compatibility

For v1 to remain Iran-first rather than Iran-locked, the following seams must stay abstract — expressed as an interface, a single point of configuration, or an isolated module — rather than being hardcoded inline wherever they are used:

- **Currency.** Wherever a monetary amount is handled (currently: `HourlyRate`), the concept of "which currency" must remain a distinct, nameable fact even if v1 only ever populates it with one value — not an assumption baked into formatting or arithmetic with no seam to change it later. Whether and how to make this concrete is a Task 2/Task 3 finding, addressed in `docs/phases/PHASE-005-REPORT.md`, not decided further here.
- **Timezone conversion.** The UTC-storage / local-interpretation boundary (point 3) must be a single, identifiable seam — not `Asia/Tehran`'s offset assumed or computed ad hoc in more than one place. Where this currently stands is a Task 2 audit finding, not re-litigated here.
- **Payment provider.** Whenever payments are eventually built, the redirect + callback + server-side-verify model (point 5) must be implemented behind an interface that could plausibly be satisfied by a different PSP later, consistent with this codebase's existing dependency-inversion discipline (`ADR-005`) — not a payment-module decision made now, since no payment module exists yet.
- **Notification provider.** No notification/messaging capability exists in this domain today (`PROJECT_CONSTITUTION.md`: Project Scope excludes in-platform communication) and none is introduced by this ADR. This seam is named for completeness, matching the four seams the owner's decision explicitly called out — if a notification capability is ever approved, it must not hardcode a specific provider inline, for the same reason payments must not.

None of these four seams requires action under this ADR — Task 3 of the phase this ADR belongs to determines whether any code currently violates them, and if so, whether fixing it is authorized now or must wait for explicit owner approval as a separate work item.

## Supersedes / Relates To

- **Resolves** `PRODUCT_REQUIREMENTS.md` Section 10.5, Open Question 16 (initial launch/target market).
- **Partially informs, does not fully resolve** `PRODUCT_REQUIREMENTS.md` Section 10.5, Open Question 19 (jurisdiction-specific regulatory requirements) — naming Iran as the market is a prerequisite to answering that question, but this ADR does not itself enumerate Iran-specific regulatory requirements.
- **Formalizes an assumption `ADR-017` already relied on informally** in its Decision Rationale (Password over OAuth/passwordless, reasoned from Iran-market delivery-channel risk). Does not alter any decision `ADR-017` itself made.
- **Directly implements** `PRODUCT_REQUIREMENTS.md` DATA-3 ("future international expansion does not require re-architecting the data or compliance model") now that a concrete v1 market exists to design against.
- **Does not reopen** `PRODUCT_REQUIREMENTS.md` Section 9's exclusion of Payments from v1 scope, or DISC-2's "no payment processing occurs on the platform." Both stand unchanged.
- **Does not alter** `ADR-013` (PostgreSQL) or `ADR-014` (unique-constraint concurrency) — hosting location (point 6) is an operational/infrastructure decision, not a persistence-technology one, and remains compatible with both.
- **Relates to, does not supersede,** `DOMAIN_MODEL.md`'s Hourly Rate Open Question ("currency/format not specified") — this ADR names the currency; it does not amend `DOMAIN_MODEL.md`'s own text (per this project's established addendum-only convention for correcting an approved document — see `ADR-003`'s Second Addendum, Correction 1 for precedent).

## Non-Goals

Does not choose a specific hosting provider (point 6 names the constraint — inside Iran — not the vendor). Does not choose a specific Iranian PSP (point 5 names the integration model, not a vendor). Does not implement any payment module, i18n framework, or new package — forbidden by this phase's own rules and, independently, by `PRODUCT_REQUIREMENTS.md` Section 9's standing exclusion. Does not fully resolve jurisdiction-specific regulatory requirements (`PRODUCT_REQUIREMENTS.md` Section 10.5, Open Question 19) — only names the market those requirements would apply to. Does not change any Domain, Application, Infrastructure, or Web source file — this ADR is a governance decision; any source changes it motivates are separately authorized (or not) in `docs/phases/PHASE-005-REPORT.md`.

---

*Status: Accepted — 2026-07-21. Market: Iran-only for v1, English-only, `Asia/Tehran` (UTC+03:30, no DST), single currency (Rial/Toman), no payment module in v1, hosting inside Iran, no US-hosted external dependencies, Iran-first not Iran-locked.*

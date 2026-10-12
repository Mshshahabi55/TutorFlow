# Product Vision

## What TutorFlow is

TutorFlow is a two-sided online tutoring marketplace for the European market, connecting students with independent tutors for 1:1 (and potentially group, TBD) lessons across academic and language subjects. The target product experience is comparable in scope and sophistication to Preply: tutor discovery, trial lessons, recurring bookings, subscriptions, an in-product classroom, messaging, reviews, and tutor earnings/payouts.

**Explicitly out of scope for this document:** literal Preply branding, copy, visual design, or source code. "Comparable" means capability and information-architecture parity, not a clone.

## Target market

European market, multi-currency (EUR primary, others TBD), multi-timezone, multi-language UI (scope TBD — see Open Questions). This has concrete architectural consequences documented in `docs/domain/DOMAIN_MODEL.md` (Money as integer-minor-units + ISO currency, UTC-stored timestamps with separate timezone metadata) and legal consequences flagged below.

## Personas

| Persona | Summary | Primary surfaces |
|---|---|---|
| **Student** | Wants to find a suitable tutor quickly, trial them cheaply/risk-free, then commit to recurring lessons or a subscription. Price- and trust-sensitive; cares about tutor responsiveness, reviews, and schedule fit. | Public site, Student app |
| **Tutor** | Independent contractor monetizing their teaching time. Cares about profile visibility (search ranking), booking friction, payment reliability, and payout speed. Onboarding must build trust (verification) without being so long it causes drop-off. | Public site ("Become a Tutor"), Tutor app |
| **Admin / Operations** | Internal staff moderating tutor applications, handling disputes/refunds, managing taxonomy (subjects/languages/specialties), and monitoring marketplace health. | Admin |
| **Guest / Anonymous visitor** | Not yet a student or tutor; browsing the public site, searching tutors, deciding whether to sign up. | Public site |

A given person may hold both Student and Tutor roles on the same account. **Resolved**: dual-role (same login, switch hats) is in scope for v1 — a single `User` may carry both role grants simultaneously, independent of each other (`ADR-0002`).

## Success shape (qualitative, not metrics — metrics are a PO decision)

A student should be able to go from "I don't have a tutor" to "I just finished a paid trial lesson" without friction, confusion, or a payment that silently fails. A tutor should be able to go from "I applied" to "I have my first booking" without the onboarding wizard feeling like a wall. Both sides should trust that money (payment, refund, payout) behaves correctly — this is why the trial-booking and payment workflows get dedicated, non-CRUD treatment in this spec (`docs/product/TRIAL_BOOKING_WORKFLOW.md`, `docs/domain/STATE_MACHINES.md`).

## Explicit non-decisions in this document

The following are named in the originating brief but intentionally left undecided here, pending Product Owner / Legal input — see `docs/OPEN_QUESTIONS.md` for the full register:

- Exact pricing model, commission/take-rate, subscription pricing and cycle length
- Supported languages for the UI itself (not to be confused with tutoring subject languages)
- VAT / tax handling, invoicing requirements, tutor tax residency handling
- Content moderation policy specifics (what gets a tutor suspended, review abuse thresholds)

**Resolved**: group lessons are explicitly **out of scope for v1**. The domain must not be made structurally impossible to extend to group lessons later, but no group-lesson functionality or speculative complexity for it is implemented now.

**Resolved (Product Owner, 2026-10-04)**: currency is **EUR only for V1** — see `ADR-0004`'s "Product Owner decisions locked for V1" section. The `Money` value-object pattern (integer minor units + ISO 4217 currency code) already used throughout the codebase remains currency-agnostic in shape; this decision only constrains which currency `Payments` is configured to accept in V1, not the type system. Supported currencies beyond EUR remain open for a future iteration.

# ADR-0004: Payment Provider & Webhook Authority Model

**Status:** Accepted

## Context

`Payments` must support SCA/3DS (European market requirement), idempotent webhook-driven confirmation, refunds, disputes, and marketplace payouts to tutors (`docs/product/TRIAL_BOOKING_WORKFLOW.md`, `docs/domain/STATE_MACHINES.md`).

## Decision

Use **Stripe** as the marketplace payment provider:

- **Stripe Connect** for tutor connected accounts and payouts (marketplace payout splitting).
- **Stripe PaymentIntents / Checkout** (as appropriate per flow) for charging students.
- **SCA/3DS** support via Stripe's PaymentIntents flow (required for the European market).

### Non-negotiable requirements

- **Webhooks are authoritative** for all payment-state transitions. No code path confirms a booking or payment purely because the browser/frontend reported success.
- **Webhook signature verification is required** on every incoming webhook.
- **Payment operations must be idempotent** — a webhook delivered more than once must not double-confirm a booking, double-publish a side-effecting integration event, or double-credit a tutor's earnings.
- **Payment state is independent from Booking state.** `Payments` and `Bookings` are separate state machines (see `docs/domain/STATE_MACHINES.md`) that react to each other only through integration events — neither reaches into the other's data.
- **Amounts are stored as integer minor units plus an ISO 4217 currency code.** No floating-point monetary types anywhere in the codebase.

## Product Owner decisions locked for V1 (2026-10-04)

The following were open questions this ADR explicitly deferred (see the original Consequences list below); the Product Owner has now resolved them for the first production payment implementation. None of these are implemented by this update — `Payments` still does not exist.

- **Stripe Connect account type: Express.** Stripe hosts connected-account onboarding/KYC for tutors (Stripe's own hosted flow), rather than `Custom` (which would require TutorFlow to build its own onboarding/compliance UI) or `Standard` (which gives the tutor a full, independently-usable Stripe dashboard — more than a marketplace payout recipient needs). **Rationale:** lowest integration/compliance burden for a small team, while still meeting EU KYC requirements through Stripe's own hosted flow. Resolves the "specific Stripe Connect account type" item this ADR originally deferred.
- **Currency: EUR only for V1.** No multi-currency support is built in the first implementation. **Rationale:** matches `docs/product/PRODUCT_VISION.md`'s "EUR primary" target market, and avoids building currency-conversion/multi-currency-ledger logic before there is a concrete need. The `Money` value-object pattern already used by `Tutors` and `Bookings` (integer minor units + ISO 4217 currency code, per the non-negotiable requirement above) remains currency-*agnostic* in shape — nothing about this decision hardcodes `"EUR"` into the type system; it only constrains which currency `Payments` is configured to accept in V1. Multi-currency remains possible to add later without a `Money` redesign.
- **Dispute evidence process: manual Admin for V1.** When Stripe reports a payment dispute (chargeback), a human Admin handles evidence submission through Stripe's own dashboard/API by hand. No automated dispute-evidence generation is built. Resolves the "dispute evidence workflow" item this ADR originally deferred.
- **Partial refunds: out of scope for V1 — full-refund-only.** The `Payments`↔`Bookings` refund contract (`ADR-0015` §5) carries no partial-amount parameter; a requested refund is always for the full captured amount. Resolves the "partial-refund policy" item this ADR originally deferred; see `ADR-0015` §5 and `docs/domain/STATE_MACHINES.md` for the mechanical consequence, and `docs/product/TRIAL_BOOKING_WORKFLOW.md` for the cancellation-window policy that decides *whether* a refund is requested at all.

**Still deferred, not resolved by this update** (remaining Product/Legal decisions — see `docs/OPEN_QUESTIONS.md`): payout frequency, payout minimum threshold, payout fees, exact earnings accrual/risk-window timing, VAT/tax/invoicing/tutor-tax-residency handling. None of these block the first `Payments` feature (PaymentIntent creation via Stripe Connect Express) — they block the payout/tax side specifically.

**Keep the provider behind an abstraction where practical** — `Payments`' own module boundary (an opaque `PaymentReferenceId` crossing into `Bookings`, per `ADR-0015` §1) already prevents Stripe-specific types leaking into `Bookings`; within `Payments` itself, isolate Stripe SDK calls behind an internal seam (e.g. a narrow interface wrapping PaymentIntent/Connect calls) so a future provider change doesn't require touching `Bookings` or any other module. This is a code-organization expectation for whenever `Payments` is scaffolded, not a new architectural decision requiring its own ADR.

## Rationale

Stripe has mature EU/SCA support, a well-established webhook model, and Stripe Connect is purpose-built for marketplace payout splitting — the standard default for this product shape (two-sided marketplace with per-tutor payouts).

## Consequences

- `Payments`' schema and webhook handlers are now designed against Stripe's specific event taxonomy (`payment_intent.succeeded`, etc.) when that module is scaffolded — no longer a provider-agnostic placeholder.
- The specific Stripe Connect account type (Standard vs. Express vs. Custom) for tutor onboarding — **Resolved 2026-10-04: Express**, see "Product Owner decisions locked for V1" above.
- No Stripe integration code, SDK wiring, or webhook endpoints are introduced by this ADR — this is an architectural decision only; implementation happens when `Payments` gets its first real feature.
- This resolves Architect question A5 (`docs/OPEN_QUESTIONS.md`).
- Deferred and *not* resolved by this ADR originally: payout frequency, payout minimum threshold, payout fees, earnings accrual timing, dispute evidence workflow, partial-refund policy, VAT/tax/invoicing model. **As of 2026-10-04**, dispute evidence (manual Admin) and partial refunds (out of scope, full-refund-only) are resolved — see above. Payout frequency/minimum/fees, earnings accrual timing, and VAT/tax/invoicing remain `[OPEN — Product Owner]` per `docs/OPEN_QUESTIONS.md`.

# Payments, Subscriptions, and the Platform Economy

Cross-references the pre-existing `docs/product/SUBSCRIPTION_JOURNEY.md` and `docs/product/TRIAL_BOOKING_WORKFLOW.md` (both brief-derived, both explicit that pricing/cycle/commission numbers were deliberately left `[OPEN — Product Owner]` pending real research). This document supplies the real research those files anticipated.

## Student-side payment

### Payment methods — DOCUMENTED (help.preply.com/articles/4182730)

| Method | Restriction |
|---|---|
| Cards: Visa, MasterCard, Amex, Diners Club, Discover, JCB, China UnionPay, Maestro, Bancontact | JCB/UnionPay/Maestro/Bancontact may be unavailable by geography |
| Apple Pay, Google Pay | **App + mobile-site only, not desktop web** |
| PayPal | USD only; currency-exchange fees may apply |
| iDEAL | Netherlands only; one-time payments only, not recurring |

### Currency — DOCUMENTED

Payments processed in USD, EUR, or GBP depending on geography. TutorFlow has already made an independent, locked Product Owner decision to be **EUR-only for V1** (`ADR-0004`) — an INTENTIONAL DEVIATION from Preply's multi-currency support, not a gap.

### Checkout presentation — DOCUMENTED (help.preply.com/articles/15432866)

Checkout shows: "the cost of your lesson plan or extra lessons" + "a processing fee, shown before you complete payment" + "applicable taxes, based on your location," combined into one **"Taxes & Fees"** total. "Your full amount is always displayed before you confirm payment." Bank statements may show a slightly higher amount due to the student's own bank's conversion/international-transaction fees — explicitly **not** a Preply-side charge.

**TutorFlow has no tax/fee computation anywhere in its pricing model** (confirmed by direct code inspection this project — `Booking.PriceAmountMinorUnits` is a flat pro-rata figure with no fee/tax field). This is a genuine, real gap against observable Preply behavior, and is **not resolved here** — see `16-product-decisions-and-open-questions.md`. TutorFlow's own checkout UI (built this project) correctly shows a flat "Total" without claiming it includes any tax, rather than inventing a fake tax line.

### Payment timing — DOCUMENTED

Three charge moments: initial subscription/trial payment at checkout, automatic renewal every 28 days, and manual extra-lesson purchases any time.

## Subscriptions — DOCUMENTED (prior research this project)

| Mechanic | Behavior |
|---|---|
| Cycle length | 28 days (not a calendar month) — first cycle starts at initial payment |
| Minimum plan | 4 lessons per cycle |
| Pricing | lesson count × tutor's rate; rate locked at subscription time, student notified/must confirm if the tutor's rate later changes |
| Renewal | saved payment method charged; new lessons added; unscheduled lessons from the expiring cycle are forfeited |
| Plan change | increase/decrease lesson count any time; upgrades apply next renewal, downgrades apply the following cycle |
| Extra lessons | purchasable mid-cycle without changing the plan; do not expire with the cycle; carry over up to 2 full billing cycles |
| Pause | once per cycle, delays renewal up to 20 days, can still take remaining lessons; resumable early via "Renew subscription now" (greyed out until all remaining lessons are scheduled) |
| Cancel | no fee, no lock-in; already-booked lessons remain protected regardless of renewal timing |
| Resubscribe | anytime |
| Multiple tutors | independent subscription per tutor, managed separately |
| Credit transfer between tutors | remaining lessons convert to "Preply Credits" at the *new* tutor's rate on the transfer date; credits never convert to cash; requires having previously completed/scheduled/cancelled a trial with the new tutor first |

TutorFlow's `Subscriptions` module is a **pure stub** — none of this exists in any form. See `13-tutorflow-current-state-and-parity.md`.

## Tutor-side earnings, commission, payout

### Commission — DOCUMENTED, now confirmed on Preply's own official page

> "commission details (trial lessons at 100%, subsequent lessons from 33-18%)" — directly quoted from preply.com/en/teach's own FAQ, real-browser pass.

This **upgrades** an earlier, third-party-sourced (NOT VERIFIED) figure to DOCUMENTED. Separately, help.preply.com/articles/4171348 confirms: lessons auto-confirm (≈15min inside Classroom / 72h outside) and earnings = "your rate minus Preply's commission," credited to balance only on confirmation. **Trial lessons generate zero tutor earnings** under the 100% figure.

### Withdrawal — DOCUMENTED (help.preply.com/articles/15083120)

| Method | Minimum | Fee | Notes |
|---|---|---|---|
| Bank Transfer (Wise-powered) | $10 | $0.56–1.13 + conversion | Local currency |
| Wise | $10 | ~0.6% | — |
| PayPal | none | 2% (up to $20) | USD only |
| Skrill | $10 | 1% | USD only |
| Payoneer | $20 | +$1 | USD only |

Maximum $3,000/transaction across all methods.

### Identity verification (payout-gated) — DOCUMENTED (help.preply.com/articles/4175176)

Required only at first withdrawal (not registration): live-captured government-ID photo (no pre-existing photo uploads), same sanctioned-country restriction as registration, data retained ≤60 days (staff access) / deleted within 1 year.

### Withdrawal secondary verification — DOCUMENTED (help.preply.com/articles/16645480)

For withdrawals to a *new* recipient account: SMS code (3 attempts, if phone verified) or Veriff-mediated ID check otherwise. 3 failed SMS + incomplete Veriff → withdrawal cancelled, funds returned, email sent.

### Annual tax information

Confirmed to be required annually, but the exact form/mechanism (W-9/W-8-style, 1099-K via Stripe) is **NOT VERIFIED** against an official Preply source — third-party sources mention this but it was not independently confirmed on help.preply.com.

## TutorFlow's current state (cross-reference)

TutorFlow has a correct, real, already-tested Stripe PaymentIntent + webhook + Payment Element pipeline for **student-side trial payment collection** (built this project). TutorFlow has **no tutor earnings ledger, no commission computation, no payout mechanism, and no Stripe Connect integration at all** — this remains blocked on the already-identified Product Owner decision about the commission/take-rate model and Connect charge type (`ADR-0018`, `ADR-0027`). The real commission figures above (100% / 18–33%) are now available as a concrete reference point for that decision, but do **not** resolve it — adopting Preply's exact figures is itself a Product Owner choice, not an automatic consequence of having found them.

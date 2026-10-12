# Edge Case Matrix

## OBSERVED edge cases

| Edge case | Observed behavior |
|---|---|
| Guest browsing without an account | No forced login wall on homepage, search, or tutor profile viewing — confirmed via real browsing |
| Guest views subcategory review ratings | Visible without login, on the public profile |

No other edge case was directly browser-observed this project — every page-level investigation reached a login/account boundary before any further edge case could be triggered live.

## DOCUMENTED edge cases

| Edge case | Preply behavior | Source confidence |
|---|---|---|
| Tutor rejects a trial request | Tutor cancels directly or messages the student; student is free to rebook elsewhere | High |
| Rejected trial's balance | Transfers to another tutor via "Book trial with balance" → "Use my balance"; blocked if the original trial is already marked "Paid"/"Cancelled and paid", or inside the 12h window | High |
| Cancellation ≥12h before start | Lesson returns to balance; tutor receives no compensation | High |
| Cancellation <12h before start | Lesson does not return to balance; recorded as "Cancelled and Paid"; tutor compensated | High |
| Lesson not manually confirmed | Auto-confirms ≈15 min after end (inside Classroom) or 72h after end (outside), unless the student confirms sooner | High |
| Tutor inactivity (no-show reports, non-response) | Profile auto-hidden from search; self-resolves for non-response by replying | High |
| Account inactivity (180 days) | Account auto-suspends; balance expires | Medium (ToS-summarized) |
| Student blocks/is blocked by a tutor | Shared upcoming lessons auto-cancel; blocked party keeps history/profile/other-tutor access; existing reviews remain visible; no explicit notice to the blocked party | High |
| Withdrawal to a new recipient account | SMS (3 attempts) or Veriff check; 3 failed SMS attempts + incomplete Veriff → withdrawal cancelled, funds returned, email sent | High |
| Review deemed policy-violating | Removable/challengeable only via Support escalation, not self-service | High |
| Subscription renewal with a saved payment method | Charges automatically; new lesson credits added; unscheduled lessons from the expiring cycle are forfeited | High |
| Subscription pause | Up to 20 days, once per cycle, can still take remaining lessons during the pause | High |
| Extra lessons purchased mid-cycle | Do not expire with the current cycle; carry over up to 2 full billing cycles | High |

## INFERRED edge cases (never confirmed — reasonable inference only)

| Edge case | Inference | Basis |
|---|---|---|
| Guest clicks "Book trial lesson" | Triggers a login/signup modal or redirect before proceeding | Universal marketplace UX convention; matches TutorFlow's own already-built `AuthGateModal` pattern — never directly observed clicking through |
| Clicking a specific availability time slot as a guest | Also gated behind login | Same inference basis, same caveat |

## NOT VERIFIED edge cases

| Edge case | Why unverified |
|---|---|
| What happens if a saved payment method fails on subscription renewal | No official article found describing the grace period/retry behavior |
| Exact meaning of "You still have 2 free tutor trials" shown on a tutor profile | Raw text observed; no article found explaining the mechanic it refers to |
| Double-booking / concurrent-hold race behavior on Preply's own system | Internal implementation, never investigated by design (out of scope — Preply's internals are not the benchmark, only observable behavior is) |
| Payment failure retry UX | Not found in any official source fetched this project |
| What a student sees mid-3DS/SCA challenge on Preply itself | Not found; Stripe's own standard challenge UI is what any Stripe-integrated site would show, but this was never confirmed specifically for Preply |

## Cross-reference to TutorFlow's own already-tested edge cases

TutorFlow's own backend has extensive, already-passing test coverage (from this project's own prior work) for: concurrent-hold races, exclusion-constraint enforcement, expired-hold checkout rejection, duplicate-webhook idempotency, duplicate-payment-intent prevention, and reschedule/cancellation guard rails. These are **TutorFlow-side verified facts**, not Preply-side ones — see `13-tutorflow-current-state-and-parity.md` for where TutorFlow's own correctness has already been established independently of Preply parity.

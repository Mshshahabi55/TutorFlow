# Product Decisions and Open Questions

Consolidates every open, pending, or conflicting item discovered across all reverse-engineering passes this project. Nothing here is resolved by this documentation — per the project's standing rule, product and business decisions are never made silently.

## Already decided (for reference, not re-opened here)

- Currency: EUR-only for V1 (`ADR-0004`) — an INTENTIONAL DEVIATION from Preply's USD/EUR/GBP support.
- Cancellation/reschedule/no-show mechanics locked by a prior Product Owner decision (≥24h full refund / <24h none; reschedule once, ≥12h notice; 10-min no-show grace) — **now known to conflict with Preply's actual 12-hour rule** on the cancellation-window number specifically (see below).
- Trial pricing formula: pro-rata from the tutor's hourly rate (`BOOKINGS_SPEC.md` §1.3) — not re-opened by this directory; Preply's own per-lesson pricing presentation (`11-...md`) doesn't contradict the *formula*, only the *display format*.
- Group lessons: explicitly out of scope for V1 (pre-existing `PRODUCT_VISION.md`).
- Dual Student+Tutor role on one account: in scope (pre-existing `ADR-0002`).
- Admin is "composed," not a 15th module (pre-existing `ADR-0008`).

## Product Owner decisions genuinely pending

1. **Tutor Application wizard structure.** TutorFlow's *implemented* wizard already has 8 steps (not the 17-step brief); Preply's real wizard is now confirmed (OBSERVED, screenshot evidence) to also have 8 steps, but with different content grouping — e.g. TutorFlow splits Preply's single "About" step across two steps, and Preply splits TutorFlow's combined "Credentials" step into two (Certification, Education). Realign TutorFlow's step content to match Preply's grouping exactly, or keep the current grouping as a deliberate difference? See `07-wizard-inventory.md` and `18-screenshot-evidence.md`. No technical blocker either way — purely a product call.
2. **Cancellation window correction.** TutorFlow's locked spec says ≥24h for a refund; Preply's actual policy is 12h — originally DOCUMENTED (help article S16), now additionally **OBSERVED directly inside the live checkout flow itself** (`18-screenshot-evidence.md`, SS38: "Cancel or reschedule for free until 12 hours before your lesson."), the strongest evidence tier this directory uses. Correct the spec to match, or keep 24h as a deliberate, more-student-friendly deviation? (No live bug today — the refund mechanism itself isn't built yet — but the number should be decided before it is, and the evidence for 12h is now about as strong as this project's methodology can produce.)
3. **Taxes & Fees presentation.** Preply shows a "Taxes & Fees" total at checkout; TutorFlow has no tax/fee concept anywhere. New evidence (`18-screenshot-evidence.md`, SS39): a real checkout showed an itemized **"Processing fee" of €0.27 on a €42.72 lesson (≈0.6%)** — a single data point, not enough to infer whether it's flat, percentage-based, or payment-method/region-dependent. Should TutorFlow add an equivalent line item, and if so, what's the actual computation? Still not invented here — the new evidence narrows the question but doesn't answer it.
4. **Commission / take-rate model.** Preply's own figures (100% on a client's first trial, 18–33% standard) are now a concrete reference point — still blocked on the pre-existing, unresolved Stripe Connect/commission decision (`ADR-0018`, `ADR-0027`). Adopting Preply's exact numbers is itself a choice, not automatic.
5. **Tutor accept/decline + balance/credit model.** Confirmed real Preply behavior; building it requires a balance/credit system TutorFlow doesn't have. Minimal viable approach (e.g., auto-refund on decline instead of a balance-transfer) vs. full balance-ledger approach — not decided.
6. **Subscriptions V1 scope.** Full Preply parity (pause/transfer/multi-tutor/credit-conversion) in one pass, or core-only (cycle + renew + cancel) first? The pricing/cycle/rollover numbers that an earlier recommendation said must exist before scaffolding persistence are now DOCUMENTED — the blocking condition on *starting* the work is resolved, but the *scope* decision is not.
7. **Classroom video provider.** Managed (Daily.co/Twilio/Agora/LiveKit Cloud) vs. self-hosted vs. third-party embed — a real cost/time/product-quality tradeoff, not decided here.
8. **Account-standing tiering.** Adopt Preply's 3-tier model (voluntary hide / auto-hide / full suspension) as TutorFlow's own policy, or something simpler? A real policy decision, not just an engineering task.
9. **Admin role/sub-roles** — the base `Admin` role now exists and is implemented (ADR-0028), unblocking the tutor-application review pipeline (P1, now implemented). **Sub-roles** (`SupportAgent`/`ContentModerator`/`SuperAdmin` per `ROLES_AND_PERMISSIONS.md`'s own open question) remain unresolved — `Admin` is still a single flat role.
10. **Block feature's effect on existing data.** Preply explicitly does *not* retroactively hide a blocked user's past reviews — should TutorFlow match that exactly, or differ?
11. **Tutor wizard Review/Summary step.** Screenshot evidence (`18-screenshot-evidence.md`, SS13) now directly confirms Preply's real flow has no distinct review/summary screen — Pricing is the last step before "Complete registration." TutorFlow's wizard has an explicit `ReviewStep.tsx`. Keep it as a deliberate TutorFlow improvement, or drop it to match Preply exactly? No technical blocker either way.
12. **"Find your tutor" matching quiz.** A second, larger screenshot pack (`18-screenshot-evidence.md`, SS17–SS32) discovered an entire previously-unknown pre-search personalization quiz (goal, topics, skills, industry, timeline, level, special needs, teaching-style traits, tutor-country/language preferences, availability, budget — 15 steps, answers become live search filters). Build an equivalent for TutorFlow, or keep TutorFlow's simpler filter-first search as a deliberate V1 scope decision? A real but non-trivial build, not a quick add.
13. **Teaching-style trait matching + AI match-explainer.** The same pack shows tutors carry visible "teaching style" trait pills (Adaptable, Approachable, Encouraging, Engaging, Goal-Focused, Patient, Structured) and profiles explicitly explain *why* a tutor matched ("Great match! {Name}'s teaching style matches 1 of your preferences"). Adopt an equivalent concept, and if so where do the trait values come from (self-reported by the tutor? computed?) — not decided here, needs product input.
14. **"2 free tutor trials" — now resolved as a real feature to scope, not just an observation.** Confirmed (SS16, SS37–SS39) as Preply's Satisfaction Guarantee: every student gets up to 2 free tutor-replacement trials if a match isn't right. Should TutorFlow build an equivalent credit/replacement-trial concept, and if so what's the minimal viable version (a counter on the student account? tied to Bookings somehow?) — a genuinely new, well-specified candidate feature, not decided here.

## Conflicting official Preply sources (not resolved, both preserved)

| Conflict | Source A | Source B |
|---|---|---|
| Tutor application review time | "3 business days" — preply.com/en/teach (Become-a-Tutor landing, official marketing page) | "5 business days" — help.preply.com registration article (official Help Center) |

## Behavior previously observed but not interpreted — now resolved

- ~~**"Not a match? You still have 2 free tutor trials"**~~ — **RESOLVED** by the second screenshot evidence pack. Confirmed, across four independent appearances (homepage marketing copy, authenticated profile sidebar, checkout confirmation modal, and checkout page itself — see `18-screenshot-evidence.md` §"Checkout (SS38–SS39)"), to be the real mechanism behind Preply's "Lessons you'll love. Guaranteed." promise: every student account gets up to **two free tutor-replacement trials** if their first picks aren't a match. This is no longer an uninterpreted observation — see product-decision item 14 above for the resulting (still-open) build decision.

## Preply behavior not verified (candidates for future research, not decisions)

- TalkNow — exists, depth unknown.
- Lesson Insights — exists, depth unknown.
- Exact mobile-viewport rendering of any page (no mobile browser testing was performed).
- Terms of Service page content (URL not found within this pass's budget).
- Whether a tutor sees subcategory review scores on their own dashboard, distinct from the public profile.
- Exact notification types/channels.
- Payment-failure/retry UX on Preply's own checkout.
- Whether Preply's "Taxes & Fees" figure varies meaningfully by jurisdiction in a way TutorFlow would need to replicate structurally, or is a flat processing-fee-plus-simple-tax model.

## Items this directory explicitly does not decide on the Product Owner's behalf

Every item in the "Product Owner decisions genuinely pending" list above. This directory documents evidence; it does not choose outcomes.

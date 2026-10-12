# Platform / Trust & Safety

All findings in this document are **DOCUMENTED** (official source, not directly browser-observed) unless marked otherwise. None of this was ever browser-tested — doing so would have required a real account, a real report/block action against a real person, or both, all explicitly out of scope. No internal Preply mechanism is described anywhere below beyond what an official source states; where a source was only search-engine-summarized rather than directly fetched and read, this is called out explicitly as lower confidence.

## Reporting and blocking

- **Block** (help.preply.com/articles/5588370, **DOCUMENTED, high confidence — directly fetched**): Block action lives in the Messages tab after selecting a student. Requires a reason: harassment, bypassing [the platform], scam, "don't want to teach this student," or other+explanation. Effect: the blocked student can no longer message or book with that tutor, or leave a review; all shared upcoming lessons auto-cancel. The blocked student gets **no explicit notice** beyond an in-chat restriction banner, and keeps full access to: message history, the tutor's profile page, booking with other tutors, past lessons, balance transfer, and support. **Existing reviews from that student remain visible** — blocking is not retroactive to review content.
- **Reporting spam messages**: a 3-dot menu on the message itself; Preply states reports are typically reviewed within an hour — **DOCUMENTED, medium confidence** (title/snippet-level source, not independently fetched in full this pass).

## Disputes and refunds

- The **"100% Satisfaction Guarantee Policy"** is invoked via Support when the standard trial-balance-transfer path (see `02-student-journey.md`) doesn't apply — **DOCUMENTED, high confidence** (directly fetched in an earlier pass).
- Refund eligibility is time/usage-gated: full refund only if no lessons used and payment is less than 28 days old (monthly plans), or within a 14-day window for yearly plans (partial refund for used-lesson value otherwise) — **DOCUMENTED, medium confidence** (search-summarized from a Terms-of-Service PDF, not independently re-fetched and read in full — flagged for re-confirmation if this becomes load-bearing for any TutorFlow decision).
- Disputes/refund requests route to `support@preply.com`.

## Account standing — three distinct severities (a genuinely new, more precise finding)

Preply does **not** have one undifferentiated "suspended" state. Three are confirmed:

1. **Voluntary profile hide** — tutor-initiated, from the dashboard, instantly reversible, removes from search only; existing students can still reach the tutor. **DOCUMENTED, high confidence** (help.preply.com/articles/4175297, directly fetched).
2. **Automatic visibility hide** — system-triggered, for: repeated no-show reports, failing to reply to a new student within 24h, reported harassment/fraud/inappropriate conduct, or a broken intro video. The responsiveness-triggered case **self-resolves** by replying to pending messages; the behavioral-violation cases require Support intervention to lift. **DOCUMENTED, high confidence** (same source).
3. **Full account suspension/termination** for a Terms-of-Service breach — forfeits any balance as "liquidated damages," no refund. Separately, **180 days of account inactivity auto-suspends an account and expires its balance.** **DOCUMENTED, medium confidence** (search-summarized from the Terms-of-Service PDF, not independently re-fetched).

TutorFlow's current implementation has only a single, undifferentiated `Suspend()` method on `TutorApplication` with no equivalent of tier 1 vs. tier 2 vs. tier 3 — a real, previously-undocumented gap, recorded in `13-tutorflow-current-state-and-parity.md`.

## Identity verification (general trust, distinct from payout-specific KYC)

No general, trust-oriented identity verification was found for **either** role at registration time. The only identity-verification mechanism confirmed anywhere is the **payout-specific** one (tutors only, triggered at first withdrawal) — see `03-tutor-journey.md` and `11-payments-subscriptions-and-economy.md`. **NOT VERIFIED** beyond its absence from every article checked — absence of evidence is not proof of absence, but nothing supports inventing a general verification step.

## Age restrictions

Both students and tutors must be 18+ generally; students aged 13–17 may use the platform only under parent/guardian supervision — **DOCUMENTED, medium confidence** (Terms-of-Service summary, not independently fetched). Enforcement appears to be self-declaration; no age-specific ID gate distinct from the payout-time identity check was found.

## Content moderation

A Harassment Policy explicitly bans threats/intimidation, unsolicited sexual content, cyberstalking, doxxing, hate speech/slurs, and discrimination, applying across messages and general conduct — **DOCUMENTED, high confidence** (directly fetched). Enforcement is binary per that article (deactivation + content removal, or legal escalation for serious cases) — **no tiered warning/temporary-suspension step is documented** for harassment specifically (contrast with the three-tier account-standing model above, which covers *other* trigger categories). A 180-day appeal window to a dedicated review team exists. A broader "Rules / Code of Conduct" page is known to exist but was not independently fetched — **NOT VERIFIED** beyond the harassment-specific article.

## Tutor application review and rejection (cross-reference)

Rejection reasons (profile quality, under-18, low subject demand, residency in a sanctioned country) and the review-timing conflict (3 vs. 5 business days) are documented in `03-tutor-journey.md` and `07-wizard-inventory.md` — not repeated here.

## Help / Support structure

Support is chat/email only, no phone/video; desktop entry via a "?" icon, app entry via Settings → Customer Support; `support@preply.com`; stated 24/7 availability — **DOCUMENTED, medium confidence** (search-summarized from an official article's title/snippet, not independently fetched and read in full this pass).

## What remains BLOCKED

The actual in-product UI for reporting, blocking, receiving a suspension notice, or going through identity/withdrawal verification — none of this is observable without a real account and a real triggering action, neither of which this project performs.

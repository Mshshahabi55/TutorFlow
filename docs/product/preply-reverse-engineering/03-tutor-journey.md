# Tutor Journey

Cross-references the pre-existing `docs/product/TUTOR_JOURNEY.md` (brief-derived, not Preply-verified). **This document contains the project's most significant known conflict with that older file — see the Onboarding section below and `07-wizard-inventory.md`.**

## Become a Tutor (publicly observed this project)

```text
Homepage → "Become a tutor" (OBSERVED, exact nav label)
  → preply.com/en/teach landing page (OBSERVED)
    Headline: "Make a living by teaching the largest community of learners worldwide"
    Process (exact text): "1. Sign up to create your tutor profile / 2. Get approved by our
      team in 3 business days / 3. Start earning..."
    CTA: "Create a tutor profile now" — appears to launch registration directly
  → Registration / Application
    → BLOCKED — requires account creation, not performed
```

## Onboarding — THE KNOWN CONFLICT

The pre-existing `docs/product/TUTOR_JOURNEY.md` specifies a **"17-step wizard, as specified"**, treated by `docs/product/TUTOR_ONBOARDING_WIZARD_SPEC.md` as an authoritative, already-locked product decision ("not changed by this spec... an existing, already-specified product decision"). Those 17 steps (Account, Basic Information, Photo, Languages, Subjects, Specialties, Experience, Education, Certificates, Intro Video, Pricing, Availability, Booking Settings, Preview, Submit, Verification, Published) came from the project's **original product brief**, not from any Preply research.

This project's actual Preply reverse-engineering found, from Preply's own official registration article (help.preply.com/articles/4175138, **DOCUMENTED**), exactly **7 confirmed sections**:

1. Profile Picture
2. Certifications
3. Education
4. Profile Description
5. Intro Video
6. Calendar (availability + timezone, combined as one section)
7. Tutoring Rate

The article's own text claims an "8-section application" while enumerating only 7 — the 8th is explicitly **NOT VERIFIED / not identifiable from the official source**, and was never invented.

**This conflict is preserved, not resolved, by this documentation.** The 17-step structure is a real, already-implemented TutorFlow product decision (per the older spec's own explicit instruction not to silently change an existing decision); the 7-section structure is what Preply's current official source actually documents. Whether TutorFlow's wizard should be restructured to match the 7-section reality is a **Product Owner decision**, listed in `16-product-decisions-and-open-questions.md`. Full step-by-step comparison in `07-wizard-inventory.md`.

Review/approval timing has its own conflict: the Become-a-Tutor marketing page says **3 business days**; the registration help article says **5 business days** — both **DOCUMENTED**, both official, not reconciled here.

## Post-Approval / Dashboard (all DOCUMENTED unless noted — none of this was browser-observable without an account)

### Tutor Insights (closest official equivalent to a "dashboard home")

Six sections, from help.preply.com/articles/11067649 (**DOCUMENTED**):

- **Overview** — earnings, lessons taught, active students, new students
- **Keys to Success** — reschedules, cancellations, missed lessons, weekly consistency, Classroom usage, response time, trial follow-ups, popular time slots, average rating, profile score, price/lesson
- **New Subscriptions** — profile views, trials completed, new subscriptions started
- **Super Tutor** — results from the last monthly calculation against all 9 criteria
- **Earnings** — 7-day/30-day/lifetime filters, downloadable reports
- **Your Preply Journey** — lifetime hours taught, lifetime lessons, total students, days active, lifetime net earnings

The pre-existing `docs/product/TUTOR_DASHBOARD_SPEC.md` resolved "Insights" as a BFF composition with no owning module (profile-view/ranking signals from Search, conversion from Bookings, rating trend from Reviews) — this is architecturally still sound, but the **actual section list above is now a real, sourced target** that document didn't have.

### Earnings, commission, payout

Full detail in `11-payments-subscriptions-and-economy.md`. Headline facts: trial lessons generate **100% commission to Preply** (zero tutor earnings on a client's first trial); standard lessons are commissioned at **18–33%** (both **DOCUMENTED**, the second figure confirmed on Preply's own official page in the real-browser pass, upgrading an earlier NOT-VERIFIED third-party figure).

### Identity verification (payout-gated, not registration-gated)

Required only at first withdrawal, not at registration — live-captured government-ID photo, same sanctioned-country list as registration, data retained ≤60 days for staff access / deleted within 1 year — **DOCUMENTED** (help.preply.com/articles/4175176). A **separate**, additional layer applies specifically when withdrawing to a *new* recipient account: SMS code (3 attempts) or Veriff-mediated ID check — **DOCUMENTED** (help.preply.com/articles/16645480).

### Student management (tutor side)

**NOT VERIFIED** — no official article describing a dedicated student-list/notes/history page was found. The Insights page's "active students"/"new students" counts are the only confirmed adjacent signal. The pre-existing `TUTOR_DASHBOARD_SPEC.md`'s resolution (A17: Students-list is `Bookings`-owned, derived from confirmed-booking relationships) remains architecturally reasonable but is **not confirmed against a real Preply UI**.

### Tutor-side accept/decline of a trial request

**NOT VERIFIED** from the tutor's own observable perspective — every search result returned the *student*-facing consequence article, not the tutor's notification/action UI. The student-side mechanics (tutor cancels directly or messages the student; declined trial's balance transfers to another tutor) were already **DOCUMENTED** in prior research and remain unchanged.

### Reviews (tutor side)

- Tutor receives a notification, can view and **respond** to a review — **DOCUMENTED** (help.preply.com/articles/6231606).
- Subcategory scores (Clarity/Progress/Preparation/Reassurance) can only be challenged/removed via Support, on policy-violation grounds — no general dispute right — **DOCUMENTED**.
- Whether the tutor sees an aggregate subcategory breakdown on their own dashboard (vs. only publicly, which this project's real-browser pass confirmed — see `12-classroom-messaging-reviews-notifications.md`): **NOT VERIFIED**.

### Super Tutor / Professional Tutor

- **Professional Tutor**: static credential — passing Preply's own Online Language Teaching Course, or holding a government-accredited/endorsed ≥120-hour teaching certificate. Available for any language — **DOCUMENTED** (help.preply.com/articles/9795272, 9645596).
- **Super Tutor**: rolling 90-day performance badge, 9 criteria (full list in `06-feature-inventory.md` and `11-payments-subscriptions-and-economy.md`-adjacent performance detail) — **DOCUMENTED** (help.preply.com/articles/14880665).
- Progress visibility: the Insights page's dedicated Super Tutor section — **DOCUMENTED**.

### Profile Score

0–100%, derived from photo/video/description quality, shown on the tutor's own statistics page, "strong correlation" with trial-lesson volume claimed by Preply itself, edits reviewed within 48 hours — **DOCUMENTED** (help.preply.com/articles/7863157).

### Tutor settings (payout method, tax info, notification/language/calendar preferences as a distinct settings surface)

Payout method setup is confirmed via the withdrawal flow itself. An annual tax-information requirement is confirmed to exist, but its exact form/UI (W-9/W-8-style) is **NOT VERIFIED** via an official Preply source. A distinct settings page for notification/language/calendar preferences (separate from availability configuration itself): **NOT VERIFIED**.

### Profile visibility / auto-hide

A tutor can voluntarily "Hide profile" (instantly reversible, removes from search, existing students unaffected). **Separately**, Preply automatically hides a profile from search for: repeated no-show reports, failing to reply to a new student within 24h, reported harassment/fraud/inappropriate conduct, or a broken intro video — replying restores visibility for the responsiveness case; behavioral-violation hides need Support — **DOCUMENTED** (help.preply.com/articles/4175297). Full detail in `04-platform-trust-and-safety.md`.

## What remains entirely BLOCKED

Everything requiring an actual account: the real wizard-step UI, draft/resume mechanics, the real post-rejection resubmission flow, the real dashboard UI beyond what the Insights article describes in prose, the real messaging UI from the tutor's side, and the real withdrawal UI beyond what the help articles describe in prose.

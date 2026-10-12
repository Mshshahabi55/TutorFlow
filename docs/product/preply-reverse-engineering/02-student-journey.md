# Student Journey

Cross-references the pre-existing `docs/product/STUDENT_JOURNEY.md` (brief-derived, not Preply-verified) — this document supersedes it wherever a conflict or new evidence exists, and is explicit about which stages are now actually evidence-backed versus still assumption.

## Anonymous Student (publicly observed this project)

```text
Homepage (OBSERVED)
  → Subject discovery / search entry (OBSERVED)
  → Search results with filters (OBSERVED — exact filter labels in 01-real-user-observations.md)
  → Tutor profile (OBSERVED — one real profile, full field inventory)
  → "Book trial lesson" / "Send message" CTA click
    → BLOCKED / INFERRED — authentication gate almost certainly triggers here (universal
      pattern, matches TutorFlow's own AuthGateModal), but the exact modal/redirect was never
      directly observed
```

Guest can browse, search, filter, and view full tutor profiles (including reviews and subcategory ratings) with **no forced login wall** — confirmed OBSERVED, not assumed.

## Registration / Login

- Required fields: name, email, password; alternative signup via Google/Facebook/Apple ID — **DOCUMENTED** (help.preply.com/articles/4214724).
- Account creation is near-instant ("ready within seconds") — **DOCUMENTED**.
- Post-signup, reviewing account info and setting a timezone in Settings is **recommended, not forced** — **DOCUMENTED**. No mandatory learning-goals/subject-selection onboarding wizard was found — **NOT VERIFIED** as either present or absent; absence of evidence is not evidence of absence.

## Account / Profile Settings

- Editable fields (profile-data page): profile image, first name, last name, email, phone number, timezone, social media accounts; "Save Settings" action; account deletion available from the same page — **DOCUMENTED** (help.preply.com/articles/4179300).
- Notification preferences live in a separate Settings → Notifications page (preply.com/en/settings/notification) with "Save settings" and "Unsubscribe from all" — **DOCUMENTED** (help.preply.com/articles/4179135).
- Password/email-change mechanics: a distinct help article is referenced in Preply's own IA but was not independently fetched — **NOT VERIFIED** in detail.
- **NEW, not previously known in this project:** Preply has a student-facing **Google Calendar integration**, not only a tutor-facing one — **DOCUMENTED** (surfaced via the Help Center IA listing). Depth not investigated.

## Trial Booking

Full detail in `11-payments-subscriptions-and-economy.md` (payment specifics) and `08-state-machines.md` (Booking state machine). Summary, all **DOCUMENTED** unless noted:

- 25 or 50 minutes; duration cannot be changed once booked.
- Date/time chosen from the tutor's real availability; timezone-aware.
- Trial lessons are **paid lessons** — paid at booking, directly or from balance.
- One trial per tutor per student.
- A new, **OBSERVED** finding from the real public-profile pass: a tutor profile displays "Not a match? You still have 2 free tutor trials" — raw observation only; meaning not interpreted (see `16-product-decisions-and-open-questions.md`).
- Messaging a tutor is explicitly encouraged post-booking; contacting a tutor before booking a trial is possible but policy-discouraged specifically around sharing contact details/external links — **DOCUMENTED** (help.preply.com/articles/4179497).

## My Lessons / Schedule

- Desktop label: "My lessons (Upcoming)"; app label: "Schedule" — **DOCUMENTED** (help.preply.com/articles/13924649).
- Reschedule/cancel both governed by the 12-hour rule (full detail in `09-edge-cases.md`).
- Exact "Past" tab naming and "Join lesson" button placement: **NOT VERIFIED**.
- Lesson auto-confirmation timing (≈15 min post-end inside Classroom; 72h otherwise unless the student confirms sooner) — **DOCUMENTED** (help.preply.com/articles/4171348). This is a genuinely new finding versus any prior TutorFlow spec — TutorFlow's `Lessons` module currently has no completion/confirmation logic at all (see `13-tutorflow-current-state-and-parity.md`).

## Classroom → Completion → Review

See `12-classroom-messaging-reviews-notifications.md` for full Classroom detail. Review eligibility begins after the **3rd completed lesson with that tutor** — **DOCUMENTED** (prior official research, this project).

## Subscriptions

See `11-payments-subscriptions-and-economy.md` for the full economy detail. The pre-existing `docs/product/SUBSCRIPTION_JOURNEY.md` explicitly left pricing/cycle-length/rollover as `[OPEN — Product Owner]`, treating the 28-day cycle as "current product direction/inspiration, not a confirmed rule." This project's later research **confirmed the 28-day cycle, 4-lesson minimum, and renewal mechanics as DOCUMENTED Preply fact**, not merely an inspiration — this upgrades that older document's uncertainty, without editing the older file itself.

## Messaging / Notifications / Payment History / Balance

See `12-classroom-messaging-reviews-notifications.md` and `11-payments-subscriptions-and-economy.md`. Dedicated "Payment history/receipts" and "Balance" pages are confirmed to **exist in Preply's Help Center IA** but their exact page structure is **NOT VERIFIED** — no article was fetched describing their layout.

## Help / Support

Support is chat/email only (no phone/video); desktop entry via a "?" icon, app entry via Settings → Customer Support; `support@preply.com`; stated 24/7 availability — **DOCUMENTED, medium confidence** (search-summarized from an official article title, not independently fetched and read in full).

## States (per stage, condensed — see the pre-existing `STUDENT_JOURNEY.md` for the original, still-valid "states to design for" list)

The original document's empty/error/permission/validation state list (no search results, no favorites yet, payment failure mid-checkout, slot lost to a race, Guest trying to message/book) remains valid and is not superseded — none of this project's new research contradicts it. The new, confirmed addition: an **expired booking hold** must be communicated honestly (TutorFlow's own existing implementation already does this correctly — see `13-tutorflow-current-state-and-parity.md`).

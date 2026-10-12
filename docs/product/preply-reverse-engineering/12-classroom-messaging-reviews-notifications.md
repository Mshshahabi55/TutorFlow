# Classroom, Messaging, Reviews, Notifications

None of the four areas in this document were directly browser-observable (all require an authenticated account and, for Classroom, an actual scheduled lesson). Every finding is **DOCUMENTED** or **NOT VERIFIED**, with one **OBSERVED** exception noted explicitly (review subcategory display on the public profile).

## Classroom

### Student-side — DOCUMENTED (help.preply.com/articles/4182666)

- Chat (bottom nav) and a separate "Messenger" (opens chat in its own tab, reachable from the header)
- Video call with the tutor
- Screen share (desktop Chrome/Safari only, per this article)
- File upload via a paperclip icon in chat
- Whiteboard (desktop-recommended, not hard-blocked on mobile — see `10-mobile-responsive.md`)
- Mute audio / disable video buttons
- Vocabulary section, Notes section (bottom nav)
- Learning materials library (English/Spanish only)
- Vocabulary flashcards (English/Spanish only)
- Pre-join Lobby with device checks
- "Join lesson" button (activates as the lesson approaches)
- Get-help troubleshooting guides, "Contact our team," "Report an issue"
- Lessons auto-confirm 15 minutes after completion

### Tutor-side — DOCUMENTED (help.preply.com/articles/4984620)

- Lesson planning via built-in courses/lesson plans; a 3-step flow (enter classroom → select course/topic → notify students of prep work)
- "Tools → Lesson activities" to review/change the assigned course topic; can suggest one if the student hasn't picked
- Access to Preply's lesson-materials library, including content outside the student's own assigned course
- Same Tools menu (vocabulary, chat, notes, whiteboard) available in both full-screen-video and lesson-activity display modes

### Not yet investigated: TalkNow, Lesson Insights

Both confirmed to exist by name (surfaced incidentally while researching the student Help Center IA) but never investigated in depth. **DOCUMENTED (existence only) / NOT VERIFIED (everything else)** — flagged for a dedicated future research pass, not guessed at here.

### TutorFlow's current state

Classroom does not exist in TutorFlow in any form — zero code, zero stub beyond the module shell. This is confirmed a **P0 gap** (the actual tutoring-delivery experience) in `13-tutorflow-current-state-and-parity.md` and `15-dependency-graph-and-roadmap.md`.

## Messaging

- Encouraged post-trial-booking to share goals/expectations; pre-trial contact is possible but policy-discouraged specifically for sharing contact details/external links — **DOCUMENTED** (help.preply.com/articles/4179497).
- In-Classroom chat and a separate "Messenger" view both exist, as noted above.
- Blocking mechanics (3-dot menu on messages tab, reason required): **DOCUMENTED**, see `04-platform-trust-and-safety.md`.
- Attachments: confirmed in-Classroom (file upload via paperclip icon). Whether the standalone Messages/Messenger feature (outside a live lesson) also supports attachments: **NOT VERIFIED**.
- Unread-state mechanics, conversation-list layout, archiving: **NOT VERIFIED**.

TutorFlow's `Messaging` module is a pure stub; the frontend shows an honest "Coming Soon" placeholder rather than a fake inbox (confirmed by direct code inspection in an earlier audit this project).

## Reviews

- Eligibility: after the student's **3rd completed lesson** with a tutor; Preply emails an invite at that point — **DOCUMENTED** (prior research, this project).
- Submission: a public text review, postable from Messages ("Post review" on a conversation) or directly from the tutor's profile — **DOCUMENTED**.
- Four subcategory dimensions — **DOCUMENTED, and now also OBSERVED directly on a real public profile** (upgraded this project): Clarity, Progress, Preparation, Reassurance, each with Preply's own stated description (explanations simple/structured/easy to follow; support building skills/tracking growth; organized/focused lessons with a plan; encouragement/patience/emotional support).
- **Subcategory submission mechanics — DOCUMENTED** (S26, re-fetched in full by an independent parity audit; previously NOT VERIFIED). A genuinely **separate flow from the written review**: "once per month, after a completed lesson, learners see a short, optional prompt" — base eligibility is **≥1 completed lesson with that tutor, not the written review's 3-lesson threshold**. Each of the four dimensions is independently optional ("rate all, some, or none"), 1–5 star scale, **at most one submission per tutor per calendar month**, and **not student-editable afterward** ("can only be reviewed if a student has violated Preply's policies" — Support-mediated only, unlike the written review's self-service edit right). Public display is an **aggregate**: "an average star rating and indicates how many anonymous student reviews it is based on" per dimension — never raw per-review subcategory scores.
- Display/ranking: ranked by submission date plus additional criteria (e.g. language) — **DOCUMENTED**.
- Editing (written review only): students can edit their own review via Messages — **DOCUMENTED**.
- Tutor response: a visible "respond" action exists on a new review — **DOCUMENTED** (help.preply.com/articles/6231606).
- Dispute: subcategory scores removable/challengeable only via Support, on policy-violation grounds — **DOCUMENTED**. No general self-service dispute right for either party beyond the student's own written-review edit right.
- **Public display visibility threshold — DOCUMENTED** (S26, re-fetched a second time, targeted): *"Subcategory reviews appear on your profile once you've received at least one rating in all four categories: Clarity, Progress, Preparation, and Reassurance. If you only have ratings in some categories, the section will not appear yet."* The section is all-or-nothing — never a partial 1-, 2-, or 3-of-4 public display, regardless of how much real data exists for the populated dimensions.
- Whether a tutor sees a subcategory breakdown on their **own** dashboard, distinct from the public profile display confirmed above: **NOT VERIFIED**.
- Exact "once per month" boundary semantics (calendar month vs. rolling 30-day window): **NOT VERIFIED** — the article says "once per month" without specifying which; TutorFlow's implementation uses calendar-month (UTC) as the most literal, defensible reading, explicitly flagged as an engineering interpretation, not an observed exact mechanism.

**STATUS: IMPLEMENTED (Product Owner decision — the documented Preply model, not an intentional deviation), for both the written review and the subcategory ratings.**

**Written review**: real domain model, persistence, and tested API. Eligibility is exactly the DOCUMENTED rule: the 3rd completed lesson with that specific tutor (derived from `Lesson.Completed`, never a client-supplied count), and there is exactly one editable review per Student↔Tutor relationship — never a per-Lesson review, enforced by a DB-level unique index on `(StudentUserId, TutorUserId)`. Submitting again edits the same row. Display shows rating, optional comment, and date — matches `4.9/5 (39 reviews)`-style OBSERVED evidence structurally. **Reviewer identity is intentionally always anonymous in this slice** (no `authorName`) — Preply shows a real *mix* of named/anonymous reviewers (OBSERVED), but nothing in this project's evidence establishes how that choice is made, and TutorFlow has no mechanism anywhere to resolve a student's public display name; fabricating one was rejected. **PARTIALLY MATCHED / SCOPED SIMPLIFICATION**, not MATCHED.

**Subcategory ratings** (added in a later pass, after the submission mechanics above were re-verified from the official source): real, separate `SubcategoryRating` entity (never attached to the written `Review` row), real eligibility (≥1 completed lesson, DB-enforced once-per-calendar-month via a unique index on `(StudentUserId, TutorUserId, SubmissionMonthKey)`), real upsert-free submission (each eligible month is a new, immutable row — never edited, matching the DOCUMENTED Support-only dispute path), and real public aggregate display (`GetTutorReviewSummary.SubRatings`, per-dimension average + count, `null`/`0` when a dimension has no data yet — never fabricated). **The backend intentionally keeps returning partial nullable data even when incomplete** (useful internally / for a future tutor-dashboard breakdown); **the frontend enforces the real DOCUMENTED all-four-or-nothing public visibility rule** — the "Specific aspects" section never renders on the tutor profile unless all four dimensions have `count > 0` (corrected in a follow-up pass after the visibility threshold was independently re-verified against the official source, quoted above). **MATCHED** for eligibility rule, submission optionality/scale, once-per-month cap, non-editability, aggregate display shape, and the all-four-populated visibility gate. **NOT VERIFIED** (unchanged, not invented): the exact calendar-vs-rolling-window boundary, whether a tutor sees this breakdown on their own dashboard, and visual/pixel parity (no TutorFlow browser/screenshot evidence exists for this UI — DOM-level test assertions only).

Submission-via-Messages (the written review's other DOCUMENTED entry point, "Post review" on a conversation) remains `BLOCKED` — Messaging is still a stub. Tutor response to a review (DOCUMENTED) remains `NOT VERIFIED`/not built — out of this slice's scope.

## Notifications

- A dedicated Settings → Notifications page exists (preply.com/en/settings/notification pattern), with "Save settings" and "Unsubscribe from all" — **DOCUMENTED** (help.preply.com/articles/4179135).
- Specific notification *types/categories* and delivery *channels* (email/push/SMS/in-app) are **not documented by the article actually fetched this project** — a sibling article title ("How to set up email notifications") is known to exist but was not fetched. A separate, non-fetched search snippet claimed browser push and SMS are both supported — **NOT VERIFIED**, treat as unconfirmed.
- Native-app-only push-notification timing (24h/1h/10min before a lesson): **NOT VERIFIED** — search-synthesized only, not an official article directly read.
- **OBSERVED directly (strongest evidence tier), via real email screenshots** (`18-screenshot-evidence.md` Part C, SS49–SS52): Preply sends at least two real transactional email types. (1) A **tutor-message notification** email ("You have a new message — {Tutor} sent you a message"), which also bundles unsolicited trial-lesson education content (a 25-min vs. 50-min value-proposition explainer) into the same send. (2) An **abandoned-checkout recovery email** ("{Tutor} is ready for your lesson"), explicitly labeled "Abandoned slot," referencing the exact date/time of a real, uncompleted trial-booking checkout, reusing the same personalized relevance copy shown at checkout itself, with a tracked CTA link on a dedicated `ablink.tips.preply.com` subdomain. Delivery channel confirmed: email. Push/SMS/in-app equivalents remain unconfirmed either way.

**STATUS: IMPLEMENTED for the abandoned-checkout slice (ADR-0029).** `Notifications` now has real transactional email infrastructure (Postmark, behind a provider-neutral `IEmailSender`) and a real, idempotent abandoned-checkout recovery email, triggered by a new time-driven `Bookings.BookingHoldExpirySweepService` and delivered via a new `BookingHoldExpired` Wolverine event — proven end-to-end in `TutorFlow.Host.Tests`. The tutor-message notification email (the other real email evidenced in Part C, SS49–SS50) remains unbuilt — out of this slice's scope, a separate roadmap item depending on the still-stub `Messaging` module. See `13-tutorflow-current-state-and-parity.md` and `18-screenshot-evidence.md` Part C, and ADR-0029 for the full design and evidence-vs-engineering-decision boundary.

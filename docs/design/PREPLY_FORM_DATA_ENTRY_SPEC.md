# Preply Form & Data-Entry Spec

Companion to `PREPLY_UI_UX_AUDIT.md`. Focuses specifically on forms and field-level data-entry behavior. Evidence basis: public homepage fetch, Preply help-center articles (registration, trial booking, availability setup, profile editing), and third-party reviewer summaries — no authenticated session or screenshot tool was available (see audit §24). Every row not backed by a specific source is marked accordingly; nothing here invents validation rules, limits, or defaults that weren't actually described in a source.

---

## Form 1: Account registration (student and tutor)

| Field | Type | Required | Interaction | Validation | States | Mobile behavior | TutorFlow equivalent |
|---|---|---|---|---|---|---|---|
| Email | text/email | Required | Single-line text input | **Not reliably observable** (format validation assumed standard, not confirmed) | default/error (assumed, unconfirmed) | **Not reliably observable** | `Identity` registration — `Email` field |
| Password | password | Required | Single-line masked input | **Not reliably observable** (min length/complexity rules not sourced) | default/error (assumed) | **Not reliably observable** | `Identity` registration — `Password` field |
| OAuth (Google/Facebook) | button | Optional alternative to email/password | Click → external OAuth redirect | n/a | default/loading (redirect) | **Not reliably observable** | `Identity` — social login (not yet in TutorFlow's scope; `[OPEN — Product Owner]`) |

**Confirmed constraint (tutor-specific, help-center sourced):** tutor registration is described as desktop-only ("Currently, it is possible to register as a tutor only in the desktop version"). This is a Preply platform limitation, not a UX best practice — **do not carry this constraint into TutorFlow** without an explicit TutorFlow product reason; flagged as a one-way fact, not a pattern to copy.

---

## Form 2: Tutor application (8 sections — treated as one logical form; see audit §27.2 for the step-vs-section ambiguity)

All rows below are sourced from Preply's own help-center "Register to teach on Preply" article plus the per-section help articles it links to (profile headline/description guidelines, video introduction guidelines, availability setup). Anything not explicitly stated in those sources is marked **"Not reliably observable"** rather than inferred.

| Section | Field | Type | Required | Interaction | Validation | Helper/error text | TutorFlow equivalent |
|---|---|---|---|---|---|---|---|
| 1. Profile picture | Photo | image upload | Required | Upload | guidance: "professional-looking photo that shows your friendly face" — a content guideline, not a technical validation rule; file size/type limits **not reliably observable** | **Not reliably observable** | `Tutors` — profile photo field |
| 2. Certifications | Certificate(s) | file upload, repeating | Optional (assumed — **not confirmed required**) | Add/remove entries (repeating-entry pattern assumed from "section" framing, **not literally confirmed as add/remove UI**) | **Not reliably observable** | **Not reliably observable** | `Tutors` — certifications list |
| 3. Education | Degree/diploma entries | structured text + possibly file upload, repeating | **Not reliably observable** whether required | Add/remove entries (assumed) | **Not reliably observable** | **Not reliably observable** | `Tutors` — education list |
| 4. Profile description | Headline | short text | Required (implied — described as essential to attracting students) | Single-line text | guidance only: "powerful, engaging, keyword-rich" — a copywriting tip, not a field-level rule | **Not reliably observable** | `Tutors` — profile headline |
| 4. Profile description | Description body | textarea | Required (implied) | Multi-line text | **No character limit sourced** — do not invent one | **Not reliably observable** | `Tutors` — profile description |
| 4. Profile description | Per-subject description | textarea, repeating (one per subject taught) | Required per subject taught (implied) | Multi-line text, tied 1:1 to selected subjects | **Not reliably observable** | **Not reliably observable** | `Subjects`/`Tutors` — per-subject description |
| 5. Intro video | Video file | video upload | Required (implied — described as expected, not explicitly "optional") | Upload or in-browser recording (**recording-vs-upload-only not confirmed**) | guideline: "kept under 2 minutes long" — stated as guidance, closest thing to a hard rule sourced for this form | **Not reliably observable** | `Tutors` — intro video |
| 6. Calendar/availability | Weekly availability | recurring time-slot grid (type **not confirmed**, "Calendar section" is the only sourced label) | Required (implied — booking depends on it) | **Not reliably observable** | **Not reliably observable** | `Availability` module — weekly recurring rules |
| 6. Calendar/availability | Timezone | select | Required | Single select | **Not reliably observable** | **Not reliably observable** | `Availability` — timezone field |
| 7. Tutoring rate | Hourly rate | number/currency input | Required | Numeric entry; help-center confirms it is **adjustable later** after launch | **No min/max sourced** — do not invent | **Not reliably observable** | `Tutors` — pricing, feeds `ITutorPricingQuery` |
| 8. (unconfirmed 8th section) | — | — | — | — | — | — | Source material referenced "8 sections" but only enumerated 7 distinctly; the 8th is **not reliably observable** and must not be guessed at |

**Submission:** a single submit action triggers the ~5-business-day review (help-center confirmed); no autosave/draft behavior was described in any source — **"Needs product decision"** for whether TutorFlow's own wizard persists drafts (the wizard architecture doc recommends yes, independent of whether Preply does it).

---

## Form 3: Trial booking checkout

| Field | Type | Required | Interaction | Validation | States | Mobile behavior | TutorFlow equivalent |
|---|---|---|---|---|---|---|---|
| Duration | single-select (25 or 50 min) | Required | Two-option choice, exactly one of two fixed durations (confirmed — not freely variable) | Locked after confirmation (confirmed: "durations cannot be modified after booking confirmation") | selected/unselected | **Confirmed platform difference:** on web, duration is chosen before date/time; on the mobile app, payment method is chosen first, then duration, then date/time | `Bookings` trial-hold creation — duration input |
| Date/time slot | calendar/slot picker | Required | Select one open slot from the tutor's availability | Must be a currently-open slot (implied — slots are filtered to availability) | available/selected/(unavailable — implied, not explicitly described) | Same platform-order difference as above | `Bookings` — backed by `ITutorAvailabilityQuery` + `IActiveBookingIntervalsQuery` |
| Payment method | select/radio (new method vs. existing balance) | Required | Choose a stored method, add a new one, or apply an existing lesson-credit balance with that tutor | **Not reliably observable** (card-form validation detail not sourced) | **Not reliably observable** | Chosen earlier in the app flow than on web (confirmed order difference) | `Payments` — `clientSecret`/Stripe Elements step, per `ADR-0019` |
| Final confirm | button ("Schedule lesson") | Required | Single click/tap finalizes | n/a | default/loading/success (implied by "lesson appears in My Lessons") | **Not reliably observable** | `POST /api/bookings/{id}/checkout` |

**Confirmed business rule (not a UI detail, but attached to this form):** one trial lesson per tutor, maximum. Reschedule/cancel requires ≥12 hours' notice to return the credit to the student's balance with that tutor; under 12 hours forfeits it. **This is Preply's own rule and is intentionally different from TutorFlow's already-locked 24-hour cancellation-refund window (`ADR-0015` §6) and 12-hour reschedule-notice window (`ADR-0017`)** — do not alter TutorFlow's locked business rules to match Preply's numbers.

---

## Forms identified but not detailed this session (existence confirmed or strongly implied, field-level detail **not reliably observable**)

| Form | Where | Why not detailed |
|---|---|---|
| Tutor search / filters | Find Tutors results page | Direct fetch returned 404 this session; no field-level detail sourced |
| Message composer | Tutor profile → Message | Requires authenticated session |
| Review submission | Post-lesson, student-side | Not sourced in any fetched/searched material this session |
| Profile settings (student/tutor) | Account settings | Not sourced; fully auth-gated |
| Subscription package picker | Post-trial upsell | Pricing/cadence confirmed (§ cross-ref `PREPLY_UI_UX_AUDIT.md` §11) but the picker's actual form fields/controls were not sourced |

## Summary

**Forms audited with field-level detail: 3** (registration, tutor application, trial-booking checkout). **Forms identified but not detailed: 5.** Every validation rule, limit, and default stated above is traceable to a specific fetched source; anything not stated is explicitly marked rather than invented, per the task's own constraint.

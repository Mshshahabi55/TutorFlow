# Tutor Onboarding Wizard — Approved Structure

**Status:** Specified — resolves the open mechanism/ownership questions `TUTOR_JOURNEY.md` and `ADR-0012` left open. Not yet implemented.

## 1. Authority and scope

The wizard is a **frontend orchestration mechanism only**. It is not a backend module and introduces no `Wizard` bounded context — per `docs/design/TUTORFLOW_WIZARD_ARCHITECTURE.md`'s recommendation, now adopted. It coordinates writes into the existing modules that already own each piece of data: `Tutors`, `Availability`, and (read-only, for picker options) `Subjects`.

**The 17-step structure in `TUTOR_JOURNEY.md` is authoritative and is not changed by this spec.** The Preply audit's own wizard-architecture doc independently proposed a shorter, 14-item step list that drops "Experience" and folds "Verification"/"Published" into post-submission states — that proposal is **not adopted**: `TUTOR_JOURNEY.md`'s list is an existing, already-specified product decision ("17 steps, as specified"), and per this task's own rule, an existing decision is not silently changed because an audit suggests something different. See the final report's "conflicts discovered" section.

## 2. Step → module mapping

| # | Step | Data owner | Mechanism |
|---|---|---|---|
| 1 | Account | `Identity` | Existing signup/login; not wizard-specific |
| 2 | Basic Information | `Tutors` (`TutorApplication`) | Draft write |
| 3 | Photo | `Tutors` | Draft write + blob/CDN upload reference |
| 4 | Languages | `Tutors` (`TutorSpokenLanguage`, `ADR-0020`) | Draft write; picker options read from `Subjects`' enabled-languages reference list |
| 5 | Subjects | `Tutors` (`TutorSubject` join, stores `SubjectId` as a value, not a FK) | Draft write; picker options read directly from `Subjects`' public catalog endpoint |
| 6 | Specialties | `Tutors` (`TutorSpecialty` join) | Same pattern as Subjects |
| 7 | Experience | `Tutors` (`TutorExperienceEntry`, repeating) | Draft write |
| 8 | Education | `Tutors` (`TutorEducationEntry`, repeating) | Draft write |
| 9 | Certificates | `Tutors` (`TutorCertificate`, repeating) | Draft write + file upload |
| 10 | Intro Video | `Tutors` | Draft write + blob/CDN upload reference — a stored *asset*, unrelated to `Lessons`' live classroom video (`ADR-0006`); do not share a provider abstraction between the two |
| 11 | Pricing | `Tutors` | Draft write — confirmed `Tutors`-owned (`ADR-0012`) |
| 12 | Availability | `Availability` (`AvailabilityRule`, `TimeOff`) | **Resolved below (§3)**: direct frontend → `Availability` API call, not relayed through `Tutors` |
| 13 | Booking Settings | `Availability` (`BookingSettings`, `ADR-0012`) | Same as step 12 |
| 14 | Preview | None (frontend-only) | Assembles already-persisted draft data from `Tutors` + `Availability`; no new write |
| 15 | Submit | `Tutors` (`TutorApplication` state transition) | Publishes `TutorApplicationSubmitted` → `Notifications` |
| 16 | Verification | `Tutors` + Admin (composed admin, `ADR-0008`) | Gating criteria remain `[OPEN — Product Owner]` (`OPEN_QUESTIONS.md` #9/#10) — not resolved here |
| 17 | Published | `Tutors` | Publishes the existing `Published` event (`ADR-0011`) |

## 3. Resolving `ADR-0012`'s open mechanism (steps 12–13)

`ADR-0012` settled that `Availability` owns `BookingSettings` but left open *how* the wizard's steps 12–13 input reaches it. Resolved here: **the wizard frontend calls `Availability`'s own self-service write endpoints directly** (the same endpoints a tutor would later use to edit availability post-publish — `DOMAIN_MODEL.md` already notes every `Availability` endpoint is self-service, caller's-own-data-only). The wizard does not relay this data through `Tutors`; there is no new endpoint and no event hop for this step specifically. `Tutors`' existing `AvailabilitySettingsConfigured`/`BookingSettingsConfigured` boolean flags (`ADR-0012`) are what the wizard's own resume logic (§4) checks to know these two steps are done — `Tutors` sets them itself by consuming an integration event `Availability` publishes when a tutor's rules/settings are first configured (same event-driven pattern as every other cross-module signal in this system, no new mechanism invented).

This ADR-0012 open item is now closed; `ADR-0012` is not rewritten, this spec supersedes only its "not decided here" caveat.

## 4. Draft persistence, resume, and validation

- **Draft aggregate**: `TutorApplication`, owned by `Tutors`, exactly as `TUTOR_JOURNEY.md`'s own `[OPEN — Architect]` recommendation proposed — now formally adopted, not re-designed. It holds a `FurthestCompletedStep` (int) and per-step completion flags (including the two `Availability`-backed flags from §3).
- **Resume**: on wizard load, the frontend fetches `TutorApplication`'s current step/flags from `Tutors` and routes the tutor to the furthest incomplete step. No separate "wizard session" entity.
- **Expiry**: a draft does not expire in V1 (`OPEN_QUESTIONS.md` #8 asked about this — resolved as "no expiry for V1," consistent with not inventing a cleanup policy nobody asked for; revisit if abandoned-draft volume becomes a real operational problem).
- **Validation boundary**: each step's fields are validated by whichever module owns that step's data (`Tutors` via FluentValidation for steps 2–11, `Availability`'s own existing validation for steps 12–13) — there is no wizard-level validation service. Client-side validation mirrors the server rules for immediate feedback but the server is always authoritative (`error-handling` skill).
- **Autosave**: per-step, on step-exit (not per-keystroke) — each step's "Next" action is itself the save; there is no separate autosave timer/endpoint.
- **Network/browser-refresh recovery**: because every step's data is persisted on exit (not held only in frontend state), a refresh or network drop loses at most the current, unsaved step — consistent with "server-authoritative drafts," not a client-side draft store.

## 5. Submission, review, rejection/resubmit, publish gating

- **Submit (step 15)** is only enabled once every prior step's completion flag is set; `Tutors` enforces this server-side (the frontend disabling the button is a UX convenience, not the actual guard).
- **Review state**: admin rejection (mechanism/criteria `[OPEN — Product Owner]`, `OPEN_QUESTIONS.md` #9/#10) sets `TutorApplication` back to an editable state with a stored rejection reason; the tutor re-enters the wizard at the first step needing correction (not necessarily step 1), edits, and resubmits — this loops back to step 15, it does not restart the whole wizard.
- **Publish gating**: `Tutors` only publishes (step 17, `ADR-0011`'s event) after admin approval; no soft-launch-with-pending-certificate path exists until `OPEN_QUESTIONS.md` #10 is resolved.
- **Deep-link/unauthorized access**: a direct link to a specific step is only honored if `FurthestCompletedStep` allows it; otherwise the tutor is redirected to their furthest valid step (not an error page) — consistent with `TUTOR_JOURNEY.md`'s own "Permission" state note.

## 6. What this spec does not decide

- Verification gating criteria and one-stage-vs-two-stage review (`OPEN_QUESTIONS.md` #9).
- Publish-with-pending-certificate (`OPEN_QUESTIONS.md` #10).
- Pricing bounds, if any (`OPEN_QUESTIONS.md` #11).

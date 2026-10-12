# Wizard Inventory

## Tutor Application Wizard — THE CENTRAL CONFLICT IN THIS DIRECTORY

Two mutually exclusive step counts exist in this project's own history, from two different sources, and neither is silently resolved here.

### Source A — original product brief (pre-existing, `docs/product/TUTOR_JOURNEY.md` and `docs/product/TUTOR_ONBOARDING_WIZARD_SPEC.md`)

**17 steps**, explicitly locked as "an existing, already-specified product decision" by the onboarding-wizard spec's own text, which also notes that an *internal* audit once proposed a 14-step alternative and that proposal was **explicitly rejected** in favor of keeping the 17-step brief structure:

| # | Step | TutorFlow module (per the existing spec) |
|---|---|---|
| 1 | Account | Identity |
| 2 | Basic Information | Tutors |
| 3 | Photo | Tutors |
| 4 | Languages | Tutors |
| 5 | Subjects | Tutors |
| 6 | Specialties | Tutors |
| 7 | Experience | Tutors |
| 8 | Education | Tutors |
| 9 | Certificates | Tutors |
| 10 | Intro Video | Tutors |
| 11 | Pricing | Tutors |
| 12 | Availability | Availability |
| 13 | Booking Settings | Availability |
| 14 | Preview | (frontend only) |
| 15 | Submit | Tutors |
| 16 | Verification | Tutors + Admin |
| 17 | Published | Tutors |

**Verification of this structure against real Preply: NONE.** This list's origin is the project's original brief, not Preply research — it was never claimed to be Preply-verified, and this directory is the first time that gap has been made explicit.

### Source B — this project's actual Preply reverse-engineering (`03-tutor-journey.md`)

**7 confirmed sections**, DOCUMENTED directly from Preply's own official registration article (help.preply.com/articles/4175138):

| # | Section |
|---|---|
| 1 | Profile Picture |
| 2 | Certifications |
| 3 | Education |
| 4 | Profile Description |
| 5 | Intro Video |
| 6 | Calendar (availability + timezone, combined) |
| 7 | Tutoring Rate |

The article's own text claims "8-section application" but enumerates only 7 — the 8th was, at the time this table was written, **NOT VERIFIED / not identifiable from the official source**. **This gap is now closed — see Source C below.**

Notably absent from Preply's confirmed 7: **Languages, Subjects, Specialties, and Experience as distinct sections** — Preply's own structure appears to fold subject/specialty/language selection into fewer, denser sections than TutorFlow's 17-step assumption does, though the exact internal sub-structure of each of Preply's 7 sections was not itself investigated via official documentation. **Source C below resolves this for the real flow's first step.**

### Source C — real screenshot evidence, `18-screenshot-evidence.md` (OBSERVED, strongest evidence tier in this directory)

A 13-image screenshot pack of a real, in-progress (never-submitted) `preply.com/en/tutor-signup` session directly confirms the **actual real wizard has 8 steps**, in this exact order, with the literal on-screen breadcrumb labels:

| # | Step (as labeled on-screen) | Confirmed content |
|---|---|---|
| 1 | **About** | First name, last name, email, country of birth, subject you teach, languages you speak + proficiency level (repeatable) |
| 2 | **Photo** | Profile photo upload + live preview card of name/subject/languages from step 1 |
| 3 | **Certification** | Optional; subject, controlled certificate list, "not on the list" fallback, years of study, manual-review file upload |
| 4 | **Education** | Optional; university, degree, degree type, specialization, years of study, "Diploma verified" badge upload, repeatable |
| 5 | **Description** | Free-text profile description guided by 4 sub-prompts (introduce yourself / teaching experience / motivate students / catchy headline), soft ~400-char counter |
| 6 | **Video** | Live webcam recording, OR paste a YouTube/Vimeo link, OR (implied) file upload; explicit Do/Don't rules; AI-assisted "Create script" aid |
| 7 | **Availability** | Timezone auto-detect + Yes/No confirm, then a weekly From/To schedule (all 7 days default-enabled, 09:00–17:00, multiple timeslots per day) |
| 8 | **Pricing** | Single 50-minute-lesson price in USD only, collapsible "Preply commission" disclosure, Corporate Tutor Program opt-out notice, **"Complete registration"** — no separate review/summary step exists anywhere in the flow |

This **resolves the "unidentified 8th section" question**: the 8th section is simply the first one, **"About"** — the help article's own prose undercounted it, not because it is hidden or later in the flow. It also directly confirms (OBSERVED, not DOCUMENTED) that **Certification and Education are two genuinely separate steps**, matching TutorFlow's own pre-existing code comment ("Preply sections 2+3" in `CredentialsStep.tsx`) which had previously only inferred this from the DOCUMENTED help-article text. Full per-screenshot detail, including exact field labels, helper text, and TutorFlow-side parity comparison, lives in `18-screenshot-evidence.md`.

**One newly-discovered, previously-undocumented divergence**: Preply's real flow has **no distinct Review/Summary step** — Pricing (step 8) leads straight to "Complete registration." TutorFlow's own wizard has an explicit `ReviewStep.tsx` as its 8th step, which Preply's real flow does not appear to have an equivalent of. This is listed as a new item in `16-product-decisions-and-open-questions.md`, not resolved here.

### What this means, and what it does not mean

This does **not** mean TutorFlow's 17-step *brief* (`TUTOR_ONBOARDING_WIZARD_SPEC.md`, Source A) is "wrong" — it means it was never actually benchmarked against Preply, and now there is real evidence to benchmark it against. Resolving the conflict (collapse to 7 sections? keep 17 with Preply's 7 as a loose reference? something between?) is a **Product Owner decision**, listed in `16-product-decisions-and-open-questions.md`. This directory does not choose for them.

**Important correction, confirmed by reading the actual current frontend code (`apps/web/src/features/tutor-onboarding/`), not the brief:** TutorFlow's *implemented* wizard already has **8 steps today** — `basics`, `photo`, `teaching`, `credentials`, `video`, `availability`, `rate`, `review` (`types.ts`, `WIZARD_STEPS`) — not 17. The 17-step structure exists only in the original, never-built-against brief document; the real, running code already independently converged close to Preply's real 8-step count, years before this screenshot evidence existed. The remaining real gap is not step *count* but step *content/order*: TutorFlow's `teaching` step (subjects/languages/country-of-birth) corresponds to part of Preply's step 1 ("About"), not a later step, and TutorFlow's `basics` step (display name/headline/about-text) corresponds more closely to Preply's step 5 ("Description") than to Preply's step 1. TutorFlow also has a `review` step Preply's real flow does not have. Full field-by-field comparison is in `18-screenshot-evidence.md`.

### Field-level enforcement — already independently audited this project (unaffected by the step-count conflict)

A separate, earlier TutorFlow-code audit this project found that of the 7 confirmed Preply sections, TutorFlow's actual backend (`TutorApplication.GetSubmissionReadinessErrors()`) only **backend-enforces 2 of them** (Photo, Rate) as required for submission — Video, Availability/Calendar, Certifications, Education, and Description all have frontend UI but no backend requirement. This finding is independent of the step-count conflict and remains valid; see `13-tutorflow-current-state-and-parity.md`.

### Draft / resume / navigation mechanics

The existing `TUTOR_ONBOARDING_WIZARD_SPEC.md` specifies: `TutorApplication` as the draft aggregate with a `FurthestCompletedStep` field and per-step completion flags; resume-to-furthest-incomplete-step on reload; no draft expiry in V1; per-step (on step-exit) persistence, not per-keystroke autosave; server-authoritative validation with client-side mirroring for UX only; deep-link access only honored up to `FurthestCompletedStep`. **None of this was verified against real Preply** — it is a TutorFlow-side architectural decision, not a Preply-benchmarked one, and is presented here as such (this directory does not have independent Preply evidence for draft/resume behavior at all — **NOT VERIFIED** on the Preply side).

### Submission → Review → Resubmission → Publish

- Review timing: **conflicting official sources** — "3 business days" (Become-a-Tutor marketing page) vs. "5 business days" (registration help article). Both **DOCUMENTED**, neither resolved.
- Rejection reasons (**DOCUMENTED**): profile doesn't meet requirements, applicant under 18, low subject demand, residency in a sanctioned country (Iran, Sudan, Cuba, Syria, North Korea, Crimea, Russia, Belarus).
- The exact resubmission UI/flow (does the tutor re-enter at a specific step, is there a stored rejection reason shown inline): **NOT VERIFIED** against real Preply — the existing TutorFlow spec's assumption (re-enters at the first step needing correction, loops back to Submit) is a TutorFlow design decision, not Preply-sourced.
- Publish gating (admin approval required, no soft-launch-with-pending-certificate path): consistent with the DOCUMENTED review-then-publish sequence, but the exact gating mechanism inside Preply is **NOT VERIFIED**.

## Other wizards

No other multi-step wizard was confirmed to exist in Preply's product during this project's research. Student onboarding is explicitly **not** a forced wizard (DOCUMENTED — reviewing account info and setting timezone is "recommended," not required).

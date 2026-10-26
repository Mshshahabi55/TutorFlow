# TutorFlow Become-a-Tutor Wizard — Live Verification and Parity Matrix

**Date:** 2026-10-18. **Methodology:** live Playwright MCP browsing of both `preply.com` and a local
TutorFlow dev instance (`http://localhost:3000` frontend, `http://localhost:5148` backend), not
code reading alone. Screenshots in `evidence/tutorflow-wizard-live-2026-10-18/`.

## 1. Public vs. authenticated boundary — verified, not assumed

Per this task's explicit instruction not to assume an 8-step *public* wizard: **there is no public
multi-step wizard on either site.**

- **Preply**: `https://preply.com/en/teach` (marketing) → `/en/teach/signup` (single-step account
  creation: email/password or Google/Facebook) is the entire public surface. Navigating directly to
  `https://preply.com/en/tutor-signup` (the real 8-step wizard URL, per the existing
  `18-screenshot-evidence.md` pack) **redirects an unauthenticated visitor to `/en/teach`** —
  re-confirmed live this session, same result as every prior check. The wizard is 100%
  authenticated-only.
- **TutorFlow**: `http://localhost:3000/become-a-tutor` for a guest shows **only** an intro card
  ("Become a tutor" heading, one paragraph, "Sign up to apply"/"Log in" links) — zero step content,
  zero progress indicator. Confirmed live via Playwright snapshot. The 8-step wizard
  (`basics → photo → teaching → credentials → video → availability → rate → review`) only renders
  once authenticated **and** a `Draft` `TutorApplication` exists (created by the "Start
  application" button, which calls `becomeTutor()` then `createApplication()`).

This matches Preply's real structure exactly — not a TutorFlow approximation.

## 2. Preply's actual wizard content — relied on existing real evidence, not re-browsed

The wizard itself remains genuinely inaccessible without a real Preply account (same
Terms-of-Service/real-reviewer-time concern documented in `00-scope-and-methodology.md`). Per this
task's own instruction ("do not create real accounts... do not invent behavior for inaccessible
steps"), its content below is drawn from the existing `18-screenshot-evidence.md` pack — a 13-image
set captured by a real person, the strongest evidence tier this project uses — **not re-verified
live this session**, and not claimed as such.

Preply's real 8 steps (OBSERVED, prior session): **About** (name/email/country/subject/languages) →
**Photo** → **Certification** (optional) → **Education** (optional) → **Description** (4
sub-prompts) → **Video** → **Availability** (timezone + weekly schedule, 7-day/09:00–17:00 default)
→ **Pricing** → "Complete registration" (no distinct review/summary step).

## 3. TutorFlow's actual wizard — live-verified this session (new evidence)

Signed up a real test account (`wizard-parity-test@example.local`) on the **local dev instance only**
(not Preply) and walked the live wizard:

| Step | Live-verified behavior |
|---|---|
| Entry | "Start application" → real `POST /api/identity/me/become-tutor` + `POST /api/tutors/applications`, lands on step 1 with a minimal header (logo + Help + avatar, no main nav) — matches Preply's documented minimal wizard chrome (`18-screenshot-evidence.md` SS06). |
| 1. Basics | Display name + Headline (required) + About (optional, 0/4000 counter) + a 4-item "Writing tips" box. "Save & continue" is disabled until required fields are filled — confirmed live (button state changed from disabled → enabled after typing). |
| 2. Photo | Live preview card (initials-avatar fallback showing name+headline from step 1 — matches Preply's SS02 "live preview card" exactly), Upload button, drag-drop + format/size copy (JPEG/PNG/WebP, 5 MB), 4 real example photos with a guidance checklist, "paste a link instead" fallback. |
| 6. Availability | Pre-filled 7-day/09:00–17:00 default template (per-day dropdown + two time pickers + Remove), matching Preply's documented default exactly. |
| 8. Review | Live summary `<dl>` of every field, a client-side "incomplete sections" checklist with deep-linked step names, real `Submit application` button. |
| Resume | Confirmed live: re-navigating to `/become-a-tutor` after partially completing steps lands back on the **first incomplete step** (not step 1), matching the documented resume design. |
| Progress indicator, mobile | Wraps to 2 rows at 390px (✓/current-circle + chevrons, labels visually hidden but still in the accessible-name — confirmed via DOM snapshot, not just a screenshot). **Initially appeared broken** (only row 2 visible) — investigated and confirmed to be a scroll-position artifact from the step transition, not a real bug; scrolling to top shows the full, correct breadcrumb. Documented here so this isn't re-investigated as a false lead later. |

## 4. A real gap found and fixed

Clicking **Submit application** on the live Review step with required fields missing correctly
called the real backend and was rejected (`alert: "One or more errors occurred."` — the actual
`ResultExtensions.ToProblemDetails()` response). But the **client-side "incomplete sections"
checklist did not list the intro video** as missing, even though it was never added — because an
earlier task this session added `IntroVideoReference` to the backend's
`GetSubmissionReadinessErrors()`, and `ReviewStep.tsx`'s own informational `missing` array was never
updated to match. Confirmed via live reproduction (screenshot: `tutorflow-wizard-review-desktop.png`
— video genuinely absent from the warning list despite "Not added" showing in the summary above it).

**Fixed**: `apps/web/src/features/tutor-onboarding/components/ReviewStep.tsx` — added the missing
`if (!application.introVideoReference) missing.push('Intro video (Intro video)')` check, in the same
form as the existing Photo check. Re-verified live (screenshot: `tutorflow-wizard-review-fixed.png`)
— the checklist now correctly lists all 5 real backend requirements.

This was not a Preply-parity gap (Preply's video step's own requirement status isn't independently
verified) — it was an internal-consistency gap between TutorFlow's own frontend and its own backend,
found by actually exercising the live submit path rather than reading code in isolation.

**Separately noticed, not fixed (out of this task's narrow scope):** the actual backend rejection
message shown to the user (`"One or more errors occurred."`) is the response's generic `title`, not
the specific per-field `errors` array — `OnboardingWizard.tsx`'s `errorMessage()` reads
`error.problem.detail`, which the backend's `ResultExtensions.ToProblemDetails()` never sets (only
`title` + an `errors` extension). A tutor who bypasses the (now-accurate) client-side checklist sees
a non-specific error, not "An intro video is required before submission." Fixing this touches a
shared utility used by three call sites (draft-update, reopen, submit) — a larger, cross-cutting
change warranting its own scoped task, not bundled into this one.

## 5. What this task did not re-verify

- Preply's actual Teaching/Credentials/Video/Rate steps were not re-screenshotted live this round
  (budget-prioritized Basics/Photo/Availability/Review as representative of each input type:
  text, file-upload, repeating-structured-input, and summary/submit). Existing screenshot evidence
  and this project's established field-by-field comparison (`07-wizard-inventory.md`,
  `18-screenshot-evidence.md`) remain the source for those.
- Mobile layout was spot-checked (Photo step only) — not exhaustively re-verified across all 8 steps
  at 390×844 this round.

# Comparison With Existing Evidence — 2026-10-18

Baseline being compared against: `docs/product/preply-reverse-engineering/18-screenshot-evidence.md` (Part A, SS01–SS13, manually captured by a real person partway through the real wizard, exact capture date not stated in that doc beyond being part of this project's original research pass) and the official help article `https://help.preply.com/en/articles/4175138-register-to-teach-on-preply` (fetched today, article's own displayed update date: June 17, 2026).

## What could be directly re-verified today — and matches

- The entry point is still **"Become a tutor"** on the main nav / **"Create a tutor profile now"** CTA on `/en/teach` → still lands on a `/en/teach/signup` registration gate before any wizard content. This matches the historical account's description of the overall funnel shape (public page → registration → wizard).
- The registration gate still offers **email/password and Google/Facebook OAuth**, consistent with SS01–SS13's documented use of "a real completed Google OAuth sign-in."

## What could NOT be re-verified today (and why)

- **None of the 8 wizard sections' actual current field layouts, labels, or validation could be re-checked.** The historical SS01–SS13 record remains the only evidence for: About's name/email/country-of-birth/subject/languages fields (SS01 era), Photo, Certification, Education, Description, Video, Availability, Pricing. Since the gate blocks all anonymous access the same way it always has, **this task cannot confirm or deny whether the wizard's content has drifted since the historical capture.** Treat the historical record as potentially stale, not as current ground truth.

## A discrepancy surfaced by the fresh official-help-article fetch (not previously recorded in this project, as far as this task's author is aware)

- The **landing page** (`/en/teach`, freshly captured today as SS53/SS58) states application review takes **"3 business days"** ("2 Get approved — by our team in 3 business days").
- The **official help article** (fetched today) states review takes **"within 5 business days."**
- These two official Preply sources disagree with each other on review timing, as of today. This is a genuine, freshly-observed discrepancy between two live/current Preply sources — not a historical-vs-fresh discrepancy. It should be flagged to whoever owns the "application review" messaging decision for TutorFlow, since neither number can be treated as authoritative without Preply resolving its own inconsistency.

## A gap in today's supplementary documentation evidence (flagged, not resolved)

- The official help article extraction enumerated **7 named wizard sections** (Profile Picture, Certifications, Education, Profile Description, Intro Video, Calendar/Availability, Tutoring Rate) under a header describing an **"8-section form."** It's unclear from this extraction whether "About" (the historical wizard's first section, per SS01) is the unnamed 8th section, folded into "Registration" (step 1), or was simply dropped by the automated extraction tool's summarization. **This is uncertain and should not be treated as evidence that the "About" section no longer exists.** A fresh, direct read of the article (not an automated extraction) or an authenticated wizard walkthrough would be needed to resolve this — both out of scope for this task.

## New evidence this task adds that the historical record did not have

- **OAuth redirect targets and callback URL shape** (`/complete/google-oauth2/`, `/complete/facebook/` — python-social-auth convention) — not previously documented anywhere in this project.
- **Confirmation that the registration form validates only on submit, not on blur** — not previously tested.
- **Confirmation that no CAPTCHA/bot-check is present on the public registration gate** — not previously explicitly tested/stated.

## Net conclusion

Today's capture **adds genuine new evidence about the registration gate** (OAuth mechanics, validation timing, absence of CAPTCHA) but **does not extend, confirm, or refute** the historical record of the 8-section wizard itself. Any product decision that depends on the wizard's *current* field-by-field content should still be understood as resting on evidence that may be out of date, pending either authenticated access or the user's own fresh manual walkthrough.

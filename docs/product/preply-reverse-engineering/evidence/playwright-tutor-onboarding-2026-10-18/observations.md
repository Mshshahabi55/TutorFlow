# Observations — Live Preply Registration Entry, 2026-10-18

Scope note up front: this session reached only the **public registration gate** (`/en/teach`, `/en/teach/signup`). The authenticated 8-section application wizard (About, Photo, Certification, Education, Description, Video, Availability, Pricing) was **not reached** — see `access-limitations.md`. Everything below is about the gate, not the wizard itself.

## Stage: Public marketing landing page (`/en/teach`)

- **Title**: "Make a living by teaching the largest community of learners worldwide"
- Three-step explainer, rendered as numbered cards: "1 Sign up — to create your tutor profile", "2 Get approved — by our team in 3 business days", "3 Start earning — by teaching students all over the world!"
- Primary CTA button text, repeated 4 times down the page: **"Create a tutor profile now"**
- Secondary content: rate-setting pitch ("$15-25 per hour" average for English), flexibility pitch, an FAQ accordion (8 questions, collapsed by default — not expanded this session, out of scope for the registration-gate focus), footer nav.
- Header shows **"Log In"** (not "Log out" or an avatar) — confirms the browsing session is anonymous.
- No CAPTCHA or bot-check widget visible on this page.
- Console: 3–8 errors / 5–9 warnings reported by the browser on each load (not investigated — likely third-party analytics/ad scripts; not reproduced as being related to the registration flow itself).
- Evidence: SS53 (desktop), SS58 (mobile).

## Stage: Registration gate (`/en/teach/signup`)

- **Title**: "Sign up as a tutor"
- Reached by clicking any "Create a tutor profile now" CTA on the landing page — client-side navigation, URL changes from `/en/teach` to `/en/teach/signup`.
- **Exact visible fields/controls**:
  - Button: **"Sign up with Google"**
  - Button: **"Sign up with Facebook"**
  - Divider text: **"or"**
  - Label: "Email" / Input: `<textbox>` with placeholder **"Your email"**
  - Label: "Password" / Input: `<textbox>` with placeholder **"Your password"**, plus a **"Reveal password"** toggle button
  - Checkbox: **"Remember me"**, checked by default
  - Button: **"Get started"**
  - Legal text: "By clicking Continue or Sign up, you agree to **Preply Terms of Use**, including **Subscription Terms** and **Privacy Policy**" (three separate links)
- This is a **single-step form** — email and password fields are both visible simultaneously; there is no separate "enter email, then see password" sub-step observed.
- **Required vs optional**: both Email and Password are required — confirmed by submit-time validation (see below). No field is marked optional.
- **Input types**: Email field has no visible format hint/placeholder pattern beyond "Your email"; Password field is a masked text input with a reveal toggle. No password-strength meter or minimum-length hint was visible before submission.
- **Default values**: none pre-filled; "Remember me" defaults to checked.
- Evidence: SS54 (desktop initial), SS57 (mobile initial).

### Validation behavior actually tested

- **Blank-submit validation (tested)**: clicking "Get started" with both fields empty produces inline text **"This field is required."** under both Email and Password (Password's version renders as an ARIA `alert`, Email's as a plain paragraph — a minor accessibility inconsistency worth noting). No network submission error was shown; this reads as pure client-side validation. Evidence: SS55 (desktop), SS56 (mobile).
- **Blur validation (tested)**: typing a syntactically invalid email (`not-an-email`) and then blurring (by clicking into the Password field) produced **no inline validation message**. This form does not validate on blur — only on submit-attempt. Evidence: SS62.
- **Malformed-email submit validation**: **not tested**. Clicking "Get started" with any non-blank value in both fields risks an actual account-creation attempt against Preply's backend, which is out of scope (no fake or real information may be submitted). This remains **NOT VERIFIED**.
- **No CAPTCHA, reCAPTCHA, or hCaptcha widget was observed** on this form in either its initial or validated states.

### OAuth sign-up paths (tested up to the point of leaving Preply, no further)

- **"Sign up with Google"** navigates (full page redirect, not a popup) to a real Google OAuth authorization URL at `accounts.google.com`. The redirect URI embedded in that URL is `https://preply.com/complete/google-oauth2/`, and the requested scopes are `userinfo.email`, `userinfo.profile`, `openid`, `email`, `profile`. This lands on Google's generic account chooser/sign-in page — no Preply-specific content, no personal data. **No Google account was selected or signed into.** Evidence: SS60.
- **"Sign up with Facebook"** navigates similarly to `facebook.com/login.php`, with a callback to `https://preply.com/complete/facebook/` and requested scope `email`. **No Facebook credentials were entered.** Evidence: SS61.
- Both callback paths (`/complete/google-oauth2/`, `/complete/facebook/`) follow the naming convention of the Python `social-auth` / `python-social-auth` library, suggesting Preply's backend (or at least its auth layer) is Django/Python-based — an inference, not confirmed by any other direct evidence this session.
- What happens *after* a real OAuth login completes (e.g., does it drop the user straight into wizard section 1 "About", skipping a separate email/password step?) is **NOT VERIFIED** — this would require actually completing a third-party login, which was not attempted.

## Stage: Authenticated 8-section wizard (About → Pricing)

- **Not reached.** Direct navigation to `https://preply.com/en/tutor-signup` resolves to `https://preply.com/en/teach` for an anonymous session — see `access-limitations.md` for full detail.
- Per `docs/product/preply-reverse-engineering/18-screenshot-evidence.md` (line 7), this wizard lives at a **single URL** (`/en/tutor-signup`) with internal step state — it is not implemented as one route per stage. This was corroborated by finding no stage-specific route naming anywhere in this session's navigation, OAuth redirect parameters, or help-center documentation.
- Everything about the wizard's actual field-by-field content in this report is therefore sourced only from the historical record (`18-screenshot-evidence.md`, SS01–SS13) or the official help article (see below) — never from a fresh observation this session. See `comparison-with-existing-evidence.md`.

## Supplementary documentation evidence

- **Source**: `https://help.preply.com/en/articles/4175138-register-to-teach-on-preply`, fetched 2026-10-18. Article's own displayed "Last Updated" date: **June 17, 2026**.
- Documents registration as **three main steps**: (1) Registration (email/password or Facebook/Google) — explicitly notes *"Currently, it is possible to register as a tutor only in the desktop version"*; (2) an "8-section form" naming 7 sections by name — Profile Picture, Certifications, Education, Profile Description, Intro Video, Calendar/Availability, Tutoring Rate — plus implicitly an 8th (consistent with "About" being folded into the registration step itself, though the article's exact text for an explicit "About" section was not captured in this extraction — flagged as uncertain, see `comparison-with-existing-evidence.md`); (3) Application Review, stated as **"within 5 business days"** (a change from the landing page's own "3 business days" claim — see comparison doc).
- This is **DOCUMENTED**, not **OBSERVED**, evidence — it was summarized by an automated fetch/extraction tool, not read verbatim character-by-character, so treat exact wording as approximate, not a verified direct quote.

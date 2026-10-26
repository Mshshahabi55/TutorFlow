# Playwright Tutor Onboarding Capture — 2026-10-18

## Objective

Re-capture, systematically and reproducibly, as much of Preply's real tutor registration/application wizard as is safely and legitimately reachable by an anonymous Playwright browser session — covering the 8 historical stages (About, Photo, Certification, Education, Description, Video, Availability, Pricing) where possible — for product-research purposes only. No TutorFlow code was changed by this task.

## Capture date / browser configuration

- **Date**: 2026-10-18, approx. 19:37–19:52 local machine time.
- **Tooling**: Playwright MCP (`mcp__playwright__browser_*` tools) — a Chromium browser instance driven by the MCP server, not a standalone `playwright.config.ts`/test-runner setup (none exists in this repository — see "Existing infrastructure inspected" below).
- **Authentication state**: fully anonymous. No cookies, no `storageState` file, no CDP connection to any already-logged-in browser. Confirmed anonymous throughout by the page header consistently showing "Log In".
- **Viewports used**: 1440×900 (desktop) and 390×844 (mobile).

## Existing infrastructure inspected before capturing

- No `playwright.config.*` file exists anywhere in the repository, and `playwright` is not a dependency in `package.json` or `apps/web/package.json`. TutorFlow has no Playwright-based E2E test suite — the only Playwright capability available is the MCP server used here, which drives a real, ad-hoc browser session rather than a configured test project.
- No `storageState`/auth-state JSON files exist anywhere in the repo — confirming no authenticated Preply session has ever been persisted for reuse.
- `.playwright-mcp/` (repo root) is the MCP tool's own scratch directory for console logs, accessibility-tree snapshots (`.yml`), and ad-hoc screenshots from this and prior sessions — it is not a deliverable location; all screenshots this task actually commits to the evidence package were copied from there into `screenshots/` below.
- Prior research docs inspected: `18-screenshot-evidence.md` (confirms the wizard is a single-URL SPA at `/en/tutor-signup`, not per-stage routes, and that its SS01–SS13 evidence was captured manually by a real person using their own real, pre-existing Preply account — carries a PII Notice), `20-tutorflow-wizard-strict-parity-matrix-2026-10-18.md`, `21-tutor-onboarding-product-decision-brief.md`, `22-first-form-about-parity-review-2026-10-18.md` (all already-completed TutorFlow-side parity analysis, not re-litigated here).
- Why the previous capture (`docs/product/preply-reverse-engineering/evidence/preply-wizard-evidence-2026-10-18/`, same date, earlier in this session) stopped at the public landing page: the authenticated wizard URL (`/en/tutor-signup`) redirects anonymous visitors to `/en/teach`, and no authorized Preply account exists for this project. That earlier package's screenshots (SS53–SS59) are reused here rather than re-captured, since re-capturing identical, already-verified states would add no new evidence — see "Relationship to the other evidence-capture-2026-10-18 folder" below.

## Entry URL and scope

Started at `https://preply.com/en/teach` and followed the real "Create a tutor profile now" CTA through to `https://preply.com/en/teach/signup`. This is the actual, current tutor-registration entry point — not a stand-in for it.

## What was reached

- The public marketing landing page (`/en/teach`).
- The registration gate (`/en/teach/signup`): email/password form, both OAuth buttons (redirect targets inspected up to the third-party provider's own generic login screen, never completed), blank-submit validation, and blur-vs-submit validation timing.

## What was NOT reached, and why

**None of the 8 wizard sections (About, Photo, Certification, Education, Description, Video, Availability, Pricing) were reached.** `https://preply.com/en/tutor-signup` redirects anonymous visitors straight back to `/en/teach`. Full detail, including exactly what would be needed to go further, is in `access-limitations.md`.

## Interactions actually exercised vs. merely visited

- **Exercised**: landing→signup navigation; blank-field submit (triggers real validation messages); email-field blur with an invalid format (triggers no message — informative negative result); both OAuth buttons clicked through to the third-party provider's own login screen, then abandoned.
- **Visited only, not exercised**: the FAQ accordion on the landing page, the language/currency selector, the "Remember me" checkbox, the password-reveal toggle.
- **Not touched at all**: anything requiring authentication.

## Personal information handling

No real person's name, email, face, payment detail, or account content appears in any screenshot captured this session — the furthest any screen went was Google's/Facebook's own generic, pre-login chooser screens, which contain no account-specific data since no account was selected. No cookies, `storageState`, or browser-profile files are included in this package. The OAuth redirect URLs contained provider-issued anti-CSRF `state` tokens tied to this one-off, abandoned session; these are not included verbatim in any document here (only the stable, non-secret parts — host, path, `redirect_uri`, requested `scope` — are quoted in `observations.md`).

## Relationship to the other `evidence-capture`/`preply-wizard-evidence` folder from today

Earlier in this same session, a separate, narrower task produced `docs/product/preply-reverse-engineering/evidence/preply-wizard-evidence-2026-10-18/` (7 screenshots, SS53–SS59, aimed at an external ChatGPT-review ZIP deliverable). This folder **supersedes and extends** that one for product-research purposes: it reuses SS53–SS59 verbatim (same files, same evidence, not re-captured) and adds 3 new screenshots (SS60–SS62: both OAuth redirects, and the blur-validation negative result) plus the fuller `manifest.json`/`observations.md`/`access-limitations.md`/`comparison-with-existing-evidence.md` structure this task specifically requires. Neither folder was deleted or modified in place — both remain available, unaltered.

## How to reproduce this capture

1. Confirm Playwright MCP tools are available (`mcp__playwright__browser_navigate`, etc.).
2. `browser_resize` to 1440×900, `browser_navigate` to `https://preply.com/en/teach`, screenshot.
3. `browser_find`/`browser_snapshot` to locate the "Create a tutor profile now" button, `browser_click` it.
4. On `/en/teach/signup`: screenshot initial state; click "Get started" with both fields blank and screenshot the validation state; type an invalid email into the Email field, click into Password to blur, screenshot (expect no message); click "Sign up with Google", screenshot the resulting `accounts.google.com` page, then `browser_navigate_back`; repeat for "Sign up with Facebook" (`facebook.com/login.php`).
5. `browser_resize` to 390×844 and repeat the landing-page and signup-page screenshots.
6. Attempt `browser_navigate` to `https://preply.com/en/tutor-signup` directly and observe/screenshot the resulting redirect.
7. Do not, at any point, submit the email/password form with any value, or complete either OAuth flow.

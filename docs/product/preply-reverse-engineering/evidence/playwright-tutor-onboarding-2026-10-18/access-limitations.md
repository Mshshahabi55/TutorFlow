# Access Limitations — 2026-10-18

## The exact blocker

`https://preply.com/en/tutor-signup` — the authenticated 8-section tutor application wizard (About, Photo, Certification, Education, Description, Video, Availability, Pricing) — **redirects anonymous visitors to `https://preply.com/en/teach`** (the public marketing landing page). This was tested directly this session (not inferred): requesting the URL via `page.goto()` resulted in the browser's final URL being `/en/teach`, with the landing page's own content rendered. Evidence: `screenshots/SS59_tutor-signup_redirect_desktop.png`.

This is the same blocker documented on every previous attempt across this project's history (confirmed consistent across at least 5 separate attempts now, including this one).

## Why it wasn't bypassed

- No Preply account exists for this project, and creating one is explicitly out of scope per `docs/product/preply-reverse-engineering/00-scope-and-methodology.md` (standing Product Owner rule) and this task's own instructions ("do not create repeated fake accounts to get around access restrictions").
- The Playwright MCP browser used this session carries **no authentication state** — no cookies, no `storageState` file, no CDP connection to the user's own logged-in browser. This was confirmed by observing the page header show "Log In" (not an authenticated user menu) on every page load, and by finding zero `storageState`/auth-state files anywhere in the repository before starting (see `README.md` → "Existing infrastructure inspected").
- Completing either OAuth path (Google or Facebook) was deliberately stopped short of entering real credentials or selecting a real account — doing so would either access a real third party's account (not authorized) or, if a Preply account doesn't yet exist for that identity, likely create one (an irreversible action out of scope). Both redirects were captured up to the point of landing on the provider's own generic login page, then abandoned (`browser.goBack()`), per `observations.md`.
- Submitting the email/password form with any value (real or fabricated) was not attempted, since a successful submission would very likely create a real (if fake) Preply account — explicitly prohibited by this task's instructions.

## What would be needed to go further

One of the following, from the user:

1. **Explicit authorization to create a throwaway Preply tutor account** for this research, with the user either (a) completing the actual email-verification / OAuth step themselves since that can't be done without real credentials or a real inbox, or (b) handing over an already-authenticated browser session (e.g., running the MCP browser against an existing authenticated Chrome profile via CDP) that the user has knowingly set up for this purpose. No credentials, cookies, or tokens should ever be typed into chat — only the authenticated browser session itself would be used.
2. **Acceptance that the wizard's actual current field-by-field content cannot be freshly re-verified** this session, and that the only record of it remains the historical, already-redacted evidence in `18-screenshot-evidence.md` (SS01–SS13, captured by a real person using their own pre-existing account, per that document's own PII Notice) plus the official help-center documentation summarized in `observations.md`.

## Stage-by-stage status (per this task's required classification)

| # | Stage | Status |
|---|-------|--------|
| — | Public landing page (`/en/teach`) | Captured — directly observed |
| — | Registration gate (`/en/teach/signup`) | Captured — directly observed (including blank-field validation and both OAuth redirect entry points) |
| 1 | About | Blocked — authentication/access required |
| 2 | Photo | Blocked — authentication/access required |
| 3 | Certification | Blocked — authentication/access required |
| 4 | Education | Blocked — authentication/access required |
| 5 | Description | Blocked — authentication/access required |
| 6 | Video | Blocked — authentication/access required |
| 7 | Availability | Blocked — authentication/access required |
| 8 | Pricing | Blocked — authentication/access required |

No stage from 1–8 is marked "Captured" or "Partially captured" — none were reached this session. This supersedes any impression from route names, button labels, or the historical screenshots that these stages were inspected live today. They were not.

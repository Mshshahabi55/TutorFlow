# Preply Wizard Evidence Capture — 2026-10-18

Purpose: fresh, unauthenticated evidence of Preply's publicly-reachable tutor-signup entry point, captured for external (ChatGPT) review to help decide how TutorFlow's tutor onboarding wizard should match Preply. This package does **not** implement anything and does **not** supersede any prior product decision document.

## Browser automation used

Yes — Playwright MCP (`mcp__playwright__browser_*` tools) was available and was actually used this session to navigate `preply.com` live and capture these screenshots. This was re-confirmed working at the start of this task.

## Where navigation reached, and where it stopped

- Reached and freely explored (unauthenticated, no account): the public marketing landing page (`/en/teach`) and the public email/password + OAuth signup form (`/en/teach/signup`), at both desktop and mobile viewports.
- **Stopped at**: `/en/tutor-signup` — the authenticated 8-step wizard (About, Photo, Certification, Education, Description, Video, Availability, Pricing). Requesting this URL directly redirects anonymous visitors to `/en/teach` (SS59). This is the same blocker documented every time it has been attempted this project — it is not new or unexpected.
- **Why it stopped there**: no Preply account exists for this project, and creating one is out of scope per the project's own standing rule (`00-scope-and-methodology.md`) and this task's explicit instructions (no fake information submitted, no registration finalized, no credentials requested from the user). The only way past this point would be for the user to either (a) explicitly authorize creating a throwaway Preply tutor account for research, and either complete a real signup step themselves or hand over an already-authenticated browser session, or (b) accept that the 8-step wizard's actual screens remain evidenced only by the historical, already-redacted screenshots from `18-screenshot-evidence.md` (SS01–SS13).

## What was actually interaction-tested vs. merely visited

- **Interaction-tested**: clicking the "Create a tutor profile now" CTA (landing → signup navigation); clicking "Get started" with both fields blank to trigger the signup form's client-side required-field validation (captured at both viewports).
- **Merely visited (not interacted with)**: the OAuth "Sign up with Google"/"Sign up with Facebook" buttons, the "Remember me" checkbox, the password-reveal toggle, and the language/currency selector — all visible in the screenshots but not clicked, since doing so either risks touching a real third-party account or doesn't inform wizard-content parity.
- **Not tested at all**: malformed-email/weak-password validation messages, any state inside the 8-step wizard itself (unreachable this session).

## Fresh vs. historical evidence — kept strictly separate

- **Fresh (this folder)**: `SS53`–`SS59`, 7 screenshots, all captured today (2026-10-18) in this session, listed with full detail in `evidence-manifest.md`. These cover only the public landing page and the public signup form — not the 8-step wizard.
- **Historical (referenced, not copied)**: the 8-step wizard's actual field-by-field content (About, Photo, Certification, Education, Description, Video, Availability, Pricing) is **not re-included as image files in this package**. It remains documented, already-redacted, in `docs/product/preply-reverse-engineering/18-screenshot-evidence.md` (SS01–SS52). See "Why historical images aren't copied here" below.

### Why historical images aren't copied here

`18-screenshot-evidence.md` carries its own PII Notice: the original source image files behind SS01–SS52 (`evidence/tutor.zip`, `evidence/BeforFindYourTutor.zip`, `evidence/FindYourTutor.zip`, `evidence/PreplySentEmailForMe.zip`) contain real, unredacted personal information — a real name, a real email address, a real uploaded face photo and webcam video frame, a real completed Google OAuth sign-in to a real pre-existing Preply account, and a real near-completed checkout session with a real tutor. The markdown documentation in `18-screenshot-evidence.md` has all of this redacted, but the raw image files on disk do not. Since this package is being sent to an external service (ChatGPT) for review, **no raw copies of those original historical images are included here** — only a pointer to the already-redacted markdown. If ChatGPT needs the actual historical wizard screens, point it at the redacted markdown text, not image files.

## Exact source of each evidence item

- SS53–SS59: captured live, this session, 2026-10-18, via Playwright MCP against `https://preply.com`, unauthenticated. Full detail (URL, action, viewport, timestamp, observed-vs-inferred) in `evidence-manifest.md`.
- Any reference to SS01–SS52: sourced from the existing, already-completed `docs/product/preply-reverse-engineering/18-screenshot-evidence.md` — not re-captured, not re-copied as images, in this task.

## Remaining unknowns

- The 8-step wizard's current actual field labels/order/validation cannot be freshly re-verified against the live site this session — only against the historical SS01–SS13 record, which may have drifted since it was captured.
- OAuth-initiated signup flow (clicking "Sign up with Google"/"Sign up with Facebook" instead of email/password) was not explored at all, fresh or historical, for this specific entry point.

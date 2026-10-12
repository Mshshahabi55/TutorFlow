# Mobile / Responsive

**No mobile-viewport browser testing was performed this project** — the available tooling (`WebFetch`) renders a single document response with no viewport emulation capability. Every finding below is **DOCUMENTED** (from official sources) or **NOT VERIFIED**; nothing in this document is OBSERVED, and this document must never be read as implying mobile behavior was actually browser-tested.

## DOCUMENTED surface-specific differences

| Behavior | Surfaces it applies to | Confidence |
|---|---|---|
| Tutor registration | Desktop web only — explicitly stated as a restriction | High |
| Apple Pay / Google Pay | App + mobile-site only, **not** desktop web | High |
| Classroom core features (video, chat, file-share, notes) | Work in a phone/tablet browser, not app-exclusive | High (direct fetch) |
| Classroom screen-share / learning-materials library | Desktop-**recommended**, not hard-blocked on mobile browsers — a usability recommendation, not an enforced restriction | High (direct fetch) — **this corrects a common assumption**; it is not the same category of restriction as the payment-method one above |
| Native-app-only push notification timing (24h/1h/10min before a lesson) | App only | Low/medium — search-engine-synthesized, not an official article directly fetched and read; treat as NOT VERIFIED pending direct confirmation |
| Desktop-only bulk/recurring-slot scheduling convenience | Desktop | NOT VERIFIED — search-synthesized only, no official article found |
| Google Calendar integration surfacing the Classroom join link in calendar events | Unclear which surface(s) | NOT VERIFIED |

## Why this matters for TutorFlow specifically

TutorFlow is a **responsive web application with no native app**. Every "app-only" restriction found above (wallet payment methods, push-notification timing) is something TutorFlow structurally cannot match without building a native app — this is correctly out of scope, not a gap to close, and should not be miscategorized as a parity failure in `13-tutorflow-current-state-and-parity.md`. Conversely, the Classroom screen-share finding is good news: it means a responsive-web-only Classroom implementation does not inherently fall short of Preply's own mobile-web behavior on that specific point.

## What remains entirely unverified

Actual rendered mobile layout for any Preply page (navigation collapse, touch targets, form field sizing, calendar widget usability on a small viewport, checkout layout) — none of this was observed and none of it should be assumed. If mobile-specific TutorFlow work is ever benchmarked against Preply, a genuine mobile-viewport browser session (not available this pass) would be needed first.

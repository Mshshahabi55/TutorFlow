# Accessibility Requirements

**Status:** Specified. Frontend engineering/design requirement, not a backend concern. Target set independently of Preply's own (not reliably observable, `PREPLY_UI_UX_AUDIT.md` §22) accessibility posture.

## Target

**WCAG 2.2 Level AA**, as the baseline for every production, user-facing TutorFlow surface (public site, Student app, Tutor app). Admin surfaces should aim for the same bar but are lower priority if a genuine trade-off arises (internal tool, smaller audience) — never a reason to skip it outright.

## Requirements by area

| Area | Requirement |
|---|---|
| **Keyboard navigation** | Every interactive element (including custom components — multi-select, date picker, calendar grid, wizard stepper) reachable and operable via keyboard alone; no mouse-only interaction anywhere |
| **Focus management** | Visible focus indicator on every focusable element (never `outline: none` without a replacement); focus moves predictably on route change, modal/drawer open (trapped inside), and modal/drawer close (returns to the triggering element) |
| **Semantic HTML** | Native elements (`<button>`, `<nav>`, `<table>`, heading levels in document order) over `<div>` + ARIA role soup; ARIA only to fill a genuine native-HTML gap |
| **Form labels & errors** | Every input has a programmatically associated `<label>` (not placeholder-as-label); errors are associated to their field via `aria-describedby` and announced on submit, not just shown visually |
| **Screen reader support** | All of the above, plus live-region announcements (`aria-live`) for async state changes a sighted user would notice visually but a screen-reader user wouldn't — booking confirmation, validation result, toast notifications |
| **Color contrast** | WCAG 2.2 AA minimums: 4.5:1 for normal text, 3:1 for large text and meaningful UI/icon elements — checked against `docs/design/DESIGN_SYSTEM_DIRECTION.md`'s palette once it's finalized with actual hex values, not Preply's (unobserved) palette |
| **Reduced motion** | Respect `prefers-reduced-motion`; any animation (wizard step transitions, skeleton shimmer, toasts) has a reduced/instant variant, never motion-only feedback |
| **Accessible dialogs/drawers** | `role="dialog"`/`aria-modal="true"`, labelled via `aria-labelledby`, focus-trapped, `Esc`-to-close, focus restored to trigger on close |
| **Calendar accessibility** | The availability/booking calendar (date/time-slot picker) is the highest-risk custom component in this product — grid pattern with arrow-key navigation, `aria-selected` on the chosen slot, and an accessible name per cell (e.g. "Tuesday, October 6th, 3:00 PM, available") rather than a bare visual grid |
| **Validation messaging** | Inline, associated to the field, present in the accessibility tree immediately on error (not only visually styled) — consistent with `error-handling` skill's `ValidationFilter<T>` output feeding a real `aria-describedby` target, not just a visual red border |

## What this spec does not decide

- Automated testing tooling (axe-core, Playwright + axe, etc.) — an implementation-phase choice, not a product/architecture decision.
- Exact color values — depends on `DESIGN_SYSTEM_DIRECTION.md`'s still-pending final palette.

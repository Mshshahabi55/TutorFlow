# Design System Direction

This document sets *direction and token categories* for TutorFlow's design system. It deliberately contains no literal color values, exact type scale, or component visuals — those are design decisions for whoever owns visual design (Product Owner / design function), not invented here. Copying Preply's actual colors, type, or styling is explicitly out of bounds per the product brief.

## Direction (qualitative brief for whoever designs the tokens)

Modern, trustworthy, European, premium, friendly, optimized for marketplace conversion. In practice for a tutoring marketplace, this usually means: calm/neutral base palette so tutor photos and video are the visual focus, a confident accent color reserved for primary CTAs (book, subscribe, pay) so they stand out against supporting UI, and typography that reads well in long-form tutor bios and chat as well as dense dashboard tables.

## Token categories to define (not the values)

- **Color**: primary, secondary, background (base + elevated/surface), text (primary/secondary/disabled/inverse), semantic (success/warning/error/info), border
- **Typography**: font family, type scale (display/heading/body/caption), weight scale, line-height scale
- **Spacing**: a consistent scale (e.g. 4px/8px base) used everywhere — no ad hoc margins
- **Radius**: a small scale (e.g. none/sm/md/lg/full) applied consistently to cards, buttons, inputs, modals
- **Shadow/elevation**: a small scale for cards, dropdowns, modals
- **Breakpoints**: mobile/tablet/desktop (exact widths TBD by whoever owns responsive strategy)

## Component inventory implied by the product surfaces documented in this spec

Buttons (primary/secondary/destructive/ghost, each with loading/disabled states), inputs (text/select/date/time/currency), cards (tutor card, booking summary card, lesson card), tabs, badges (status badges for Booking/Payment/Subscription states — see `docs/domain/STATE_MACHINES.md`, so badge variants should map 1:1 to actual state enum values, not invented separately), modals, dropdowns, calendar/availability grid, booking flow components (duration/date/time pickers), dashboard stat tiles, navigation (public/student/tutor/admin each likely need their own nav shell, per `docs/product/NAVIGATION_AND_PAGE_MAP.md`).

**`[OPEN — Architect]`**: per existing user preference captured for this product, any shared calendar/availability-grid component must be presentation-only — it renders slots it's given and never makes its own overlap/availability decisions; that logic belongs in the `Availability` domain and is computed server-side, with the component simply reflecting the result. Flagging this now so it isn't relearned the hard way when the first calendar component gets built.

## Explicit non-decisions

- No color values, hex codes, or font choices are specified here.
- No component library (e.g. shadcn/ui, MUI, custom) is chosen here — that's a frontend-architecture decision for whoever sets up the Next.js app.
- No logo/brand mark — out of scope for engineering documentation.

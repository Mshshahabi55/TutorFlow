# TutorFlow Design System — Foundation (Phase D1, revised Preply Redesign Phase 1)

> **This document was substantially superseded on the palette/typography/
> shape/shadow front by the Preply Redesign Phase 1** (§8 below). §§1–7
> below describe Phase D1/D3's original cool-neutral, border-first
> system — kept for historical record of *why* those choices were made at
> the time, since some of that reasoning (system font stack, MUI variant-
> name reuse instead of a parallel scale, dark mode's "bright fill + dark
> contrastText" pattern) still holds. Where §§1–7's actual **values**
> (hex codes, px sizes, "border not shadow") conflict with §8, §8 wins —
> `frontend/src/app/theme.ts` is the implementation, and §8 is what it
> currently looks like.

Single source of truth for every token behind the frontend UI: colour,
type, spacing, shape, shadow. The tokens themselves live in
`frontend/src/app/theme.ts` — this document explains *why* each value was
chosen and records the measurements that back the accessibility claims
made in that file's comments. If this document and `theme.ts` ever
disagree on a rationale, `theme.ts` is the implementation and this file
has drifted — fix the doc, not the reasoning, unless the code itself is
wrong.

This is a **Foundation-level implementation choice, not a ratified
brand.** No approved brand document exists for TutorFlow yet. The palette
below was chosen deliberately — calm, low-saturation, cool neutral, in
the reference class of Linear / Notion / the Stripe Dashboard — and is
revisitable the moment a real brand is supplied. Nothing here rises to
the level of an Architecture Decision Record: it is a UI implementation
detail, not a structural or cross-context decision, so no ADR was opened
for it.

## Scope of Phase D1

D1 built the foundation only. It did **not** restyle any real page —
that is D2 onward, page by page, each adopting the tokens and shared
components below. D1's five tasks:

1. **Audit** — read every shared primitive and a sample of pages to find
   what already exists, what's inconsistent, and what's missing. Findings
   are folded into the rationale comments throughout `theme.ts` and into
   §4 below, rather than kept in a separate document, since a stale
   standalone audit is a document lie waiting to happen.
2. **Tokens** — `frontend/src/app/theme.ts`: colour, type, spacing,
   shape, shadow (commit `7ef7009`).
3. **Shared primitives** — wire those tokens into the primitives every
   page already depends on (Button, IconButton, Checkbox, Radio, Card,
   Paper, Dialog, DataTable's TableHead, StatusPill, Tooltip, NavSidebar,
   RoleSwitcher) with **no public API change** to any of them, plus the
   new `UnitText` component (commit `de9f871`).
4. **Style guide** — `frontend/src/dev/StyleGuidePage.tsx`, a dev-only
   living reference rendering every token and every shared-component
   variant on one page, so the tokens and primitives can be reviewed
   before any real page adopts them.
5. **Verify exclusion** — prove the style guide never reaches a
   production build. See §5.

## 1. Colour

A neutral grey scale as the workhorse, one restrained accent
(`primary`), and four semantic tones (`success`/`warning`/`error`/
`info`). No second accent — `secondary` is deliberately just
`grey.700`, not a second brand colour, because the pre-D1 palette had
`secondary.main` accidentally equal to `warning.main`, an unintentional
collision this phase fixes by removing the ambiguity outright rather
than picking two colours that happen not to collide today.

| Token | Hex | Role |
|---|---|---|
| `grey.50` | `#F8F9FA` | lightest surface tint, `background.default` |
| `grey.100` | `#EEF1F3` | |
| `grey.200` | `#E1E5E9` | `divider`, outlined-card border |
| `grey.300` | `#CBD2D9` | |
| `grey.400` | `#9AA5B1` | `text.disabled` |
| `grey.500` | `#6B7684` | |
| `grey.600` | `#4C5563` | `text.secondary` |
| `grey.700` | `#363E4A` | `secondary.main`, TableHead text |
| `grey.800` | `#232933` | Tooltip fill |
| `grey.900` | `#14181F` | `text.primary`, darkest |
| `primary.main` (accent) | `#2E6486` | the one accent — buttons, links, focus rings, selected-nav |
| `primary.light` | `#5C85A3` | |
| `primary.dark` | `#1F4258` | |
| `success.main` | `#1E7A42` | |
| `warning.main` | `#8A5F10` | |
| `error.main` | `#8A3524` | |
| `info.main` | `#1565A6` | added this phase — MUI's unthemed default info blue failed AA (3.86:1) for white-on-fill text |

**Measured contrast ratios** (WCAG relative-luminance formula, sRGB,
computed directly from the hex values above — not eyeballed):

| Colour | As white text on fill | As text on white background |
|---|---|---|
| `primary.main` | 6.40:1 | 6.40:1 |
| `success.main` | 5.36:1 | 5.36:1 |
| `warning.main` | 5.64:1 | 5.64:1 |
| `error.main` | 8.03:1 | 8.03:1 |
| `info.main` | 6.10:1 | 6.10:1 |
| `secondary.main` (`grey.700`) | 10.80:1 | 10.80:1 |
| `grey.600` (`text.secondary`) | — | 7.54:1 |
| `grey.900` (`text.primary`) | — | 17.79:1 |
| `grey.400` (`text.disabled`) | — | 2.50:1 |

Every tone that is actually used for body/label text or as a
button/chip fill **clears WCAG AA (4.5:1)** both ways — the same hex
works as a solid fill with white text and as body text directly on
white, so no tone needs a separate "text version" and a separate
"fill version." `grey.400` (`text.disabled`) is the sole value below
4.5:1, deliberately: WCAG's contrast requirement explicitly exempts
inactive/disabled UI components and their text, so this is not a
violation, just not a value to reuse for anything that isn't disabled.

## 2. Typography

One font stack — the OS system stack (`-apple-system`, `Segoe UI`,
`Roboto`, ...), no self-hosted webfont. A webfont adds a network
dependency and a flash-of-unstyled-text risk for close to zero visual
gain over a system stack that already renders natively and consistently
on every platform TutorFlow's audience actually uses; it was not chosen
for the `ADR-018` (regional deployment / reachability-from-Iran) reason
— system fonts have no reachability concern either way — simply because
it's the right default absent a ratified brand typeface.

A modular scale (~1.2–1.25 ratio) mapped onto **MUI's own existing
variant names** (`h1`..`h6`, `subtitle1`/`2`, `body1`/`2`, `caption`,
`overline`, `button`) rather than inventing a parallel custom scale —
the Task 1 audit found every call site in the app already writes
`variant="h4"` or `variant="body2"`, never a raw pixel size, so this
phase documents and tightens that existing convention instead of
migrating it to something new.

| Variant | Size | Weight | Used for |
|---|---|---|---|
| h1–h3 | 48/36/30px | 700 | reserved, unused today |
| h4 | 24px | 700 | `PageHeader` title |
| h5 | 20px | 600 | |
| h6 | 18px | 600 | AppBar title |
| subtitle1 | 16px | 600 | card/section headings |
| subtitle2 | 14px | 600 | |
| body1 | 16px | 400 | default body copy |
| body2 | 14px | 400 | secondary/dense copy |
| caption | 12px | 400 | unit suffixes, helper text |
| overline | 12px | 600 | uppercase, `0.06em` tracking |
| button | 14px | 600 | no text-transform (MUI default uppercase is disabled) |

### Type role naming (Phase D3)

The "Foundation UI & Design System" brief asks for a named hierarchy
(Display, Page Title, Section Title, Card Title, Subtitle, Body, Caption,
Small Label). Mapped onto the scale above rather than invented as a
parallel one — same reasoning as the scale itself: every call site already
writes a MUI variant name, so naming is documentation, not a second token
system.

| Role | Variant | Note |
|---|---|---|
| Display | `h1` | reserved, unused today (same as the scale table above) |
| Page Title | `h4` | `PageHeader` title |
| Section Title | `h5` | |
| Card Title | `subtitle1` | |
| Subtitle | `subtitle2` | |
| Body | `body1` | |
| Caption | `caption` | |
| Small Label | `overline` | |

## 3. Spacing, shape, shadow

- **Spacing**: MUI's own `theme.spacing(n)`, base unit **8px** — not a
  new token, a statement of the existing convention. The Task 1 audit
  found no inline pixel spacing outside `theme.ts` already.
- **Shape**: two radii. `sm` (4px) for dense inline elements; `md`
  (8px, the default) for Button/TextField/Card/Paper/Dialog. `StatusPill`
  (a MUI `Chip`) stays fully pill-shaped regardless — that's its own
  visual language for "this is a status," not a shared radius.
- **Shadow**: depth comes from a **1px border** (`variant="outlined"`,
  already the convention on every Card/Paper) and from spacing, not
  decorative drop shadow, per the brief ("no gradients, no decorative
  motion"). MUI requires exactly 25 shadow entries (elevation 0–24);
  this flattens Material's default heavy ramp to two deliberately
  subtle tiers (`resting`, `floating`) rather than removing shadow
  outright, because a handful of MUI components (Menu, Popover, Dialog,
  Snackbar) still rely on elevation to separate themselves from page
  content when they float with no bordered container of their own.

## 4. Task 1 audit findings, and how each was closed

| Finding | Where | Closed by |
|---|---|---|
| `secondary.main` accidentally equal to `warning.main` | palette | `secondary` redefined as `grey.700`, a neutral, not a second accent |
| No themed `info` tone; MUI default failed AA | palette | explicit `info.main` (`#1565A6`), measured at 6.10:1 |
| `background.default` a warm cream, inconsistent with the calm-neutral brief | palette | shifted to a cool neutral (`grey.50`) |
| No visible focus indicator beyond the browser default | Button, IconButton, Checkbox, Radio | `:focus-visible` gets a 2px accent-colour outline on all four |
| `NavSidebar` had no visual indication of the current route at all | NavSidebar | `.Mui-selected` gets an accent-tinted background and a left accent bar, driven by `useLocation()` |
| `RoleSwitcher` (dev-only role preview) sat visually identical to `AuthStatus` (real signed-in identity), in the same AppBar | RoleSwitcher | `RoleSwitcher` now sits inside a dashed, warning-tinted frame — visually distinct from a real identity |
| `StatusPill`'s neutral/outlined tone used MUI's default action-grey, reading as "disabled" rather than "no status opinion" | StatusPill | deliberate border/text colour via `MuiChip` `outlined` override |
| `DataTable`'s header row had no distinct treatment from body rows | DataTable | `MuiTableHead` override: tinted background, heavier text weight, distinct border colour |
| Six different ad hoc renderings of a Tehran time or Toman amount's unit suffix across pages (table header suffix, bold inline label, parenthetical, field label — no shared component behind any of them) | multiple pages | new `UnitText` component (not yet adopted by any page — that's D2+) |
| Tooltips had no consistent affordance (some had an arrow, most didn't; no consistent tone) | Tooltip | app-wide `arrow: true` default, consistent dark fill/tone |

## 5. Excluding the style guide from production

`frontend/src/dev/StyleGuidePage.tsx` is a **dev-only** page — a living
reference for reviewing every token and shared-component variant before
any real page adopts them (D2–D6). It must never ship in the production
bundle, and must never be reachable from navigation in production.

**Mechanism** (`frontend/src/routes/router.tsx`): the lazy import and
the route registration are both gated on `import.meta.env.DEV`:

```ts
const StyleGuidePage = import.meta.env.DEV
  ? lazy(() => import("@/dev/StyleGuidePage").then(...))
  : null;

const devRoutes =
  import.meta.env.DEV && StyleGuidePage
    ? [{ path: paths.dev.styleGuide, element: withSuspense(<StyleGuidePage />) }]
    : [];
```

Vite's `define` transform replaces `import.meta.env.DEV` with the
literal `false` in a production build *before* Rollup bundles — so in
that build, the whole ternary (including its dynamic `import()` call)
is statically-known dead code and is tree-shaken away entirely. No
`StyleGuidePage` chunk is emitted, and `devRoutes` collapses to `[]`, so
the route is never registered either.

**Verified directly** (Phase D1 Task 5, this phase): `npm run build`
was run against the working tree containing `StyleGuidePage.tsx` and
the router changes above, then every file under the resulting `dist/`
was searched for any trace of the style guide:

```
$ grep -rl "StyleGuidePage\|style-guide\|Every Phase D1 token" dist/
(no matches)
```

No chunk file, no route string, no page content — confirming the
mechanism works in practice, not just in theory. See
`docs/phases/PHASE-D1-REPORT.md` §5 for the full build output.

## 6. What D2+ inherits

Every shared component and token above is ready for pages to adopt.
Nothing in D1 changed how any page looks — `NavSidebar`'s selected-state
and `RoleSwitcher`'s frame are the two exceptions, both layout-level
components rather than page content, and both closing a real gap the
audit found rather than restyling existing page content. `UnitText` in
particular has zero callers today; the first page to adopt it is a D2+
task.

## 7. Dark mode (Phase D3)

`frontend/src/app/theme.ts`'s single `createTheme(...)` call is now
`createAppTheme(mode: PaletteMode)`, called twice: `theme` (light — every
existing import keeps working unchanged) and `darkTheme` (new). The dark
palette is **not an inverted copy** of the light one — a bright accent or
semantic tone that reads well as text on a dark surface needs *dark*
`contrastText`, not white (measured, not assumed: a light fill with white
text fails AA). Surfaces step up in lightness from `background.default` →
`paper` → an `elevated` tier (TableHead background, Tooltip fill) — dark-
mode "elevation" is a lighter surface here, matching this app's existing
border/spacing-first depth language rather than introducing shadow-based
depth for the first time.

| Token | Hex | Role |
|---|---|---|
| `background.default` | `#0F1319` | page background |
| `background.paper` | `#1C232D` | Card/Paper/Dialog surface |
| elevated surface | `#242C38` | TableHead background, Tooltip fill |
| `divider` | `#333B47` | |
| StatusPill outlined border | `#3E4753` | a touch more visible than `divider` |
| `text.primary` | `#EDF0F3` | |
| `text.secondary` | `#9AA5B1` | (same hex as light mode's `grey.400`) |
| `text.disabled` | `#5C6470` | WCAG-exempt, same as light mode |
| `primary.main` (accent) | `#6FA8C9` | |
| `primary.contrastText` | `#0B0E12` | dark, not white — see above |
| `success.main` | `#4CAF71` | contrastText `#0B0E12` |
| `warning.main` | `#D3A038` | contrastText `#0B0E12` |
| `error.main` | `#E08070` | contrastText `#0B0E12` |
| `info.main` | `#5A9FD6` | contrastText `#0B0E12` |
| `secondary.main` | `#8A94A3` | contrastText `#0B0E12` |

**Measured contrast ratios**, same WCAG relative-luminance formula as §1 —
now backed by a real test (`frontend/src/app/theme.test.ts`,
`frontend/src/app/contrast.ts`) instead of a static, unrepeatable table:

| Pair | Ratio |
|---|---|
| `text.primary` on `background.default` | 16.28:1 |
| `text.primary` on `background.paper` | 13.83:1 |
| `text.secondary` on `background.default` | 7.44:1 |
| `primary.contrastText` on `primary.main` | 7.48:1 |
| `success.contrastText` on `success.main` | 7.07:1 |
| `warning.contrastText` on `warning.main` | 8.16:1 |
| `error.contrastText` on `error.main` | 6.90:1 |
| `info.contrastText` on `info.main` | 6.78:1 |
| `secondary.contrastText` on `secondary.main` | 6.30:1 |

Every pair clears WCAG AA (4.5:1), same bar as the light palette in §1.

**Wired into the live app as of Phase D4.** `ColorModeProvider`
(`frontend/src/shared/context/ColorModeProvider.tsx`) and `ThemeToggle`
(`frontend/src/shared/components/ThemeToggle.tsx`) are no longer
StyleGuidePage-only previews — `AppProviders.tsx` selects `theme`/
`darkTheme` from `useColorMode()`'s resolved mode, and `AppHeader`
renders a real `ThemeToggle` icon button in the live header. Dark mode's
palette is therefore live production surface, not a future-phase
placeholder — see §8 for the current (Preply Redesign) dark tones and
their measured ratios. See `docs/phases/PHASE-D3-REPORT.md` for the
original build-and-preview record and `docs/phases/PHASE-D4-REPORT.md`
for the wiring.

## 8. Preply Redesign — Visual Foundation Refresh

A full palette/typography/shape/shadow replacement, requested to move the
app from this document's original "calm, low-saturation, Linear/Notion/
Stripe-Dashboard" reference class to a marketplace one (Preply/Airbnb/
Notion/Linear) — colours are no longer restrained-blue-grey but a warm
deep-emerald brand; depth is no longer border-first but soft-shadow-and-
hover-lift; radius is deliberately generous (24px cards, not 8px). All
values below are literal inputs from that request except where noted;
`contrastText` choices were derived, not given, and are called out
explicitly.

### 8.1 Colour — light

| Token | Hex | Role |
|---|---|---|
| `primary.main` | `#0F766E` | brand accent — buttons, links, focus rings, selected-nav |
| `primary.dark` | `#115E59` | hover (MUI's own contained-Button darken reads this directly) |
| `primary.light` | `#14B8A6` | accent tint |
| `success.main` | `#16A34A` | contrastText `#111827` (**not white** — see below) |
| `warning.main` | `#D97706` | contrastText `#111827` (**not white** — see below) |
| `error.main` | `#DC2626` | contrastText `#FFFFFF` |
| `info.main` | `#2563EB` | not named in the brief; chosen from the same Tailwind family as the fourth semantic tone |
| `background.default` | `#F8FAFC` | |
| `background.paper` | `#FFFFFF` | Card/Paper/Dialog surface |
| `divider` / border | `#E5E7EB` | |
| `text.primary` | `#111827` | |
| `text.secondary` | `#6B7280` | |

**Why success/warning need dark `contrastText`.** The brief's literal
hex values, paired with the "obvious" white text a filled success/warning
button would default to, both measure under WCAG AA's 4.5:1 floor
(white-on-`#16A34A` ≈ 3.30:1, white-on-`#D97706` ≈ 3.19:1 — a real,
computed failure, not a style call). Swapping `contrastText` to
`text.primary` (`#111827`) while keeping the requested fill hex exactly
as given clears AA comfortably in both cases. `error` (`#DC2626`) is kept
white-on-red since it already clears AA (≈4.83:1) and dark-on-red does
not (≈3.67:1). Verified empirically by `frontend/src/app/theme.test.ts`,
not just hand-derived — all 20 assertions (10 per mode) pass.

**Measured contrast ratios** (same WCAG relative-luminance formula as §1):

| Pair | Ratio |
|---|---|
| `primary.contrastText` / `primary.main` | 5.47:1 |
| `success.contrastText` / `success.main` | 5.38:1 |
| `warning.contrastText` / `warning.main` | 5.57:1 |
| `error.contrastText` / `error.main` | 4.83:1 |
| `info.contrastText` / `info.main` | 5.17:1 |
| `secondary.contrastText` / `secondary.main` | 10.31:1 |
| `text.primary` / `background.default` | 16.96:1 |
| `text.primary` / `background.paper` | 17.74:1 |
| `text.secondary` / `background.default` | 4.62:1 |

### 8.2 Colour — dark

Re-tinted from Phase D3's blue accent to this phase's emerald family
(same "bright fill + near-black contrastText" pattern D3 established) so
the live dark mode (§7) inherits the new brand instead of a mismatched
leftover.

| Token | Hex | Role |
|---|---|---|
| `background.default` | `#0B1412` | |
| `background.paper` | `#11201D` | |
| elevated surface | `#16281F` | TableHead background, Tooltip fill |
| `divider` | `#24352E` | |
| `text.primary` | `#F1F5F4` | |
| `text.secondary` | `#94A3A0` | |
| `primary.main` | `#2DD4BF` | contrastText `#08120F` |
| `primary.dark` (hover) | `#14B8A6` | |
| `success.main` | `#4ADE80` | contrastText `#08120F` |
| `warning.main` | `#FBBF24` | contrastText `#08120F` |
| `error.main` | `#F87171` | contrastText `#08120F` |
| `info.main` | `#60A5FA` | contrastText `#08120F` |
| `secondary.main` | `#9CA6A2` | contrastText `#08120F` |

| Pair | Ratio |
|---|---|
| `primary.contrastText` / `primary.main` | 10.22:1 |
| `success.contrastText` / `success.main` | 10.92:1 |
| `warning.contrastText` / `warning.main` | 11.40:1 |
| `error.contrastText` / `error.main` | 6.88:1 |
| `info.contrastText` / `info.main` | 7.48:1 |
| `secondary.contrastText` / `secondary.main` | 7.60:1 |
| `text.primary` / `background.default` | 17.01:1 |
| `text.primary` / `background.paper` | 15.31:1 |
| `text.secondary` / `background.default` | 7.13:1 |

### 8.3 Typography

Same MUI-variant-reuse convention as §2 (every call site already writes
a variant name, never a raw size) — values updated to the brief's
hierarchy (Page title 44–48px, Section title 28–32px, Card title
20–22px, Body 16px, Caption 14px, Buttons 16px/600):

| Variant | Size | Weight | Line-height | Used for |
|---|---|---|---|---|
| h1 | 48px | 700 | 1.15 | reserved — upper Page-title bound |
| h2 | 32px | 700 | 1.25 | Section title (upper) |
| h3 | 28px | 700 | 1.3 | Section title (lower) |
| h4 | 44px | 700 | 1.15 | `PageHeader` title |
| h5 | 21px | 600 | 1.45 | Card title (`SectionCard`, `EmptyState`) |
| h6 | 19px | 600 | 1.45 | AppBar/sidebar-brand title |
| subtitle1 | 16px | 600 | 1.55 | |
| subtitle2 | 14px | 600 | 1.55 | |
| body1 | 16px | 400 | 1.6 | default body copy |
| body2 | 14px | 400 | 1.6 | secondary/dense copy |
| caption | 14px | 400 | 1.5 | bumped from 12px per brief |
| overline | 12px | 600 | 1.5 | uppercase, `0.06em` tracking |
| button | 16px | 600 | 1.5 | bumped from 14px per brief |

Line-height was increased across every body/caption/subtitle register
per the brief's "increase line-height everywhere" — **except** `h1`/`h4`
(the two Page-title-register variants), deliberately kept tight (1.15).
A loose line-height on 44–48px display text reads as a mistake, not
"airy"; every reference app this phase names (Preply, Airbnb, Notion,
Linear) keeps its own large display type tight and reserves generous
line-height for body copy, which is where the brief's intent actually
applies.

### 8.4 Shape

Per-component radii replace §3's single two-tier scale:

| Token | px | Applied to |
|---|---|---|
| `radiusPx.sm` | 8 | dense inline elements (nav rows, Tooltip, Skeleton default) |
| `radiusPx.md` | 16 | `shape.borderRadius` default — Button, TextField/OutlinedInput, Menu |
| `radiusPx.lg` | 24 | Card, rounded Paper, Dialog |
| `radiusPx.pill` | 999 | Chip / StatusPill |

### 8.5 Shadow

Replaces §3's "border, not shadow" depth philosophy outright — a
deliberate, requested departure. Cards now carry a real, soft resting
shadow plus a hover-triggered "floating" tier (translateY(-2px) + a
stronger shadow), both gated behind `prefers-reduced-motion` (existing
`MuiCssBaseline` mechanism, unchanged). Dialogs use the same "floating"
tier. Still exactly two tiers beyond `none` — `namedShadows.resting`/
`namedShadows.floating` — just softer/larger than §3's, and now actually
load-bearing for Card depth instead of a fallback for elevation-only
components.

### 8.6 Other changes in this phase

- **Buttons**: `minHeight: 44` on every size (also satisfies the brief's
  mobile 44px touch-target minimum for every button app-wide, not a
  mobile-only special case), generous horizontal padding, `disableElevation`.
- **Inputs**: `OutlinedInput` padding increased (`14px 16px`), a 2px
  focus-ring border colour change on `.Mui-focused`.
- **IconButton**: default (medium) size padding bumped to 10px so the
  total hit target is exactly 44px; `AppHeader`'s `ThemeToggle` and
  `NotificationsButton` dropped their own `size="small"` override so the
  header's icon buttons actually get this benefit, rather than staying at
  MUI's ~30px small-size target.
- **Nav (`MuiListItemButton`)**: selected state changed from Phase D1's
  left accent bar to a fully rounded, tinted-background "pill" (Preply's
  own selected-state language); rows are `minHeight: 48` with a 4px
  horizontal gutter (`NavSidebar`'s `List` now has `px: 1.5`) so the
  rounded pill doesn't touch the drawer's edges.
- **EmptyState**: gained a large (88px) soft-tinted circular illustration
  area with a default generic icon (no per-context icon existed at any of
  its 20+ call sites), a bigger friendly headline (`h5`, still a real
  heading — the previous `subtitle1` rendered as `<h6>`), unchanged
  single-action-slot API.
- **ErrorState / ErrorBoundary**: default copy changed from "Something
  went wrong" to the brief's own example, "We couldn't load this page" /
  "We couldn't load this right now. Please try again." — the literal
  wording the brief asked for, applied to both the query-level
  (`ErrorState`) and render-crash (`ErrorBoundary`) fallbacks.

### 8.7 Explicitly out of scope for this phase

- **Per-page `Stack spacing` sweep** for the brief's "24px between cards
  / 48px between sections." These are scattered literal props across
  dozens of page files, not a theme-level lever the way colour/radius/
  shadow are — a mechanical sweep of every page was judged disproportionate
  for a foundation-refresh phase. Many pages already use `spacing={3}`
  (24px), which already matches the "between cards" figure; documented
  here as the going-forward convention rather than retrofitted everywhere.
- **`PageContainer.tsx`**: already satisfied "max width, centered,
  consistent spacing" before this phase (Phase D4) — left unchanged.
- No backend, route, permission, React Query, validation, or business-logic
  file was touched, per this phase's explicit constraint.

## 9. Preply Redesign — Phase 2 (Trust-First Marketplace)

Visual-trust pass over the Tutor Card, Tutor Hero, and Tutor Profile page —
no backend/API/permission change, no invented data. Every fact shown below
traces to a real `TutorDto`/availability-slot field; where the requested
pattern had no real backing data (a bio, a certificate, a response-time
figure, a video), the pattern was either omitted entirely or replaced with
an honest, permanent placeholder — the same convention `ReviewsSection`/
`FaqSection` already established in Phase 3.

### 9.1 New component: `TrustIndicators`

`frontend/src/features/identity/components/TrustIndicators.tsx` — a single
row of real trust facts, each independently optional:

- **Verified** — shown only when `tutor.isApproved`.
- **Speaks {language}** — shown only when `tutor.language` is set.
- **Next available {Tehran date}** — shown only when the caller supplies a
  `nextAvailableLabel`, computed from the same `useTutorAvailabilitySlots`
  query `AvailabilityPreviewSection` already fetches (React Query dedupes
  both call sites onto one HTTP request — no new network cost). The
  indicator is silently omitted, never replaced with a "no availability"
  message, since a trust row is meant to reassure, not discourage.

Deliberately **not** shown, because no such field exists anywhere in
`TutorDto`/the Domain: Education, Experience, Response time, Video
available — these were named only as *examples* in the brief this
component implements, not a checklist to fill regardless of whether the
data is real. Adding any of them would require a new Domain field and a
product decision this UI-only phase is not authorized to make.

Currently mounted once, in `TutorProfileHero` — written to be reusable
anywhere a `TutorDto` is available (e.g. `TutorCard`), but not added there
in this phase: `TutorCard` already shows a verified checkmark inline next
to its heading, and adding an availability fact to every card in a search
grid would require one HTTP request per card (an N+1 query pattern this
phase's performance constraint rules out) unless a bulk
availability-summary endpoint is added later — flagged as a backend
dependency for a future phase, not solved here.

### 9.2 Tutor Card / Hero changes

- **`TutorCard.tsx`**: avatar grown 48→56px with a brand-tinted circular
  background (`alpha(primary.main, 0.12)` light / `0.22` dark) instead of
  plain `action.selected` grey — the same "soft tinted circle" language
  `EmptyState`'s illustration area already established, so the
  no-photo-yet placeholder itself reads as an intentional design choice.
  The hourly rate gained its own prominent row (`variant="h6"`, `primary.main`,
  bold) instead of being one more line in the meta-chip stack — a search
  grid's single strongest comparison-shopping signal, now scannable at a
  glance.
- **`TutorProfileHero.tsx`**: avatar grown 88→96px, same tinted-circle
  treatment as the Card. The separate "Verified" `Chip` and the
  "location · Speaks {language}" text line were replaced by one
  `TrustIndicators` row (Verified + Speaks {language} + Next available),
  so every trust fact reads as part of one consistent, scannable line
  instead of being split across two different visual treatments.
- **`TutorDetailPage.tsx`**: "Subjects & Languages" (the page's "About"
  anchor) restyled from a plain caption+chip stack into a small grid of
  tinted highlight boxes — reads as a real summary at a glance, and is
  ready to hold more content the moment a real bio field exists, without
  a second restyle. A new **"Certificates & Experience"** section was
  added (id `certificates`, not listed in the section nav — same
  precedent `TeachingInformationSection` already set for an anchor-only,
  non-nav section) — a permanent, honest placeholder for a field that does
  not exist on `TutorDto` today, deliberately without an "add" CTA (the
  Tutor's own offering-edit form has no matching field to link to).

### 9.3 Explicitly out of scope for this phase

- **`RecommendedTutors.tsx`'s own `RecommendedTutorCard`** is a real,
  separate duplicate of `TutorCard`'s rendering logic (own avatar-less
  layout, own "Speaks {language}" line, own price line) — flagged here as
  a genuine "no duplicated UI" violation worth closing in a future pass,
  but not touched in this phase: it is deliberately a denser, smaller card
  for a dashboard-embedded preview widget, and swapping in the full
  `TutorCard` (three stacked actions, larger footprint) risked making that
  widget disproportionately large relative to its neighboring dashboard
  sections, which this phase's brief did not ask to touch.
- **`ProfileCompletionCard.tsx`** was reviewed against this phase's "friendly
  completion card, never warning/error style" requirement and found
  already compliant (a `LinearProgress` + checklist, no alert styling) —
  left unchanged.

## 10. Preply Redesign — Phase 3 (Search Experience & Discovery)

Restructured `TutorSearchPage.tsx`'s own layout into the requested flow —
Search → Filters → Active Filters → Results count → Tutor Grid — by
splitting one previously-mixed `SearchHero` (search box + tip + result
count + active-filter chips, all in one component) into four single-purpose
components. No new query parameter, filter, sort option, or backend call
was added — every field/count/state shown already existed.

### 10.1 New/changed components

- **`SearchHero`** (`frontend/src/features/discovery/components/SearchHero.tsx`,
  rewritten) — now only the Subject search box + Search button, the
  page's single visual focal point. Gained a per-field clear (×) button
  (via `useFormContext`/`useWatch`, shown only once the field has a
  value) and a larger pill (60px min height, 1.125rem placeholder text).
  Result count and active filters moved out — see below.
- **`ActiveFiltersBar`** (new,
  `frontend/src/features/discovery/components/ActiveFiltersBar.tsx`) — the
  "Active Filters" step: an "Active filters:" label, one removable `Chip`
  per applied filter (`Chip`'s own `onDelete` — the exact removal
  mechanism `TutorSearchPage` already had, just extracted), and a single
  "Clear filters" action for all of them. Renders nothing when no filter
  is active.
- **`SearchResultsHeader`** (new,
  `frontend/src/features/discovery/components/SearchResultsHeader.tsx`) —
  the "Results count" step: `"{n} tutors found"` / `"1 tutor found"` /
  `"Searching…"`, marked `aria-live="polite"` so the updated count is
  announced to assistive tech the same way a sighted user sees it change.
  Deliberately has **no sort control** — `GET /tutors/search` has no sort
  parameter (`useSearchTutors`/`searchTutors` accept only
  subject/language/location/availableFrom + page/pageSize); a sort
  dropdown with no real effect would be a fake feature, not a UI
  improvement. Flagged as a backend limitation below, not built around.
- **`TutorFilterPanel`** (existing, restyled) — the Drawer gained a
  one-line subtitle ("Narrow down your search") under its "Filters"
  heading, and its caller (`TutorSearchPage`) now groups the same three
  fields it already had (Language, Location, Available from) under two
  `overline`-labelled sections ("Where & language" / "Availability")
  separated by a `Divider`, instead of one flat list — no new filter was
  added, this is presentation grouping over the existing three fields
  only.
- **Sticky search bar** — `SearchHero` + the `TutorFilterPanel` trigger
  button now sit inside one `position: "sticky", top: 88` container
  (the same `top: 88` offset `TutorDetailPage`'s own sticky booking rail
  already uses, so it sits flush below the fixed `AppHeader` rather than
  behind it) — visible while scrolling through results at any viewport,
  not just mobile.

### 10.2 Tutor Card / Grid refinements (Phase 2 follow-up)

- `TutorCard`'s meta block no longer repeats the Subject as a `Chip` —
  it's already the card's own heading; showing it twice added no
  information and slowed down comparing several cards at a glance. Only
  the Language chip remains (a fact genuinely distinct from the heading).
- `TUTOR_CARD_SX` (shared by `TutorCard`/`TutorCardSkeleton`) gained an
  explicit `height: "100%"`, and every results grid now sets
  `alignItems="stretch"` with a 24px gap (`gap={3}`, matching this
  document's own §8.7 "gap between cards: 24" convention) — cards on the
  same row now genuinely match height instead of relying on implicit
  flexbox stretch behavior.

### 10.3 Empty / Error states

- **Empty search** (`EmptyState`): gained a search-icon illustration and,
  only when a filter is active, a "Reset Filters" button wired to the
  same `handleClear` the existing "Clear filters" action already used —
  no new clearing mechanism, just a second, more prominent entry point to
  it from the empty state itself, per this phase's "NOT an error" framing.
- **Search error**: switched from the generic `ErrorState` (a single
  "Try again" action) to `UnavailableState`, offering **Try again**,
  **Clear filters** (only when a filter is actually active — nothing to
  clear otherwise), and **Go Home** — the "friendly recovery" set this
  phase's brief asked for, still never surfacing the backend's own error
  message (same `ApiRequestError`-hiding convention every other page
  already follows).

### 10.4 Explicitly out of scope / backend limitations

- **No sort control** — see `SearchResultsHeader` above. A real one needs
  a `sort` (or similar) query parameter added to `GET /tutors/search`,
  which is a backend/API change this UI-only phase is not authorized to
  make.
- **No live "available now" filter or per-card next-available fact on
  search cards** — same N+1 concern already flagged in §9.1
  (`TrustIndicators`): a bulk availability-summary endpoint would be
  needed to show this on a grid of cards without one HTTP request per
  card.
- **Filter fields stay free-text** (Language/Location/Subject) — a
  dropdown/autocomplete taxonomy would need either a canonical
  subject/language list in the Domain or a "distinct values already in
  use" endpoint; flagged in `PREPLY_UX_GAP_ANALYSIS.md` §6, not solved
  here, since it is a backend change.

## 11. Preply Redesign — Phase 4 (Tutor Profile Experience)

Rebuilt `TutorDetailPage.tsx` around a decision narrative — Hero → Why
learn with this tutor → Certificates & Experience → Teaching Style →
Availability → Learning Plans → Reviews → FAQ → Related tutors — instead
of a flat list of database-shaped sections. `PROFILE_SECTIONS` (the
visible section-nav row) is unchanged: About/Learning Plans/Availability/
Reviews/FAQ, still 5 links, still the same anchor ids — new sections
(Certificates, Teaching Style, Related tutors) get their own anchor id for
consistency but are reached by scrolling, not a nav link, same precedent
`TeachingInformationSection` already set in Phase 2.

### 11.1 New components

- **`ProfilePlaceholderSection`** (new,
  `frontend/src/features/identity/components/ProfilePlaceholderSection.tsx`)
  — the one shared "this part of the profile has no real data yet" layout,
  replacing four near-identical copies (Reviews, FAQ, Certificates &
  Experience, and the new Teaching Style). A neutral grey icon circle
  (`grey.100`/`grey.800`, not the brand-tinted circle `EmptyState`/
  avatars use) keeps these deliberately low-key — they have no
  call-to-action and nothing to celebrate, unlike an actionable empty
  state. Every caller supplies its own honest copy; the component invents
  nothing.
- **`MobileBookingBar`** (new,
  `frontend/src/features/identity/components/MobileBookingBar.tsx`) —
  Phase 4 PART 6's "fixed bottom CTA" for mobile/tablet
  (`display: { xs: "flex", md: "none" }`): the Tutor's real hourly rate +
  the same "Book Lesson" link every other CTA on this page already uses,
  pinned to the viewport bottom. Desktop is unaffected — the existing
  `position: sticky` right column already keeps the booking card in view
  there. `TutorDetailPage` reserves `MOBILE_BOOKING_BAR_HEIGHT` (72px) of
  bottom spacing (mobile/tablet only) so this bar never covers the page's
  last section.
- **`RelatedTutorsSection`** (new,
  `frontend/src/features/identity/components/RelatedTutorsSection.tsx`) —
  "Related tutors," rendered only because it's backed by a real,
  already-supported capability: `GET /tutors/search` (the exact endpoint
  `TutorSearchPage`/`RecommendedTutors` already call), filtered to this
  Tutor's own `subject`, with the Tutor being viewed excluded from their
  own "more like this" list. Renders nothing if the Tutor has no subject,
  or no *other* Tutor teaching it exists. **This is the one genuinely new
  network request this phase introduces** — every other change in Phase 4
  reuses an existing query. Flagged here rather than glossed over: PART 1
  explicitly scopes this section to "only if already supported,
  otherwise omit," and reusing the existing search endpoint (not a new
  one) was judged to satisfy that — but it is a real, additional request
  per profile view, not a free reuse of already-fetched data the way
  `TrustIndicators`' availability fact is.

### 11.2 Hero changes (PART 2)

- **Pricing prominence**: the hourly rate now renders directly in the
  Hero's action column (large, bold, `primary.main`), above the Book
  Lesson button — previously only shown further down the page.
- **Language badge**: the Tutor's spoken language is now its own `Chip`
  next to location, instead of only appearing as text inside
  `TrustIndicators`' trust row. `TrustIndicators` gained a `showLanguage`
  prop (default `true`) so the Hero can opt out of showing the same fact
  twice in two different treatments — every other caller is unaffected.

### 11.3 Section-by-section notes

- **"Why learn with this tutor"** (renamed from "Subjects & Languages",
  merged with the old standalone "Teaching Information"): a grid of
  highlight boxes for Subject and Session lengths only — Language and
  Hourly rate are deliberately not repeated here since both are now
  prominent in the Hero itself (same declutter reasoning `TutorCard`'s
  meta block already applies to its own Subject chip vs. heading).
- **About / long text**: no bio field exists in `TutorDto` — there is no
  long paragraph text to apply "reading rhythm" to. Not fabricated; noted
  here so this isn't mistaken for an oversight.
- **Teaching Style**: new, honest `ProfilePlaceholderSection` — no such
  field exists on `TutorDto` either.
- **Availability**: gained a one-line legend ("A preview of this tutor's
  next open times (Tehran time) — select one to start booking.") and a
  "View full schedule" link into the existing booking route — booking
  logic itself (slot selection, the wizard) is unchanged; `selected`/
  "booked" visual states were not added here since this section only ever
  shows already-open (non-consumed) slots, so there is nothing to mark as
  booked, and it isn't a multi-select flow.
- **Reviews / FAQ**: same honest copy as before, now using
  `ProfilePlaceholderSection` for a consistent look. No accordion exists
  for FAQ — there is no real Q&A content to accordion over, and inventing
  placeholder questions would violate "never fabricate."
- **Section nav bug fix**: `TutorProfileSectionNav`'s sticky `top` was
  `0` (sitting directly behind the fixed `AppHeader` instead of flush
  beneath it) — corrected to `88`, the same offset this page's own
  booking rail and Phase 3's sticky search bar already use.

### 11.4 Loading

`TutorProfileSkeleton` gained a thin 5-item skeleton row for the section
nav (previously absent, causing a layout shift once the real nav
appeared) and now renders 4 placeholder cards in the left column instead
of 2, better approximating the page's real section count without trying
to mirror every one exactly.

## 12. Preply Redesign — Phase 5 (Booking Experience)

Restructured `BookSessionPage.tsx`'s 5-step wizard (Choose Tutor → Choose
Date → Choose Time → Review → Confirm) around one persistent, reusable
Tutor summary instead of two separate, redundant summaries — no booking
rule, validation, permission, or `POST /sessions` contract changed.

### 12.1 Removed duplication (PART 2/5)

- **`TutorSummaryCard`** now renders once, outside every per-step block,
  and stays visible across all 5 steps — the single place "who am I
  booking, at what rate" is shown for the whole wizard. Previously it only
  rendered on "Choose Tutor" (step 0).
- **`BookingSummaryCard`** lost its `tutor` prop entirely and no longer
  repeats Tutor/Subject/Price rows that `TutorSummaryCard` already shows
  above it — it now shows only Date, Time, Duration, and Platform, the
  facts genuinely unique to Review/Confirm.
- **"Choose Tutor" step** was evaluated for outright removal (PART 1: "can
  this step be simpler?") since the persistent summary card already
  answers everything it showed. Kept as its own step (removing a step
  outright was judged the higher-risk option — its label is a real,
  expected part of the wizard's navigation) but its content was reduced
  from a full "Learning Plans" `SectionCard`+`EmptyState` combo to one
  plain sentence.

### 12.2 Progress indicator (PART 3/4)

The mobile-only progress text ("Step X of Y: Label") gained real visual
weight instead of being a single sentence: a small "Step X of Y" caption,
a bold step-name heading, and a thin `LinearProgress` bar
(`aria-label="Step N of 5: {label}"`) showing how far through the wizard
the user is. Desktop's `Stepper` is unchanged — it already showed
active/completed state via MUI's own built-in behavior.

### 12.3 Focus management (PART 12)

Each step's content is now wrapped in its own `role="group"`
`aria-label={STEPS[n]}` region, focused automatically via a callback ref
the moment it mounts — whether that's from clicking Continue/Back or from
the initial loading skeleton finishing. A screen-reader user is told
which step they landed on instead of focus silently vanishing with the
button/skeleton that was just removed from the DOM. (A plain
`useEffect([activeStep])` was tried first and doesn't work for the
initial-load case — the effect fires while the page is still showing
`BookingPageSkeleton`, before the real step region exists, and
`activeStep` never changes again once it mounts; a callback ref sidesteps
this by firing exactly when each region's DOM node actually mounts.)

### 12.4 Confirmation (PART 8)

`BookingStatusBanner`'s success view: bigger, friendlier heading ("Your
lesson is booked!"), a sentence explaining what happens next (the tutor
is notified; join details appear on the session once ready), and three
shortcuts alongside the existing "View Lesson" — **My Lessons**
(`paths.scheduling.studentScheduleBase`), **Message Tutor** (reuses the
exact same `useStartConversation` mutation `TutorProfileHero`'s
`SendMessageAction` already calls — shown whenever the Tutor id is known,
not role-gated again since this banner only ever renders inside
`BookSessionPage`, whose route already restricts booking to Student/
ParentGuardian), and **Return Home**. Every shortcut is an existing route
or capability — nothing new was added to support this.

### 12.5 Availability selection (PART 6)

`AvailabilityCard`: border width 1→2px and an elevation-on-hover/selected
treatment (`boxShadow`, respecting `prefers-reduced-motion`) for a clearer
selected/hover distinction, padding 16→20px. No "unavailable" visual
state was added — this component only ever renders already-open,
non-consumed slots (consumed ones are filtered out before reaching it),
so there is nothing un-bookable to visually distinguish; inventing one
would mean rendering slots that don't exist in the returned data.

### 12.6 Explicitly out of scope

- **Sticky Back button** on Choose Date/Choose Time: already existed for
  Review/Confirm's action row (`mobileStickyActionsSx`); now applied to
  every step's own action row for consistency, not just those two.
- **Loading skeleton**: `BookingPageSkeleton` already mirrors the
  Tutor-summary-then-content shape this phase's architecture still uses —
  left unchanged.
- **Error copy**: booking failures already surface the Domain layer's own
  human-readable message (ADR-008) rather than a raw exception, and the
  409 slot-conflict case already has dedicated, friendly recovery
  copy/actions — both reviewed against this phase's "no technical
  wording" requirement and found already compliant.

## 13. Phase 7 — Tutor Onboarding Wizard UX Polish

UX-only pass over `TutorOnboardingWizardPage` (ADR-024) — no Domain,
Application, API, or schema change; no new package (every animation below
uses MUI's built-in `Fade`/`Grow`/`Collapse`, already neutralized under
`prefers-reduced-motion` by the global `MuiCssBaseline` override in
`theme.ts`).

- **Welcome screen**: a one-time `OnboardingWelcomeScreen`, shown before
  Step 1 only for a Tutor who has never opened the wizard — gated on the
  existing `tutorflow.onboardingStep.*` `localStorage` key rather than a
  new flag, so a returning Tutor (any remembered step, including the
  tests that seed one directly) never sees it again. Renders before the
  `useTutor` fetch even resolves, since it needs no server data.
- **Stepper**: `Step`/`StepLabel` became `Step`/`StepButton` wrapping
  `StepLabel` — every already-visited step is a real, keyboard-operable
  button (`goToStep`), future steps stay `disabled`. A custom
  `StepIconComponent` swaps the numbered circle for a checkmark
  (`CheckCircleRoundedIcon`, the same icon `ProfileCompletionCard` already
  uses) once a step is completed.
- **Progress**: the mobile progress block gained a completion percentage
  and a short encouraging line (`encouragingMessageForProgress`, a pure
  function with its own unit test, mirroring `profileCompletion.ts`'s
  convention) alongside the existing "Step X of Y"/`LinearProgress` pair.
- **Transitions**: the seven duplicated per-step `Stack` wrappers were
  extracted into one local `StepPanel` helper that also wraps children in
  `Fade in appear` — each step already mounts/unmounts via its own
  ternary, so this is a free fade-in with no new state. `PricingStep`'s
  conditional trial-lesson-price field became a `Collapse`; newly-added
  rows in `TeachingInfoStep`'s subject list get a `Grow` entrance
  (removal stays instant — no exit-orchestration needed).
- **Empty states**: `AvailabilityStep` shows a compact hint when a Tutor
  has zero declared slots; `MediaStep` shows a short guidance line when
  photo/video/gallery are all empty. Both are static explanatory copy
  only — no fabricated status, consistent with ADR-024's verification
  section.
- **Skeleton**: `OnboardingWizardSkeleton` (title block, stepper row,
  field-height bars, a button) replaces the page's loading spinner, the
  same hand-tailored convention `TutorProfileSkeleton` already uses.
- **Success feedback**: each of the four autosave points now fires a
  short (2.5s) success snackbar via the existing `NotificationProvider` —
  no new inline UI, reusing the same mechanism autosave-failure and
  publish already used.
- **Review & Publish**: the flat preview list became three `SectionCard`s
  (Personal, Teaching & Subjects, Pricing), each with an `Edit` action
  (`SectionCard`'s existing top-right `action` slot) that jumps back to
  the relevant step. The two backend-required fields (Subject, Hourly
  rate) get a warning-tinted highlight when unset, and a publish-readiness
  `Alert` now summarizes the same requirement above the preview. The
  existing `ProfileCompletionCard` is unchanged — still the one
  completion checklist, extended not replaced, per ADR-024.
- **Sticky mobile actions**: applied `BookSessionPage`'s existing
  `mobileStickyActionsSx` pattern (Phase 5) to every onboarding step's
  action row — kept as a page-local copy, not extracted to a shared
  module, to keep this phase's diff scoped to the onboarding page only.

---

# Part II — Design System Reference (Phase DS2)

> Everything above this line (§1–13) is a **phase history** — what
> changed, in what order, and why, kept for the reasoning it records.
> Everything below is a **standing reference** — the answer to "what do
> I use for X" without reading thirteen sections of changelog. Nothing
> below invents a new convention: every rule, value, and pattern here
> already exists in `frontend/src/app/theme.ts` or in a real, shipped
> page — this Part organizes and documents it, it doesn't change it.
> Where a value lives in `theme.ts`, that file is still the source of
> truth; if the two ever disagree, `theme.ts` is right and this doc has
> drifted.

## 14. Design Tokens Reference

Every token below is defined once, in `frontend/src/app/theme.ts`, and
consumed by both `theme` (light) and `darkTheme` (dark) via the shared
`createAppTheme(mode)` function. See §8 for the original rationale
behind the palette/type/shape choices; this section is the lookup
table, not the argument.

### 14.1 Colors

**Light mode**

| Token | Value |
|---|---|
| Primary | `main:#0F766E` `dark:#115E59` `light:#14B8A6` `contrastText:#FFFFFF` |
| Secondary | `main:#374151` (`neutral[700]`) `contrastText:#FFFFFF` |
| Success | `main:#16A34A` `contrastText:#111827` (dark text — see note) |
| Warning | `main:#D97706` `contrastText:#111827` (dark text — see note) |
| Error | `main:#DC2626` `contrastText:#FFFFFF` |
| Info | `main:#2563EB` `contrastText:#FFFFFF` |
| Text | `primary:#111827` `secondary:#6B7280` `disabled:#9CA3AF` |
| Background | `default:#F8FAFC` `paper:#FFFFFF` |
| Divider | `#E5E7EB` |
| Neutral/grey scale | `50:#F9FAFB 100:#F3F4F6 200:#E5E7EB 300:#D1D5DB 400:#9CA3AF 500:#6B7280 600:#4B5563 700:#374151 800:#1F2937 900:#111827` |

**Dark mode**

| Token | Value |
|---|---|
| Primary | `main:#2DD4BF` `dark:#14B8A6` `light:#5EEAD4` `contrastText:#08120F` |
| Secondary | `main:#9CA6A2` `contrastText:#08120F` |
| Success | `main:#4ADE80` `contrastText:#08120F` |
| Warning | `main:#FBBF24` `contrastText:#08120F` |
| Error | `main:#F87171` `contrastText:#08120F` |
| Info | `main:#60A5FA` `contrastText:#08120F` |
| Text | `primary:#F1F5F4` `secondary:#94A3A0` `disabled:#5B6864` |
| Background | `default:#0B1412` `paper:#11201D` (elevated surfaces: `#16281F`) |
| Divider | `#24352E` (border: `#2E4038`) |
| Neutral pill text (StatusPill outlined, dark) | `#CBD2D9` |

The `grey` palette key maps to the same Tailwind neutral scale in
**both** modes — dark mode does not get its own grey ramp, only its own
surface/text/primary colors above.

**Why success/warning use a dark `contrastText`:** white text on
`#16A34A`/`#D97706` measures roughly 3.3:1 / 3.2:1 — below WCAG AA's
4.5:1 body-text threshold. Dark text (`#111827`) on the same
backgrounds clears ~5.4:1 / ~5.6:1. Error keeps white contrastText
since `#DC2626` is dark enough to clear AA with white (~4.8:1) —
`theme.test.ts` enforces all of this automatically so a future palette
tweak can't silently reintroduce a failing pair.

### 14.2 Typography

Font stack (`fontFamily`): `-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif` — applied to every variant, no per-variant override. A second stack, `monoFontFamily = "ui-monospace, SFMono-Regular, Menlo, Consolas, monospace"`, is exported separately for raw-id displays (`CopyableId`, etc.) and applied ad hoc via `sx`, not wired into the theme's typography object.

| Variant | Size | Line height | Weight | Letter spacing | Text transform | Intended use |
|---|---|---|---|---|---|---|
| h1 | 3rem (48px) | 1.15 | 700 | — | — | reserved, not yet used on a real page |
| h2 | 2rem (32px) | 1.25 | 700 | — | — | reserved |
| h3 | 1.75rem (28px) | 1.3 | 700 | — | — | reserved |
| h4 | 2.75rem (44px) | 1.15 | 700 | — | — | `PageHeader`'s page title |
| h5 | 1.3125rem (21px) | 1.45 | 600 | — | — | `SectionCard`'s section title; `DialogTitle` |
| h6 | 1.1875rem (19px) | 1.45 | 600 | — | — | sub-section headings |
| subtitle1 | 1rem (16px) | 1.55 | 600 | — | — | emphasized body line |
| subtitle2 | 0.875rem (14px) | 1.55 | 600 | — | — | dense emphasized text (table headers) |
| body1 | 1rem (16px) | 1.6 | 400 | — | — | default body copy, form labels |
| body2 | 0.875rem (14px) | 1.6 | 400 | — | — | secondary/dense body copy |
| caption | 0.875rem (14px) | 1.5 | 400 | — | — | field helper/error text, small labels |
| overline | 0.75rem (12px) | 1.5 | 600 | 0.06em | uppercase | rarely used, small category labels |
| button | 1rem (16px) | 1.5 | 600 | — | none | all `Button`/`Chip` text |

`h1`–`h4` deliberately keep a tight ~1.15 line-height (large display
text reads better tight); every other variant was widened for reading
comfort. `caption` (12→14px) and `button` (14→16px, 600 weight) were
both bumped up from MUI's defaults for legibility/touch-target feel.

**Do**: pick a variant by *role* (page title → h4 via `PageHeader`,
section title → h5 via `SectionCard`, helper/error text → caption via
`FormTextField`'s built-in helper text), not by eyeballing a font size.
**Don't**: hand-type a `fontSize` in `sx` when an existing variant
already matches — every hardcoded `sx={{ fontSize: N }}` in the app
today is a deliberate one-off icon-size override, never body text (see
§15.1).

### 14.3 Spacing

`spacingUnitPx = 8`, passed straight through to MUI's `spacing()`
function — `theme.spacing(n) = n × 8px`, MUI's own default multiplier,
unchanged. See §17.2 for the page/section/component spacing hierarchy
built on top of this unit.

### 14.4 Border Radius

| Token | px | Used by |
|---|---|---|
| `sm` | 8 | `Skeleton` (default), `Tooltip` |
| `md` | 16 | theme's default `shape.borderRadius` (so plain `TextField`/`Button`/`Menu` inherit it), `Button`, `OutlinedInput`, `Skeleton` (rounded variant), `ListItemButton` |
| `lg` | 24 | `Card`, `Paper` (rounded variant), `Dialog` |
| `pill` | 999 | `Chip` (and therefore `StatusPill`) |

### 14.5 Elevation / Shadows

Three tiers only — MUI's 25-step default elevation ramp is flattened,
not extended:

| Tier | Light | Dark |
|---|---|---|
| `none` (elevation 0) | `none` | `none` |
| `resting` (elevation 1–6) | `0px 1px 3px rgba(17,24,39,.06), 0px 1px 2px rgba(17,24,39,.04)` | `0px 1px 3px rgba(0,0,0,.40), 0px 1px 2px rgba(0,0,0,.28)` |
| `floating` (elevation 7–24) | `0px 12px 24px rgba(17,24,39,.10), 0px 4px 8px rgba(17,24,39,.06)` | `0px 12px 24px rgba(0,0,0,.48), 0px 4px 8px rgba(0,0,0,.36)` |

`Card` uses `resting` at rest and `floating` on hover (with a 2px
`translateY` lift); `Dialog` always uses `floating`. Dark mode uses a
plain-black shadow color at much higher alpha rather than the light
mode's near-black `rgba(17,24,39,·)` at low alpha — same offsets/blur
either way, just enough opacity to read against a dark surface.

### 14.6 Motion

See §18 for the full motion catalog (durations, easing, and the
`prefers-reduced-motion` strategy) — this entry exists so token
lookups don't have to guess which section owns it.

### 14.7 Breakpoints

**Not overridden — MUI's defaults are used as-is**: `xs:0 sm:600 md:900 lg:1200 xl:1536`. This is a deliberate choice (not an oversight): the app's one drawer-collapse point (`AppLayout`) and every wizard's Stepper→LinearProgress swap already land on `md`, and nothing built so far has needed a bespoke breakpoint. See §20 for how real pages behave at specific viewport widths.

### 14.8 Z-index

**Not overridden — MUI's defaults are used as-is**: `mobileStepper:1000 appBar:1100 drawer:1200 modal:1300 snackbar:1400 tooltip:1500`. Nothing in the app has ever needed to reach outside this ramp (no custom overlay stacks exist).

### 14.9 Opacity / Alpha

`theme.ts` itself only has one hardcoded alpha pattern — `ListItemButton`'s selected-state background, `` `${primary.main}1F` `` (12%) at rest and `` `${primary.main}2E` `` (18%) on hover, identical percentages in both modes.

Every tinted-background badge/hint *outside* theme.ts (i.e. in feature components) uses MUI's `alpha()` helper directly against `theme.palette.primary.main`, and by convention doubles the light-mode percentage for dark mode (a darker background needs more alpha to read the same tint):

| Component | Light | Dark |
|---|---|---|
| `EmptyState` icon badge | 10% | 18% |
| `OnboardingWelcomeScreen` icon badge | 10% | 18% |
| `TutorProfileHero`, `TutorCard` accents | 12% | 22% |
| `TutorDetailPage` section accent | 6% | 14% |
| `AvailabilityStep`, `MediaStep` empty-state hints | 6% | 12% |

**Do**: pick one of these existing pairs (10/18, 12/22, or 6/12–14) rather than inventing a new percentage — they're not yet centralized as named constants, but the pattern is consistent enough to reuse by eye. **Don't** use a flat opacity (e.g. a plain `opacity: 0.6` on a whole element) for a *background tint* — that's for genuinely de-emphasizing content (e.g. `WeeklyAvailabilityCalendar` dims past slots to `opacity: 0.6`), a different use case from a tinted badge background.

## 15. Component Catalog

Every entry below names the real file(s) implementing it and at least
one real call site — this catalog documents what's already built, it
doesn't propose new components.

### 15.1 Button / IconButton

No wrapper exists — raw MUI `Button`/`IconButton`, styled entirely by
theme overrides (`theme.ts`: `MuiButton`, `MuiIconButton`). 44px min
height everywhere (touch target), no uppercase, 16px/600 button text, a
visible focus ring, and a 0.98 press-scale (respecting reduced motion).

**Variants/states**: `contained` (primary action), `outlined`
(secondary action, and the *first step* of any destructive action —
see below), `text` (low-emphasis, e.g. table row "View all" links).
Sizes: default (44px), `large` (52px, wizard primary CTAs), `small`
(36px, dense contexts like table row actions).

- **Do**: primary action per screen = one `contained` button. Secondary
  action = `outlined`. A destructive action is *proposed* with
  `variant="outlined" color="error"` and only becomes `variant="contained" color="error"` inside the confirmation dialog itself
  (`useConfirmDialog()` — see §15.4) — never mutate directly from the
  outlined button.
- **Don't**: put two `contained` buttons of equal visual weight in the
  same row (forces the user to guess which is primary) — this is why
  `SessionDetailPage`'s Reschedule button was changed from `outlined`
  to `contained` in this phase: it was the only action in its row, so
  it should read as primary, not secondary.
- **Accessibility**: every icon-only `IconButton` in the app carries an
  `aria-label`. A disabled icon-only control that still needs an
  explanatory `Tooltip` is wrapped in a `<span>` first (MUI's own
  documented pattern for disabled children inside `Tooltip` — disabled
  elements don't fire hover/focus events on their own).

### 15.2 Form controls

`frontend/src/shared/components/forms/`: `Form` (combines RHF's
`FormProvider` with a native `<form noValidate>`), `FormTextField`
(wraps MUI `TextField`; always `fullWidth`, wires `fieldState.error` →
`error`/`helperText`), `FormSelect` (same wiring, MUI `TextField
select` + `MenuItem`s), `FormCheckbox` (wraps `Checkbox` +
`FormControlLabel` + `FormHelperText`), `IdLookupForm` (a composed
"find by id" form reused by every detail page, since the API only
supports lookup-by-id).

Raw MUI controls used deliberately *outside* these wrappers: a plain
`Switch` in `PricingStep` (bound by hand via `useController` — no
`FormSwitch` wrapper exists yet, and this is the only Switch in the
app so a wrapper isn't justified). `Radio`/`RadioGroup` and
`Autocomplete` appear only in the dev style guide as token demos — no
real feature page uses either today.

**Do**: build every real form through `Form` + `FormTextField`/
`FormSelect`/`FormCheckbox`, even for a single field. **Don't** hand-
wire a raw MUI input to `useController` unless the control genuinely
has no wrapper yet (as `PricingStep`'s `Switch` does) — see §16 for the
full form-standards writeup.

### 15.3 Card / SectionCard

`frontend/src/shared/components/SectionCard.tsx` — the one section
container for dashboards, the booking/onboarding wizards, session
pages, and the Tutor profile. Props: `title` (required), `action?`
(top-right slot — e.g. a "View all" link or, in the onboarding Review
step, an "Edit" jump-back button), `children`, `id?` (anchor-nav
target), `headingComponent?` (defaults `"h5"`; pass `"h2"` only when
the page's own title is a real `<h1>`, currently only under
`TutorProfileHero`). Renders as `<Card variant="outlined" component="section" aria-labelledby={headingId}>` with a generated heading id, so
screen-reader users can jump between sections the way sighted users
scan cards.

**Do**: reach for `SectionCard` for any titled block of content on a
page. **Don't** hand-roll `<Card variant="outlined"><CardContent><Typography variant="subtitle1" fontWeight={600}>{title}</Typography>...` — this exact pattern exists in ~16 places today and is
the single largest documented inconsistency in the app (§22).

### 15.4 Dialog

Two real dialogs exist, and they're two different — both legitimate —
archetypes:

- **Confirm dialog** (`shared/context/ConfirmDialogProvider.tsx`,
  `useConfirmDialog().confirm({ title, description?, confirmLabel?, cancelLabel?, destructive? }): Promise<boolean>`): `DialogTitle` (no
  close-X — Cancel already dismisses) → optional
  `DialogContent`/`DialogContentText` → `DialogActions` footer with
  Cancel (`color="inherit"`) and Confirm (`variant="contained"`,
  `color={destructive ? "error" : "primary"}`, `autoFocus`). Backdrop/
  Escape resolves the promise `false`, same as Cancel. Used by every
  "are you sure" flow in the app (`TutorApprovalActions`,
  `SessionActions`) — **do** reach for this hook instead of building a
  bespoke confirm dialog.
- **Content-form dialog** (`AddTeachingTimeDialog`): `DialogTitle` with
  an inline close-`IconButton` (`aria-label="Close"`) → `DialogContent`
  containing a full `Form`/`FormTextField`/`FormSelect` + a full-width
  submit button *inside* the content (no separate `DialogActions`
  footer) — appropriate when there's real content to scan and a close-X
  is the faster dismiss path.

**Do**: pick the archetype that matches your dialog's purpose (a yes/no
decision → confirm dialog; a real form → content-form dialog with a
close-X). **Don't** invent a third shape — with only two dialogs in the
app today, a new one should look like whichever of these two it's
closer to.

### 15.5 Alert / Snackbar

`shared/context/NotificationProvider.tsx` — a single-at-a-time queue
(`notify({ message, severity?, autoHideDurationMs? })`; default
severity `"info"`, default duration 5000ms). Renders one bottom-center
`Snackbar` + `Alert variant="filled"`; a queued second call waits for
the current one's exit transition to finish before showing, so
notifications never stack or overlap.

**Do**: use a short `autoHideDurationMs` (Phase 7 uses 2500ms) for a
low-stakes confirmation like "Progress saved," and the 5000ms default
for anything the user should have time to actually read (a terminal
error). **Don't** build a local, one-off Snackbar — every success/error
toast in the app goes through this one provider.

### 15.6 Skeleton

Every `*Skeleton.tsx` (e.g. `TutorProfileSkeleton`,
`OnboardingWizardSkeleton`, `TutorCardSkeleton`,
`ChildSummaryCardSkeleton`, `BookingPageSkeleton`,
`PendingTutorCardSkeleton`, `SessionCardSkeleton`,
`AdminSessionCardSkeleton`, `ConversationListItemSkeleton`,
`AvailabilitySummaryCardSkeleton`) follows one rule: **mirror the real
component's layout exactly** (same card shape, same avatar size, same
number of text lines, same button-height rectangle) so resolving the
query never shifts the page — and each carries a `data-testid` for test
targeting. Card-shaped components wrap their bars in the same
`Card variant="outlined"`/`CardContent` the real component uses; full-page
skeletons (`BookingPageSkeleton`, `TutorProfileSkeleton`,
`OnboardingWizardSkeleton`) are a bare `Stack` mirroring the page's
actual section layout, including breakpoint-gated blocks where the
real page has responsive layout changes.

**Do**: build a new skeleton by copying an existing one's structure and
adjusting bar counts/sizes to match the new component. **Don't** use a
generic spinner (`LoadingState`) for anything that renders a
recognizable card/list shape — only use `LoadingState` for content with
no stable shape to mirror (e.g. a dialog's in-flight submit state).

### 15.7 EmptyState

`shared/components/feedback/EmptyState.tsx` — `title` (required),
`description?`, `action?`, `icon?` (defaults to a generic inbox glyph).
An 88px tinted circular badge (`alpha(primary.main, 10% light / 18%
dark)` — see §14.9) around the icon, `h5` title, optional `body1`
secondary description (max-width 440px), optional action button below.
20+ call sites app-wide (e.g. `InboxPage`'s "No conversations yet" vs.
"No matching conversations," `BookSessionPage`'s "no children yet" with
an action CTA).

**Do**: reuse this component for any list/collection that can
genuinely be empty, with copy specific to *why* it's empty (first-time
vs. filtered-to-zero are different messages, not the same component
instance). **Don't** fabricate a status or affordance the empty state
implies exists but doesn't (see §19's ADR-024 cross-reference on this
exact principle in the onboarding wizard's Verification step).

### 15.8 StatusPill

`shared/components/feedback/StatusPill.tsx` — wraps `Chip`. `tone:
"neutral"|"info"|"success"|"warning"|"critical"`, mapped to Chip
`color` (`neutral`→`default`+`outlined`, everything else →filled).
Deliberately generic, not tied to any one domain enum — feature code
maps its own status (`SessionStatusBadge`, `PlanBadge`) to a tone and
renders `StatusPill`.

**Do**: build a small local mapping function (status → tone) and
render `StatusPill`, as `SessionStatusBadge`/`PlanBadge` already do.
**Don't** render a raw `Chip` with a hand-picked color for a status —
that bypasses the one place tone semantics are centralized.

### 15.9 DataTable

`shared/components/table/DataTable.tsx` — `columns` (key/header/
render/align/width), `rows`, `getRowKey`, `isLoading?`/`error?`/
`onRetry?` (delegates to `LoadingState`/`ErrorState`), `emptyState?`
(delegates to `EmptyState`), `pagination?` (translates the backend's
1-based page convention to MUI's 0-based `TablePagination` so no
consumer has to remember the offset), `onRowClick?` (adds `hover`,
`tabIndex={0}`, and an Enter/Space `onKeyDown` handler — clickable rows
are keyboard-operable by default). Currently has one production call
site (`RelationshipsPage`) plus the dev style guide.

**Do**: reach for this for any new tabular list rather than hand-
building `Table`/`TableHead`/`TableBody` — loading/error/empty states
and pagination-offset translation all come for free.

### 15.10 Wizard / Stepper

Not a shared component — a **pattern**, followed identically by the
only two multi-step flows in the app, `BookSessionPage` and
`TutorOnboardingWizardPage`: a `STEPS` string-array const; desktop
`<Stepper activeStep alternativeLabel>` (hidden below `md`); a mobile-
only "Step X of Y" + step name + `LinearProgress` block (replacing the
Stepper entirely below `md`); each step's content wrapped in its own
`role="group" aria-label={STEPS[n]}` region with a callback `ref` that
focuses it on mount (so screen-reader focus lands on the new step
after Continue/Back/skeleton-resolve, not nowhere); a page-local
`mobileStickyActionsSx` pinning the primary action row to the viewport
bottom on mobile.

**Do**: copy this exact shape for any future multi-step flow — desktop
Stepper + mobile LinearProgress + per-step focus region + sticky mobile
actions are all proven, tested patterns, not one-offs. **Don't**
extract a shared `<Wizard>` component pre-emptively — with only two
instances, the duplication is still cheap enough that a premature
abstraction would cost more than it saves (per this repo's own
no-speculative-abstraction convention).

### 15.11 Tooltip

`theme.ts` defaults every `Tooltip` to `arrow: true`. Used for exactly
two purposes app-wide: (1) labeling an icon-only control (`CopyableId`'s
copy button, `ThemeToggle`, `NavigationItem` when the sidebar is
collapsed to icons-only), (2) explaining *why* a control is disabled
(`StudentRosterCard`/`NextLessonHeroCard`'s "coming soon" placeholders,
wrapped in a `<span>` per §15.1's disabled-child note).

**Don't** use `Tooltip` to hide information a sighted user needs to
complete a task — it's for supplementary labeling only, never the sole
source of required information (nothing in the app does this today;
worth stating so it stays that way).

### 15.12 Avatar

Two patterns: a **real photo** (`TutorCard`, `TutorProfileHero`, sized
by prominence — 40-ish px in a list row, 96px on the profile hero) and
a **deliberate icon-in-circle placeholder** for entities with no photo
field at all (`ChildSummaryCard`, `StudentSummaryCard`,
`TutorSummaryCard`, `StudentRosterCard` — all identical:
`sx={{ bgcolor: "action.selected" }}` + `<PersonRoundedIcon color="disabled" aria-hidden="true" />`).

**Do**: use the icon-placeholder pattern verbatim for any entity that
has no photo capability (Students/children currently have none) —
**don't** invent a new placeholder style per feature.

### 15.13 Chip

Three purposes, all via the themed pill-shaped `Chip` (`theme.ts`:
`borderRadius: radiusPx.pill`, subtitle2 font weight): (1) tag/metadata
display (language, subject, "Minor," "Verified" — usually `outlined`),
(2) dismissible filter chips (`ActiveFiltersBar`, `onDelete`), (3) a
clickable *selection* control (`BookSessionPage`'s date picker —
`color`/`variant` flip between `default`/`outlined` and
`primary`/`filled` for unselected/selected). `StatusPill` (§15.8) is
built on this same primitive.

**Do**: use purpose (3)'s selected/unselected color+variant flip for
any future chip-as-control use case, rather than inventing a new
selected-state treatment.

## 16. Form Standards

- **Labels**: always a visible, persistent label (`FormTextField`/
  `FormSelect`'s MUI `TextField` label, `FormCheckbox`'s
  `FormControlLabel` text) — never placeholder-only. `placeholder`
  (e.g. `PersonalInfoStep`'s `"How students will see your name"`) is
  used only for a *format example*, never as a substitute for the
  label itself, since placeholder text disappears the moment a user
  starts typing and isn't read as a label by most screen readers.
- **Required vs. optional**: a field with no suffix is required. An
  optional field says so explicitly in its label or helper text —
  either inline (`"Other languages you speak (comma-separated)"`,
  `"Level (optional)"` in `TeachingInfoStep`) or via helper text
  (`PricingStep`'s hourly rate: `"Required before you can publish your
  profile."`). **Do not** mark required fields with a red asterisk
  convention — none exists in the app today, and introducing one now
  would need to be applied everywhere at once to avoid looking
  inconsistent; the inline-copy convention above is the established
  one.
- **Validation timing**: one Zod schema per form (or, for a multi-step
  wizard, one schema spanning every step —
  `tutorOnboardingSchema`/`bookSessionSchema`), resolved via
  `@hookform/resolvers/zod`. A single-page form validates on submit
  (RHF's default). A wizard validates **per step**, via
  `form.trigger([...that step's field names])` before advancing —
  never the whole schema at once mid-wizard, since later steps'
  required fields (e.g. Pricing's hourly rate) shouldn't block an
  earlier step's Continue. This is Presentation-layer validation only
  (ADR-007) — the backend remains the sole authority, so a value that
  slips past this schema is still rejected server-side.
- **Helper text vs. error text**: never both at once.
  `FormTextField`/`FormSelect` already implement this —
  `fieldState.error?.message ?? helperText` — so a field's helper copy
  (e.g. "Required before you can publish your profile") is replaced by
  the validation error the moment one exists, rather than showing both
  stacked.
- **Success state**: no per-field success indicator (no green
  checkmark on valid fields) — the app's success feedback is at the
  *action* level (a snackbar on save, per §15.5), not per-keystroke
  field-level affirmation. Introducing field-level success ticks would
  be a real new convention, not something already established — flag
  it as a future decision rather than inventing it ad hoc in one form.
- **Disabled state**: MUI's default disabled treatment
  (reduced-opacity, no pointer events) — used for a control that is
  temporarily unavailable for a reason the user can discover (e.g. a
  submit button while its mutation is in flight, showing "Saving…"
  text at the same time per the onboarding wizard's convention).
- **Readonly state**: no dedicated readonly-styled input exists in the
  app today — data that can't be edited is simply not rendered as a
  form field at all (e.g. Review & Publish shows already-saved values
  as plain `Typography`, not disabled `TextField`s). Prefer that
  pattern (plain text display) over a disabled/readonly input when a
  value is permanently non-editable in a given context.

## 17. Layout Standards

### 17.1 Containers

No custom max-width container component — pages set their own root
`Stack`'s `maxWidth` inline where a narrower reading measure helps
(e.g. the onboarding wizard's `maxWidth={720}` for a single-column
form flow); list/dashboard pages that benefit from the full available
width don't set one. There is no single "always use N px" rule — pick
`maxWidth` based on whether the page's content is a narrow linear flow
(wizard, form) or a wide grid/list (dashboard, table, card grid).

### 17.2 Spacing hierarchy

Confirmed near-universal across the audited page set (one of the
cleanest conventions in the app — only 2 pre-existing exceptions found,
now fixed, see §22):

| Level | Spacing | Where |
|---|---|---|
| Page root | `spacing={3}` | The outermost `Stack` of almost every page |
| Multi-section row (dashboards) | `spacing={3}` | e.g. a `Stack direction={{xs:"column", md:"row"}} spacing={3}` of stat/summary cards |
| Form fields | `spacing={2}`–`{2.5}` | Inside a step/section's own field `Stack` |
| Button row | `spacing={1.5}` | A Back/Continue or Cancel/Confirm row |
| List items within a section | `spacing={1}`–`{2}` | e.g. `ProfileCompletionCard`'s checklist rows |

`MuiCardContent`'s default padding is themed to 32px on every side
(including `&:last-child`, overriding MUI's smaller default) — no page
needs to set Card padding manually.

### 17.3 Responsive grid / sidebar / mobile

- **Sidebar**: `AppLayout`'s nav drawer collapses at the `md` breakpoint
  (900px) — the one deliberate breakpoint decision in the whole app
  (§14.7).
- **Multi-column layout**: the standard responsive pattern is
  `direction={{ xs: "column", md: "row" }}` (occasionally `sm` instead
  of `md` for a form's two fields side-by-side, e.g.
  `PersonalInfoStep`'s Country/City row) — stacked on mobile, row on
  desktop, no custom CSS Grid usage found anywhere in the app (MUI
  `Stack`/`Box flex` covers every layout need so far).
- **Mobile-specific affordances**: the wizard pattern's sticky bottom
  action row (§15.10) and the Stepper→LinearProgress swap are the two
  mobile-only UI changes in the app; see §20 for exact breakpoint
  behavior.

## 18. Motion Guidelines

No animation library is a dependency — every animation in the app is
either a plain CSS `transition` (via `theme.ts`'s `styleOverrides`) or
one of MUI's built-in transition components (`Fade`, `Grow`,
`Collapse`), first introduced in Phase 7. **Do not add an animation
library** — nothing built so far has needed one, and MUI's built-ins
cover fade/grow/collapse/slide/zoom already.

| Animation | Duration | Easing | Where |
|---|---|---|---|
| Card hover lift | 200ms | `ease` | `MuiCard.root` — `transform: translateY(-2px)` + shadow change on hover |
| Button background/border/press | 150ms | `ease` | `MuiButton.root` — plus a `scale(0.98)` on `:active` |
| List item button hover/select | 150ms | `ease` | `MuiListItemButton.root` |
| Step content fade-in | 250ms | MUI `Fade` default | Phase 7's `StepPanel` — wraps each wizard step's content, re-triggers because the step unmounts/remounts via its own ternary |
| Field-array row grow-in | 200ms | MUI `Grow` default | `TeachingInfoStep`'s dynamically added subject rows |
| Section expand/collapse | MUI `Collapse` default | MUI `Collapse` default | `PricingStep`'s trial-lesson-price field |
| Progress bar fill | 400ms | `ease` | Wizard mobile `LinearProgress`, explicit `transition: transform 400ms ease` on the bar element |

**`prefers-reduced-motion` strategy** (exactly as implemented, not
idealized): a **global** kill-switch in `MuiCssBaseline` collapses
every animation/transition duration to `0.01ms` and disables smooth
scrolling app-wide — this alone neutralizes MUI's built-in `Fade`/
`Grow`/`Collapse` transitions and the progress-bar fill automatically,
with zero extra code per usage. Additionally, three component
overrides (`MuiCard`, `MuiButton`, `MuiListItemButton`) each carry
their **own** local `@media (prefers-reduced-motion: reduce)` block
that also zeroes out their `transform` (not just duration) — this is a
small, known duplication (four total reduced-motion rules instead of
one), left as-is since consolidating it would mean changing `theme.ts`
behavior, which is outside this documentation-only phase's scope. A
future phase could fold the three local overrides into one shared
constant without changing any resulting behavior.

**Do**: use MUI's `Fade`/`Grow`/`Collapse` for any new mount/unmount
transition — they inherit the reduced-motion behavior for free.
**Don't** write a raw CSS `@keyframes` animation or reach for a new
dependency for something these three components already cover.

## 19. Accessibility Guide

- **Keyboard navigation**: every interactive control in the app is a
  real, focusable element — no `<div onClick>` pattern was found
  anywhere in the audited pages. `StepButton` (§15.10) makes completed
  wizard steps keyboard-operable (Enter/Space, native button
  semantics) with `disabled` correctly set on not-yet-reachable steps.
  `DataTable`'s clickable rows (§15.9) get `tabIndex={0}` plus an
  explicit Enter/Space `onKeyDown` handler, since a `<tr>` has no
  native activation behavior the way a `<button>` does.
- **Focus order**: follows visual/DOM order everywhere — no `tabIndex`
  values greater than 0 exist in the app (confirmed by the audit; the
  only non-zero `tabIndex` usage is `-1`, used deliberately below).
- **Focus visibility**: one app-wide focus-ring convention — a 2px
  solid outline in `primary.main`, `outlineOffset: 2` (or `-2`/inset
  for `ListItemButton`, since its selected background would otherwise
  make an outset ring hard to see) — applied via `&:focus-visible` (not
  `:focus`, so mouse clicks don't show it, only keyboard/programmatic
  focus) on `Button`, `IconButton`, `OutlinedInput`, `Checkbox`,
  `Radio`, and `ListItemButton`.
- **Focus management on transitions**: every wizard step region
  (§15.10) is `tabIndex={-1}` (programmatically focusable, not in the
  tab order) with a callback `ref` that calls `.focus()` the moment the
  region mounts — this is how a screen-reader user is told "you're now
  on step 3" after clicking Continue, instead of focus silently
  vanishing along with the button/skeleton that was just removed from
  the DOM. The same pattern is reused for Phase 7's one-time
  `OnboardingWelcomeScreen`.
- **ARIA usage**: `SectionCard` wires `aria-labelledby` from a
  generated heading id to its own `<Card component="section">` so
  assistive tech can jump section-to-section; wizard step regions use
  `role="group" aria-label={stepName}`; progress messages use
  `aria-live="polite"` (Phase 7's encouraging-message line) so a
  screen reader announces the update without interrupting whatever the
  user is doing; icon-only decorative glyphs (empty-state badge icons,
  checkmark/warning icons that duplicate adjacent text) are
  `aria-hidden="true"`; every icon-only `IconButton` has an
  `aria-label` (§15.1).
- **WCAG AA color compliance**: enforced automatically by
  `frontend/src/app/theme.ts`'s own test file
  (`frontend/src/app/theme.test.ts`), which checks every semantic
  color's contrast against its `contrastText` in both modes — this is
  *why* success/warning use dark `contrastText` instead of white
  (§14.1). Any future palette change that fails this test will fail
  CI, not just look subtly wrong.
- **Contrast rules for new colors**: if a new semantic color or tinted
  background is ever added, check its contrast against both the text
  color placed on it *and* the surrounding background it tints,
  in both light and dark mode — don't rely on "it looks fine" in only
  one theme mode, since this app is used in both.

## 20. Responsive Guidelines

Keyed to MUI's real, unmodified breakpoints (§14.7: `xs:0 sm:600
md:900 lg:1200 xl:1536`), described at the five viewport widths
requested:

| Width | Breakpoint bucket | What actually changes |
|---|---|---|
| 320px | `xs` | Narrowest supported width. Wizard steps show the mobile `LinearProgress` progress block, not the `Stepper`. Two-field rows (e.g. Country/City) stack to one column. Sticky bottom action row is active (`mobileStickyActionsSx`, `position: sticky`) so the primary button never scrolls out of reach below long content. |
| 375px | `xs` | Same bucket as 320px — no additional layout change happens between 320–599px; this is the same "mobile" experience end-to-end, just with more breathing room. |
| 768px | `sm` (600–899px) | Two-field form rows that use `sm` as their stacking point (rather than `md`) go to one row here. Sticky mobile action row is **no longer** sticky — `mobileStickyActionsSx` switches to `position: static` at `sm`, since desktop-style layouts have enough vertical room. |
| 1024px | `md` (900–1199px) | The one deliberate app-wide breakpoint: `AppLayout`'s nav drawer becomes a persistent sidebar instead of a collapsible overlay; every wizard swaps its mobile `LinearProgress` block for the desktop `Stepper`. |
| 1440px | `lg`+ (1200px+) | No further layout restructuring — dashboards' multi-column stat rows and card grids are already in their `md`-and-up row layout; extra width just gives existing content more breathing room (pages don't currently cap width to a fixed max except where a narrow reading measure is deliberately chosen, e.g. the onboarding wizard's `maxWidth={720}`, §17.1). |

**Do**: use `{ xs: ..., md: ... }`-style breakpoint objects on `sx`/
`direction` props for any new responsive behavior, matching the app's
existing `md` sidebar/wizard breakpoint unless there's a specific
reason a different one fits better (as the occasional `sm` two-field
row does). **Don't** introduce a new custom breakpoint value — none
has been needed yet, and `theme.ts` deliberately keeps MUI's defaults
(§14.7).

## 21. Page Patterns

Each pattern below names its real exemplar(s) rather than prescribing
something new — a future page of the same shape should start from that
file, not from a blank page.

- **Dashboard** (`TutorDashboard`, `ParentDashboard`, `StudentDashboard`,
  `AdminDashboardPage`): `PageHeader` (role-specific welcome copy) →
  a responsive row (`direction={{xs:"column", md:"row"}} spacing={3}`)
  of stat/summary tiles → one or more `SectionCard`s for lists below
  (recent sessions, pending approvals, recommended tutors). Stat tiles
  render their number as `variant="h4" component="p" fontWeight={700}`
  — a `<p>`, deliberately not a heading, since a stat number isn't a
  document-outline-worthy heading (§22 fixed the one dashboard that
  didn't follow this).
- **Wizard** (`BookSessionPage`, `TutorOnboardingWizardPage`): see
  §15.10 — desktop `Stepper` / mobile `LinearProgress`, per-step focus
  region, sticky mobile actions.
- **Profile** (`TutorDetailPage` + `TutorProfileHero`): one real `<h1>`
  hero (photo/name/headline) at the top — the one place
  `SectionCard`'s `headingComponent="h2"` override is used, since every
  section below is genuinely subordinate to a real page-level heading
  rather than `PageHeader`'s usual `h4`. Loading state is
  `TutorProfileSkeleton`, mirroring the hero + section-card grid shape.
- **Discovery** (`TutorSearchPage` + `SearchHero`, `TutorFilterPanel`,
  `ActiveFiltersBar`, `TutorCard` grid): a search hero at top, an
  active-filters chip row (dismissible, §15.13) below it, a responsive
  card grid, `TutorCardSkeleton` while loading, `EmptyState` for a
  zero-result search.
- **Booking / multi-step data entry**: same as Wizard, above — Booking
  is a Wizard, not a separate pattern.
- **Calendar / Availability** (`WeeklyAvailabilityCalendar`,
  `DeclareAvailabilityPage`, onboarding's `AvailabilityStep`): a
  horizontally-scrollable day-column layout, each day showing its own
  slots as `Chip`s (§15.13) or a per-day "No teaching time" caption
  when empty — plus, since Phase 7, a top-level empty-state hint when
  the *entire week* has zero slots (a different empty state than the
  per-day one, since "no slots at all yet" needs stronger guidance than
  "nothing on this particular day").
- **Tables** (`RelationshipsPage` + `DataTable`): see §15.9 — one
  shared component handles loading/error/empty/pagination so a new
  tabular page only needs to define `columns`.
- **Empty pages**: see §15.7 (`EmptyState`) — always paired with copy
  specific to *why* the collection is empty, not a single generic
  message reused everywhere.
- **Error pages**: `ErrorState` (retryable, inline — a failed query
  inside an otherwise-working page), `UnavailableState` (a whole page
  can't load, e.g. the onboarding wizard's Tutor-fetch failure),
  `ForbiddenState` (an authorization failure), `IdentityLookupErrorState`
  (a not-found-by-id result specific to the `IdLookupForm` pattern).
  Pick the narrowest one that matches — don't use a whole-page
  `UnavailableState` for a single failed section query that the rest of
  the page can still render around.
- **Loading pages**: a hand-tailored `*Skeleton` (§15.6) for anything
  with a stable, recognizable shape; `LoadingState` (spinner + label)
  only for content with no shape to mirror.

## 22. Consistency Audit — Findings & Fixes

A cross-page audit (this phase) checked spacing, typography, color,
icon, button, form-layout, card, and dialog usage across every
`features/**/pages/*.tsx` and `routes/**/*.tsx` file. Results below,
split by what was safe to fix immediately versus what's recorded for a
future, visually-verified pass.

### 22.1 Fixed in this phase

| Finding | Fix | File |
|---|---|---|
| Two dashboards used `spacing={4}` at the page root instead of the near-universal `spacing={3}` | Changed to `spacing={3}` | `routes/DashboardPage.tsx` (`GenericDashboard`), `routes/dashboard/StudentDashboard.tsx` |
| A lone submit button (Reschedule) was `variant="outlined"` with no adjacent `contained` button, inverting the app's primary/secondary convention | Changed to `variant="contained"` | `features/scheduling/pages/SessionDetailPage.tsx` |
| `AdminDashboardPage`'s stat tiles rendered their number as a bare `variant="h3"` (a real `<h3>` heading, since no `component` override was set), while the other two dashboards' stat tiles deliberately use `variant="h4" component="p"` (not a heading) | Aligned both of this page's stat numbers to `variant="h4" component="p" fontWeight={700}` | `features/oversight/pages/AdminDashboardPage.tsx` (`MarketplaceStat` and the inline pending-tutor count) |

Each fix was verified by re-running the touched files' existing test
suites (0 regressions) — see the phase's final verification results.

### 22.2 Documented as future work (not fixed now)

- **Raw `Card`/`CardContent` instead of `SectionCard`** — the
  highest-impact finding, and deliberately **not** mechanically applied
  in this phase. Converting means every listed page's ad hoc
  `variant="subtitle1" fontWeight={600}` heading becomes `SectionCard`'s
  `variant="h5"` — a real, visible size increase across ~16
  already-shipped pages, and this repo has no real backend/Postgres
  instance that's ever been run (confirmed in Phase 7), so there's no
  way to visually verify the result this session. A future phase should
  execute this **with real visual QA** (a live dev environment, before/
  after screenshots), not as a blind find-and-replace. Files affected:
  `RelationshipsPage` (three cards: `InviteRelationshipCard`,
  `RelationshipsForAccountCard`, `AddChildCard`), `LoginPage`,
  `AdminResetPasswordPage`, `RegisterTutorPage`, `RegisterStudentPage`,
  `RegisterParentGuardianPage`, `StudentDetailPage`,
  `ParentGuardianDetailPage`, `TutorOfferingPage`,
  `AvailabilitySlotDetailPage`, `TutorSessionListPage`,
  `StudentSessionListPage`, `DashboardPage`'s health-check card,
  `RecommendedTutors`. The migration pattern itself: replace
  `<Card variant="outlined"><CardContent><Typography variant="subtitle1" fontWeight={600} gutterBottom>{title}</Typography>{content}</CardContent></Card>`
  with `<SectionCard title={title}>{content}</SectionCard>` — a
  mechanical swap once someone can see the result.
- **`ConversationDetailPage` bypasses `PageHeader`** with a compact
  `h5` + inline back-`IconButton` header instead. This plausibly reads
  as a deliberate "chat thread header" convention (distinct from a
  generic page title — most messaging UIs use a compact header, not a
  large page title), not obviously a bug. Recorded here for an owner
  decision rather than force-fit into `PageHeader`, which would make an
  in-conversation view look like every other full page rather than a
  focused chat thread.
- **`ProviderBadge`'s hardcoded brand colors** (Google Meet/Teams/Zoom
  hex codes, outside `theme.ts`) — correctly scoped as-is: these are
  fixed third-party brand colors, not app palette, so they should
  *not* be tokenized into `theme.ts` or run through the light/dark
  alpha conventions of §14.9. Documented as an intentional exception,
  not a gap.
- **Two dialogs, two shapes** — already resolved as documentation, not
  a code fix: §15.4 records both `AddTeachingTimeDialog`'s
  content-form shape and `ConfirmDialogProvider`'s confirm shape as
  equally canonical, each for its own purpose, rather than reconciling
  them into one shape.
- **`ConversationDetailPage`'s ad hoc bordered `Box`** (a message-thread
  scroll container, `borderRadius: 1` — smaller than the app's `lg`
  (24px) Card-radius convention) and **`InboxPage`'s raw `TextField`**
  (a client-side message-search filter backed by local `useState`, not
  an `react-hook-form` field) — both low-impact, defensible exceptions
  rather than leftovers, left as-is.
- **Icons, colors (outside `ProviderBadge`), and form-layout wrapper
  usage were audited and found already consistent** — no changes
  needed or recorded as debt in any of those three categories.

## 23. Phase 9 — Discovery & Tutor Profile Experience

A "Preply-class marketplace" redesign request turned out to be mostly
already built (an earlier pass had already shipped the sticky search
hero, filter drawer, active-filter chips, filter-aware empty/error
states, and a decision-narrative profile layout). The real work was
narrower and worth recording as its own lesson.

### 23.1 Wire enriched DTO fields before assuming a gap

`ADR-024` ("Tutor Profile Enrichment") added `photoUrl`, `displayName`,
`headline`, `biography`, `tutorSubjects`, `otherLanguages`,
`yearsOfExperience`, `education`, `certifications`,
`teachingMethodology`, `lessonSpecialties`, `trialLessonAvailable`/
`Price` to `TutorDto` — but almost none of it had been read anywhere
outside the onboarding wizard's own write-side steps. `TutorCard`,
`TutorProfileHero`, and `TutorDetailPage`'s "Certificates & Experience"/
"Teaching Style" sections were all still treating this data as if it
didn't exist, months after it started existing. **Before building UI
around "missing" data, check whether it's actually missing from the
API, or just not yet wired into this particular view.** The audit
that found this gap is the template for how to check: read the DTO,
read the query/endpoint, then read every consuming component — don't
assume a field's absence from one screen means it's absent everywhere.

### 23.2 A second domain-aware wrapper around `MonthCalendarGrid`

`AvailabilityPreviewCalendar` (`frontend/src/features/identity/components/`)
is the second real consumer of Phase 8a's domain-agnostic
`MonthCalendarGrid`, alongside `TutorAvailabilityCalendar` — confirming
that component's own design goal ("reusable... without modification")
actually holds up in practice. The pattern: `MonthCalendarGrid` stays
untouched; a thin, feature-specific wrapper supplies `renderDay` and
decides what a day cell means. `AvailabilityPreviewCalendar` is
deliberately the *simplest* possible instance of this pattern — a
read-only, non-interactive preview (a dot indicating "has openings" or
not, no click handler, no navigation) for a Student deciding whether to
book, distinct from the Tutor's own richer, interactive schedule view.
**Do**: reach for this same thin-wrapper approach for any future
calendar UI (an Admin calendar view, session history, etc.) — extend by
adding a new wrapper, never by adding options to `MonthCalendarGrid`
itself.

### 23.3 Marketplace data — hidden-by-design, not fabricated

Extends §22's Consistency Audit convention to marketplace-card and
profile data specifically: **ratings, review counts, review sorting,
and an online/presence indicator do not exist anywhere in this
backend** — not a field, not an endpoint, not a Domain concept. Every
place a "Preply-class" card or profile might show one of these facts,
it is omitted entirely rather than shown as zero, blank, or a fake
placeholder value. `TrustIndicators` and `ProfilePlaceholderSection`
are the two existing, reusable primitives that already enforce this:
`TrustIndicators` renders nothing for a fact it wasn't given real data
for, and `ProfilePlaceholderSection` is the one honest "this part of
the profile has no real data yet" layout, reused for Reviews/FAQ (and,
before Phase 9, for Certificates & Experience/Teaching Style, until
those two were wired to their now-real data). **Do**: reach for one of
these two existing components whenever a marketplace UI needs to admit
data doesn't exist — never invent a plausible-looking value instead.

### 23.4 Enriched sticky booking card composition

`TutorDetailPage`'s sticky right-column card (`BookingCallToActionSection`)
went from a bare "Book a lesson directly with this tutor" sentence to a
real price/trial/next-availability summary — entirely by **reusing**,
not duplicating, logic that already existed one component over:
`formatToman` (the same money formatting the Hero already uses),
`useNextAvailableLabel` (extracted to `frontend/src/features/scheduling/hooks/`
once a second real consumer appeared — this session's own "two real
usages, extract" precedent, first established by Phase 8a for
`AvailabilitySlotChip`/`AvailabilityLegend`), and `StatusPill` for the
trial-lesson fact (the same primitive `TrustIndicators`/`SessionStatusBadge`/
`PlanBadge` all already build on). **Do**: when a card needs to restate
a fact shown elsewhere on the same page, reuse that fact's existing
formatting/derivation logic — never recompute or reformat it a second,
slightly-different way.

### 23.5 A documented, flagged trade-off: no per-card "next available"

Discovery's `TutorCard` deliberately does **not** show a "next
available lesson" fact, even though the Tutor Profile page does, and
even though the same `useNextAvailableLabel` hook could technically be
reused there too. `TrustIndicators`' own doc comment already explained
why before this phase started: computing it per card in a search
results grid means one extra HTTP request per card (an N+1 pattern) —
a real, previously-made architectural decision, not an oversight this
phase corrected. Recorded here so a future phase doesn't "fix" this by
quietly reversing it — if per-card next-availability is ever wanted, it
needs either a batch-capable endpoint (a real backend change) or an
explicit, deliberate acceptance of the N+1 cost, not a silent one-line
change.

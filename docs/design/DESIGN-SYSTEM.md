# TutorFlow Design System — Foundation (Phase D1)

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

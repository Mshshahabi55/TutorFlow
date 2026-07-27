# Phase D3 Report — Dark Mode Foundation + Typography Hierarchy

Branch: `develop` | Files this milestone: new —
`frontend/src/app/contrast.ts`, `frontend/src/app/theme.test.ts`,
`frontend/src/shared/context/ColorModeContext.ts`,
`frontend/src/shared/context/ColorModeProvider.tsx`,
`frontend/src/shared/hooks/useColorMode.ts` (+ its test),
`frontend/src/shared/components/ThemeToggle.tsx` (+ its test); modified —
`frontend/src/app/theme.ts`, `frontend/src/dev/StyleGuidePage.tsx`,
`docs/design/DESIGN-SYSTEM.md`. No `AppProviders.tsx`, `AppLayout.tsx`, or
real (non-dev) page touched.

**Status: complete, frontend build/lint/test all clean.** This is
Milestone 1 of `prompts/01_PHASE_1.md` ("Foundation UI & Design System"),
continuing this repo's own D1 (tokens) → D2 (shell application)
design-system report thread as D3 — per the two decisions confirmed before
starting: keep MUI and deepen its custom theme rather than replace the
component library, and break the brief into D1/D2-sized milestones rather
than one large pass. This milestone covers the purely foundational slice
of "Theme"/"Header": dark-mode tokens, a typography naming hierarchy, and
the toggle component itself — deliberately not yet wired into the live
app (see §4). Sidebar/Header placement, Cards/Forms/Tables restyling, and
Dashboard visual work are later milestones.

## 1. Clean Architecture / scope validation

Frontend-only — confirmed via `git status`: no Domain, Application,
Infrastructure, or Web (backend) file touched, and no real page's own
content changed either. `theme.ts`'s existing `export const theme` binding
is unchanged in shape and value (`createAppTheme("light")` instead of a
direct `createTheme(...)` call, same resulting object), so every existing
import — `AppProviders.tsx`, every test file's `<ThemeProvider theme={theme}>`
— required zero changes. `AppProviders.tsx` itself has no diff this
milestone (verified via `git diff --stat`).

## 2. Dark palette — not an inverted copy, measured against AA

`createAppTheme(mode: PaletteMode)` replaces the single `createTheme(...)`
call; `theme` (light, byte-identical output to before) and the new
`darkTheme` are both built from it. The dark palette is deliberately
independent values, not the light scale flipped: a bright accent/semantic
tone legible as text on a dark surface needs *dark* `contrastText`, not
white — measured, not assumed. A throwaway contrast-checking script
(iterated against real candidate hex values before anything was written
into `theme.ts`) found e.g. `primary.main` (`#6FA8C9`) with white
`contrastText` at 2.59:1 (fails AA) versus `#0B0E12` (dark) at 7.48:1
(clears it) — confirming this is a real design constraint, not a stylistic
choice. Full palette table and every measured ratio are in
`docs/design/DESIGN-SYSTEM.md` §7; every pair clears WCAG AA (4.5:1),
matching the bar §1's light-mode table already holds.

Surfaces step up in lightness — `background.default` (`#0F1319`) →
`paper` (`#1C232D`) → an `elevated` tier (`#242C38`, TableHead background
and Tooltip fill) — since this app's whole depth language is already
border/spacing-first (Phase D1), not shadow-first; dark-mode "elevation"
follows the same convention with a lighter surface rather than
introducing heavier shadow as the primary depth cue for the first time.
Shadows do get a heavier alpha in dark mode (`buildShadows(mode)`) since
the light-mode shadow colour is nearly invisible against a dark surface —
still used only where Phase D1's own precedent already required it (Menu,
Popover, Dialog, Snackbar).

## 3. Automated contrast regression (replaces D1's manual table)

`frontend/src/app/contrast.ts`: a pure `contrastRatio(hexA, hexB)` (WCAG
relative-luminance formula). `frontend/src/app/theme.test.ts` builds both
`createAppTheme("light")` and `createAppTheme("dark")` and asserts, for
each: every semantic tone's (`primary`/`secondary`/`success`/`warning`/
`error`/`info`) `contrastText` clears 4.5:1 against its own `main`, and
`text.primary`/`text.secondary` clear 4.5:1 against `background.default`/
`background.paper`. This turns Phase D1's one-time, unrepeatable manual
measurement into a real test — a future palette edit that drops a tone
below AA now fails a test instead of silently shipping.

## 4. Why the toggle isn't in the real app yet

`ColorModeContext`/`ColorModeProvider`/`useColorMode` mirror
`ActorContext`/`ActorProvider`/`useCurrentActor`'s exact existing pattern
— `localStorage["tutorflow.colorMode"]`, read-on-mount, write-on-change —
with one addition: `mode` can be `"system"` (defers to
`window.matchMedia("(prefers-color-scheme: dark)")`, live-updated via a
`change` listener) in addition to an explicit `"light"`/`"dark"` choice.
`ThemeToggle` is a plain icon button cycling light/dark, unit-tested
standalone.

None of this is composed into `AppProviders.tsx` this milestone. Wiring
the real app's `ThemeProvider` to `ColorModeProvider` now — before any
`ThemeToggle` exists anywhere in the live header — would silently dark-mode
the entire app for every visitor whose OS/browser already prefers dark,
with no control anywhere to switch back. That is a real, avoidable
regression for a change meant to be purely additive. Instead, the dark
theme, provider, and toggle are all built and proven together inside the
dev-only, already-`import.meta.env.DEV`-gated `StyleGuidePage` — the same
"review before real adoption" precedent Phase D1 itself used for
`UnitText` (built in D1, first adopted by a page in D2+). The next
design-system milestone wires `ColorModeProvider` into `AppProviders.tsx`
and places `ThemeToggle` in the real header at the same time, so automatic
dark-mode detection and a visible override arrive together, never one
without the other.

## 5. Typography role naming — documentation only

The brief asks for a named hierarchy (Display, Page Title, Section Title,
Card Title, Subtitle, Body, Caption, Small Label).
`docs/design/DESIGN-SYSTEM.md` §2 now maps each name onto the existing MUI
variant (`h1`→Display, `h4`→Page Title, `h5`→Section Title,
`subtitle1`→Card Title, `subtitle2`→Subtitle, `body1`→Body,
`caption`→Caption, `overline`→Small Label) and `StyleGuidePage` restates
the same table live. No call site changed — Phase D1 already recorded why
one scale beats a second, parallel one (every call site already writes
`variant="h4"`, never a raw pixel size), and this milestone extends that
reasoning rather than reopening it.

## 6. `StyleGuidePage` additions (dev-only, additive)

Two new sections, both purely additive — every existing section is
untouched: "Typography roles" (the naming table above, right after the
existing Typography section) and "Dark mode (Phase D3)" (a boxed preview
with its own nested `ColorModeProvider`/`ThemeProvider`, so toggling it
never affects the rest of the — light — style guide page). The preview
renders live: colour swatches for both palettes' background/paper/primary/
semantic tones, `Button` variants, `StatusPill` tones, and an outlined
`Card`, so both palettes can be compared against the same real components
side by side, not just described.

## 7. Verification and merge recommendation

```
$ npx tsc -b                 (clean)
$ npm run lint               (clean, 0 warnings)
$ npx vitest run             56 test files, 284 tests passed
                              (255 before this milestone + 29 new:
                               theme.test.ts, useColorMode.test.tsx,
                               ThemeToggle.test.tsx)
$ npm run build              ✓ built in 15.47s
```

Production-exclusion, same method Phase D1 used to prove `StyleGuidePage`
itself never ships:

```
$ grep -rl "Switch to dark mode\|Switch to light mode\|tutorflow.colorMode\|Preview — dark mode\|Preview — light mode" dist/
(no matches, exit code 1)
$ grep -rl "StyleGuidePage\|style-guide" dist/
(no matches, exit code 1)
```

Neither the dark-mode preview's own content nor the style guide page
itself reaches `dist/` — confirmed, not assumed.

**Ready to merge as a milestone.** No backend, API, route, repository,
CQRS, domain, DTO, database, or business-logic file touched
(`prompts/00_GLOBAL_RULES.md`'s forbidden list) — confirmed by `git
status`. No unrelated file rewritten: `AppProviders.tsx` and every real
page have zero diff this milestone. Next milestone (per the same brief,
its own slice): wire `ColorModeProvider` into `AppProviders.tsx` and build
the real Header (search/notifications placeholders, avatar/user menu,
breadcrumb, the now-ready `ThemeToggle`) and Sidebar (icons/spacing/
active-hover states, collapse animation) — the next D-numbered phase.

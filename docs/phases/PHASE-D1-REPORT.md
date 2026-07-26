# Phase D1 Report — Design System Foundation

Branch: `develop` | Commits this phase: `7ef7009` (Task 2 — tokens),
`de9f871` (Task 3 — shared-primitive restyle), plus this phase's own
commit for Task 4 (dev-only style guide + gated route) and Task 5/docs
(this report and `docs/design/DESIGN-SYSTEM.md`, verification only, no
further code change).

**Status: complete, 5/5 tasks done.** This phase is frontend-only — no
Domain, Application, Infrastructure, or Web (backend) file was touched,
so none of `CLAUDE.md`'s backend-specific Definition-of-Done items
(layer dependency rule, `AUTHORIZATION_MATRIX.md`, audit trail,
`dotnet build`/`dotnet test`) apply. It builds the design system
**foundation only** — tokens and shared primitives — and restyles
**zero real pages** (that is Phase D2 onward, deliberately out of scope
here per the original brief).

## 1. Task 1 — Audit

Read every shared primitive under `frontend/src/shared/components/` and
`frontend/src/layouts/`, plus a representative sample of pages, before
writing a single token. Findings are not kept as a separate audit
document (a frozen snapshot would drift the moment a page changes);
they're folded into `theme.ts`'s own rationale comments and restated in
`docs/design/DESIGN-SYSTEM.md` §4, which lists all ten findings and how
each was closed. Full detail there; not repeated here.

## 2. Task 2 — Design tokens (`7ef7009`)

`frontend/src/app/theme.ts` rewritten as the single source of colour,
type, spacing, shape, and shadow tokens. No component styling changed
in this commit — purely the token layer. Full palette, type scale, and
the measured WCAG contrast ratios backing the "every semantic tone
clears AA" claim are in `docs/design/DESIGN-SYSTEM.md` §1–3; this
report does not duplicate that table.

## 3. Task 3 — Shared primitives (`de9f871`)

Wired Task 2's tokens into every shared primitive pages already depend
on, with **no public API change** to any of them: focus-visible rings
on Button/IconButton/Checkbox/Radio, an outlined-card depth language for
Card/Paper/Dialog, DataTable's TableHead given a deliberate header
treatment, StatusPill's neutral/outlined tone a deliberate border/text
colour, and a consistent arrow + tone on every Tooltip via
`defaultProps`. Two real, scoped gaps the audit found were fixed as part
of this restyle rather than deferred: `NavSidebar` had no visual
indication of the current route at all (now `.Mui-selected` gets an
accent-tinted background and left bar, driven by `useLocation()`), and
`RoleSwitcher` (a dev-only role-preview control) sat visually
indistinguishable from `AuthStatus` (a real signed-in identity) in the
same AppBar — `RoleSwitcher` now sits in a dashed, warning-tinted frame.
Also added `UnitText`, the one typographic treatment for a Tehran time
or a Toman amount (six ad hoc renderings found, no shared component
behind any of them) — not adopted by any page yet, ready for D2+.

## 4. Task 4 — Style guide (this phase's commit)

`frontend/src/dev/StyleGuidePage.tsx`: a dev-only living reference
rendering every Task 2 token (colour swatches, type scale, spacing/
radius/shadow samples) and every Task 3 shared-component variant
(buttons in every state, status pills, form controls including the
Tehran `datetime-local` pattern, `CopyableId`, `DataTable`, feedback
states, confirm dialog, notification) on one page — so the whole system
can be reviewed before any real page adopts it.

Wired into `frontend/src/routes/paths.ts` (`paths.dev.styleGuide =
"/dev/style-guide"`) and `frontend/src/routes/router.tsx`, gated on
`import.meta.env.DEV` at both the lazy-import site and the route-array
site (see Task 5 below for why this gating is sufficient). Not linked
from `NavSidebar` — reachable only by typing the URL directly, and only
in a dev build.

## 5. Task 5 — Verify the style guide is excluded from production

Ran the full frontend gate against the working tree containing Task 4's
changes:

```
$ npm run lint
> eslint .
(clean, no output)

$ npm test -- --run
 Test Files  50 passed (50)
      Tests  231 passed (231)

$ npm run build
> tsc -b && vite build
✓ 1163 modules transformed.
✓ built in 12.29s
```

Then, specifically to verify the exclusion claim in
`router.tsx`'s own comment (Vite's `define` transform replaces
`import.meta.env.DEV` with the literal `false` before Rollup bundles, so
the whole style-guide ternary — lazy import included — is dead code and
tree-shaken out): every file in the resulting `dist/` was searched for
any trace of the style guide.

```
$ grep -rl "StyleGuidePage\|style-guide\|Every Phase D1 token" dist/
(no matches, exit code 1)
```

No `StyleGuidePage` chunk appears in the build's own asset listing
either — the full chunk list is page/component/service names only, none
of them the style guide. `dist/` and `dist-ssr/` are both `.gitignore`d
(confirmed), so no build artifact was ever at risk of being committed
regardless. **Conclusion: the exclusion mechanism works in practice —
zero bytes of the style guide reach a production build, and the route
is never registered there since `devRoutes` collapses to `[]`.**

Frontend-only phase, so this is the full Definition-of-Done gate that
applies: lint clean, tests green, production build succeeds and
provably excludes the dev-only page. No backend suite was run — no
backend file changed this phase.

## 6. Merge recommendation

**Ready to merge.** Tokens (`7ef7009`) and shared-primitive restyle
(`de9f871`) were already on `develop`; this phase's own commit adds the
dev-only style guide and its gated route, and this report plus
`docs/design/DESIGN-SYSTEM.md` close out the documentation the code
already referenced. No page was restyled — `NavSidebar`'s selected state
and `RoleSwitcher`'s frame are the only two visual changes reaching a
real (non-dev-only) surface, and both close audit-found gaps rather than
reskin existing content. Phase D2 (the first real page restyle) is the
natural next unit of work.

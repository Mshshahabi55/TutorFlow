# Phase D2 Report — First Real-Page Restyle

Branch: `develop` | Commits this phase: `56e57e2` (Task 2 — bounded
content column), `7c09217` (Task 3 — nav clarity), `b0b8736` (Task 4 —
Dashboard quick actions), `0bc436a` (Task 5 — 375px viewport pass). This
report is its own commit, no further code change.

**Status: complete, 5/5 tasks done.** Frontend-only phase — no Domain,
Application, Infrastructure, or Web (backend) file was touched, so none
of `CLAUDE.md`'s backend-specific Definition-of-Done items (layer
dependency rule, `AUTHORIZATION_MATRIX.md`, audit trail, `dotnet
build`/`dotnet test`) apply. This is D1's foundation (tokens + shared
primitives) put to work on the shell and the first real page — the
Dashboard — rather than a second foundation pass.

## 1. Task 1 — Audit

Read `AppLayout`, `NavSidebar`, `RoleSwitcher`, and `DashboardPage` —
the shell every signed-in user sees regardless of role, and the one
real page in scope this phase — against the D1 token set. Findings are
not kept as a separate audit document (same reasoning as Phase D1: a
frozen snapshot drifts the moment a page changes); each finding is
folded into the rationale comment at its own fix site and restated in
that task's commit message. Four findings, one task each:

1. `AppLayout`'s main content area had no max-width — pages stretched
   edge-to-edge on wide screens, against D1's "calm, spacious" brief. →
   Task 2.
2. `NavSidebar`'s `.Mui-selected` accent (D1) was sighted-only — no
   `aria-current`, so a screen-reader user had no equivalent "you are
   here" signal. Two icon collisions within always-simultaneously-visible
   groups (Students reusing Parent/Guardian's icon; two different
   session-list entries reusing Session-lookup's icon). → Task 3.
3. `DashboardPage`'s "Available modules" list was four static,
   non-interactive pills naming every bounded context — no link, no
   distinction, no role-appropriate next action; only prose telling the
   user to "use the navigation." → Task 4.
4. A real 375px-viewport check (the narrowest width this app's
   responsive breakpoints target) found two crowding issues in the
   AppBar: the Toolbar's default gap/padding left too little room for
   hamburger + title + `AuthStatus` + `RoleSwitcher` together, and
   `RoleSwitcher`'s dashed frame gave its `TextField`'s floating label
   only 4px of vertical clearance, so the label poked outside the
   frame's own border. → Task 5.

## 2. Task 2 — Bounded content column (`56e57e2`)

`AppLayout`'s routed content now centers within a 1200px
`CONTENT_MAX_WIDTH`, generous consistent padding from the D1 spacing
scale (`px` 2/3/4, `py` 3/4) replacing the old flat `p={{xs:2,sm:3,md:4}}`.
1200px is deliberately below the `xl` breakpoint (1536px) and not tied to
any single breakpoint value, since it's a reading-width choice, not a
layout-collapse one. No routing, business logic, or feature-page change.

Same commit also wrapped `AuthStatus` + `RoleSwitcher` in a
`flexShrink: 0` group in the AppBar, so the title (already
`flexGrow` + `minWidth: 0`) is what shrinks first on a narrow viewport,
not the two interactive controls — a robustness fix the audit flagged
as worth confirming alongside the max-width change, since both touch
`AppLayout`'s Toolbar.

## 3. Task 3 — Nav clarity: `aria-current` + icon disambiguation (`7c09217`)

Builds on D1's `.Mui-selected` accent indicator rather than replacing
it. The identical boolean that drives the visual selected state
(`pathname === entry.to || pathname.startsWith(...)`) now also sets
`aria-current="page"` on the active `NavLink` — for the Dashboard link
and every section entry — so a screen-reader user gets exactly the
same "where am I" answer a sighted user gets from the accent bar, never
a state where the two signals could disagree. Two icon swaps: "Students"
(`FamilyRestroomRoundedIcon` → `SchoolRoundedIcon`, freeing that icon
for Parent/Guardian's exclusive use) and "Student sessions"/"Tutor
sessions" (`EventNoteRoundedIcon` → `CalendarMonthRoundedIcon`, freeing
that icon for the unrelated single-record "Session lookup" entry's
exclusive use). Section grouping (Identity & Relationship / Discovery /
Scheduling & Booking / Marketplace Oversight) was kept as-is — it
already mirrors this app's actual bounded-context structure, a coherent
grouping, not a gap to invent new nomenclature for.

## 4. Task 4 — Dashboard quick actions (`b0b8736`)

Replaced the four static "Available modules" pills with a role-keyed
"Quick actions" set of real links: Search Tutors / Book a session / My
sessions (Student); Declare availability / My sessions (Tutor);
Relationships / Book a session / Student sessions (Parent/Guardian);
Pending Tutor approvals / Admin dashboard / All sessions (Admin/Staff).
Every destination already exists in `paths.ts` and is already reachable
from `NavSidebar` — this surfaces the same capability one click sooner
from the page a signed-in user lands on first, no new page or capability
introduced. No role selected: no quick actions render, same behavior as
before (nothing to recommend yet). The real `GET /health` query,
loading/error states, and role-summary text are all unchanged.

Tests: the old "shows every available module" assertion is replaced,
not silently dropped — Phase 1B's precedent of explaining a test
replacement rather than loosening an assertion. A new test confirms no
quick actions render without a role; two more prove the quick actions
are real links to already-existing routes and differ correctly by role.

## 5. Task 5 — 375px viewport pass (`0bc436a`)

Toolbar's `gap`/`px` now tighten at `xs` only (`gap: {xs: 0.5, sm: 2}`,
`px: {xs: 1.5, sm: 2}`, both previously flat) so the hamburger icon,
title, `AuthStatus`, and `RoleSwitcher` all fit at the narrowest
supported width without the title's `noWrap` truncating more
aggressively than necessary. `RoleSwitcher`'s decorative dashed frame
gained `pt: 1.25` / `pb: 0.75` (previously a flat `py: 0.5`) — 10px top
clearance is what the outlined `TextField`'s floating label (rendered by
MUI straddling the fieldset's own top edge) needs to stay fully inside
the frame's border at every width; 6px bottom keeps the control visually
compact and roughly balanced rather than symmetric-but-clipped. The
select's own `minWidth` at `xs` is trimmed from 140 to 108 to fit the
same 375px budget alongside the icon button and the AppBar's other
controls.

No browser-automation tool is available in this environment (same
constraint noted in Phase 4.7 §6); this was verified by reasoning
through MUI's own layout mechanics (`Toolbar`'s default padding/gap
values, `TextField`'s floating-label overlay behavior) against the
375px budget, the same method Phase D1 and D2 Task 1–4's audit findings
were reasoned through, not a rendered screenshot.

## 6. Verification

Frontend-only phase, so this is the full Definition-of-Done gate that
applies — run against the working tree after Task 5's commit:

```
$ npm run lint
> eslint .
(clean, no output)

$ npm run build
> tsc -b && vite build
✓ built in 15.42s

$ npm test -- --run
 Test Files  50 passed (50)
      Tests  236 passed (236)
```

No backend file changed this phase (confirmed via `git diff --stat`
across all four commits — six frontend files touched, zero backend), so
no backend suite was run and none of `CLAUDE.md`'s backend Definition-of-
Done items apply.

## 7. Merge recommendation

**Ready to merge.** All four tasks close a real, audit-found gap rather
than reskin for its own sake: a reading-width column, an accessible +
disambiguated nav, a landing page that recommends real next actions
instead of naming bounded contexts, and a shell that holds together at
the narrowest supported viewport. No routing, authorization, or backend
behavior changed anywhere in this phase. Phase D2's scope was
deliberately narrow (the shell + one real page); restyling the
remaining feature pages against the D1 token set is the natural next
unit of work.

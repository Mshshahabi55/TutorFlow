# Phase D4 Report — Application Shell (Header + Sidebar)

Branch: `develop` | Files this phase: new —
`frontend/src/layouts/AppHeader.tsx`, `Breadcrumbs.tsx`,
`NavigationItem.tsx`, `NotificationsButton.tsx`,
`SearchFieldPlaceholder.tsx`, `UserMenu.tsx`, `navSections.tsx` (+ each
one's test); `frontend/src/shared/components/PageContainer.tsx` (+ test);
modified — `AppLayout.tsx`, `NavSidebar.tsx`, `AppProviders.tsx`,
`App.test.tsx`; deleted — `AuthStatus.tsx`/`AuthStatus.test.tsx` (retired
into `UserMenu`). No feature page, dashboard, form, or table touched
(confirmed: `git diff --stat -- 'src/features/**/pages/*Page.tsx'` is
empty).

**Status: complete, frontend build/lint/test all clean.** This is
`prompts/02_PHASE_2.md` ("PHASE 2 — APPLICATION SHELL"), continuing the
D1→D2→D3 design-system report thread as D4 — D3's own report already
named this exact work ("wire ColorModeProvider into AppProviders.tsx and
place ThemeToggle in the real header") as its natural next milestone.

## 0. Prerequisite: Phase 4.9 committed first

Per the confirmed commit-sequencing decision, Phase 4.9's already-tested,
previously-uncommitted session/RBAC work landed first as its own commit
(`db9a8f4`, using its own already-written `PHASE-049-REPORT.md` as the
basis), giving this phase's shell work a clean starting diff instead of
one commit mixing "add real RBAC" with "redesign the header/sidebar."

## 1. Dark mode wired into the real app

`AppProviders.tsx`: `ColorModeProvider` (Phase D3) now sits above
`ThemeProvider`; a small internal `ThemedApp` component reads
`useColorMode().resolvedMode` and picks `theme` or `darkTheme` (Phase D3)
accordingly. `ErrorBoundary` stays outermost, unchanged position.
`ThemeToggle` (Phase D3, previously proven only in the dev-only
`StyleGuidePage`) is now placed in the real `AppHeader` — the exact
sequencing D3's own report called for (auto dark-mode detection and a
visible override arriving together, never one without the other).
Proven end to end in `App.test.tsx`: clicking the real header's toggle
flips its own label and persists `localStorage["tutorflow.colorMode"]`.

## 2. Shell extraction — `AppHeader`, `PageContainer`, `NavigationItem`

`AppLayout.tsx`'s previously-inline `AppBar`/`Toolbar` and content-bounding
`Box` are now `AppHeader` and `PageContainer` respectively — same values,
same breakpoints, pure extraction plus the new header content (§3).
`NavigationItem` replaces the Dashboard link's and every section entry's
own near-duplicate `ListItemButton` markup with one component (props:
`to`, `label`, `icon`, `exact`, `collapsed`) — the same `isSelected`
boolean still drives both `.Mui-selected` (Phase D1) and
`aria-current="page"` (Phase D2 Task 3), never a state where they could
disagree. `NavSidebar`'s data (`SECTIONS`, `visibleEntries`) moved into
its own `navSections.tsx` (a `.tsx` file, since the icons are JSX) so
`Breadcrumbs` can reuse it without a second copy of route→label data, and
so the file exporting them isn't also a component file (resolves a real
`react-refresh/only-export-components` lint warning, not just silences
it).

## 3. `AppHeader` — Breadcrumb, search/notifications placeholders, theme toggle, user menu

Single row, unchanged AppBar height (no second line — both existing
`<Toolbar />` spacer usages keep working unchanged). Left to right:
hamburger (mobile only, unchanged), `Breadcrumbs`, a flexible spacer,
`SearchFieldPlaceholder` (hidden below `sm` — the same crowding lesson
Phase D2 Task 5 already learned for this exact Toolbar),
`NotificationsButton`, `ThemeToggle`, the dev-only `RoleSwitcher` (moved
here unchanged from `AppLayout`), and `UserMenu`-or-"Sign in".

- **`Breadcrumbs`**: matches the route against `navSections.tsx`'s
  `SECTIONS` — `Dashboard` (plain text, current page) on home;
  `Dashboard (link) → Section → Page` for a known route; falls back to
  just a clickable `Dashboard` link for an unmatched route (e.g.
  `NotFoundPage`) rather than inventing a label from the URL's own
  segments. Not a second copy of the page's own title — `PageHeader` (in
  every page's content) remains the one place a page's full title
  renders.
- **`SearchFieldPlaceholder`**: a real, typable `TextField` (local state
  only) — feels alive, not disabled — wired to nothing: no `onSubmit`, no
  query, no navigation. Proven in its own test that typing and pressing
  Enter never calls `apiClient.get`/`.post`.
- **`NotificationsButton`**: no badge/count (nothing real to count);
  opens a `Popover` with an honest "No notifications yet." rather than a
  button that visibly does nothing.
- **`UserMenu`** (retires `AuthStatus`): a role-initial, role-colored
  `Avatar` ("St"/"Tu"/"PG"/"Ad" — derived from the already-known role, not
  a fabricated name or photo, consistent with Phase 4.9's own "don't
  invent identity data" finding) opens a `Menu` showing the role label +
  account id (read-only) and a "Sign out" `MenuItem` (the same
  `useLogout` call `AuthStatus` already used). `AuthStatus.tsx` was used
  only by `AppLayout.tsx` and its own test (confirmed by grep before
  deleting); `AppHeader` now renders the plain "Sign in" button inline
  for the signed-out case instead (too small to warrant its own file).

## 4. Sidebar icon-rail collapse

`NavSidebar` gained a header row (the "TutorFlow" brand, moved out of the
AppBar — the reference apps this design system targets keep brand with
the sidebar, not the top bar — plus a chevron toggle) and a collapsed
(icon-rail) mode: `NAV_SIDEBAR_WIDTH_COLLAPSED = 72`, `collapsed` state
lifted into `AppLayout` (`useState`, persisted to
`localStorage["tutorflow.sidebarCollapsed"]`) exactly like the existing
`mobileNavOpen` state already is, passed down as props — no new Context,
since `AppLayout` itself needs the active width to correctly offset
`main`'s margin. Only meaningful for the permanent (desktop) variant; the
mobile temporary drawer is never collapsed, since collapsing an overlay
isn't meaningful. Width and content-margin transitions both animate over
200ms (within the brief's 150–250ms motion band); `ListSubheader` section
titles hide when collapsed (a thin `Divider` between groups instead,
since a narrow rail has no room for them); each `NavigationItem` falls
back to a `Tooltip` for its label.

## 5. Verification

```
$ npx tsc -b                 (clean)
$ npm run lint               (clean, 0 warnings)
$ npx vitest run             62 test files, 310 tests passed
                              (284 before this phase + 26 new: PageContainer,
                               NavigationItem, Breadcrumbs, SearchFieldPlaceholder,
                               NotificationsButton, UserMenu, AppHeader,
                               NavSidebar collapse cases, App's theme-toggle
                               wiring test)
$ npm run build              ✓ built in 11.42s
```

`git diff --stat -- 'src/features/**/pages/*Page.tsx'` is empty — no
feature page has any diff this phase, matching the brief's own
"Forbidden: do not redesign pages" list. `RoleSwitcher`'s own strings are
still absent from `dist/` (same grep-based proof as Phase 4.9/D3) — moving
it into `AppHeader` didn't reopen its production exclusion.

## 6. Merge recommendation

**Ready to merge.** No backend, route (business), repository, CQRS, DTO,
or auth/authz logic changed — only the shell components above and their
wiring, exactly the brief's own scope. No test was weakened or deleted;
`AuthStatus.test.tsx`'s assertions moved into `UserMenu.test.tsx` (the
component it superseded), and `NavSidebar.test.tsx`/`AppLayout.test.tsx`
kept every pre-existing assertion (role filtering, route guard
interaction, RoleSwitcher visibility) while gaining new collapse-specific
coverage. Next milestones (this brief's own "Forbidden" list, deferred on
purpose): Cards/Forms/Tables shared-primitive restyling, then
Dashboard/feature-page visual work, each its own D-numbered phase.

Suggested commit message (per the brief): `feat(ui): modernize application shell and navigation`.

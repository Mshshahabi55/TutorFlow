# Phase 4.9 Report — Real Authenticated Identity Drives the Whole UI

Branch: `develop` | Files this phase (all frontend, no backend file touched):
new — `frontend/src/shared/hooks/useEffectiveRole.ts`,
`frontend/src/shared/constants/actorRoleLabels.ts`,
`frontend/src/shared/components/RequireRole.tsx`,
`frontend/src/shared/components/feedback/ForbiddenState.tsx`,
`frontend/src/test/AuthHarness.tsx`, plus four new test files; modified —
`AuthProvider.tsx`, `AuthStatus.tsx`, `RoleSwitcher.tsx`, `NavSidebar.tsx`,
`AppLayout.tsx`, `router.tsx`, `TutorDetailPage.tsx`,
`test/setupTests.ts`, and the test files for each of those. No commit made
yet — working tree only, pending review.

**Status: complete, 9/9 `scripts/verify.ps1` green.** All three live-browser
findings traced to one root cause, confirmed by reading the code and then
proven by test: the frontend already *received* a real, working
authenticated session from `POST /auth/login`, but nothing kept it beyond
the current page's in-memory `useState`, and `NavSidebar`/`TutorDetailPage`
were built against the pre-auth, dev-only "Acting as" preview role rather
than that session.

## 1. Task 1 — How identity flows today, and where it breaks

**Token storage and attachment.** `apiClient.ts` holds the bearer token in
a plain module-level variable (`currentAuthToken`), not React state, so a
vanilla request interceptor can read it without depending on React;
`AuthProvider` calls `setAuthToken`/`clearAuthToken` whenever the session
changes. This part already worked correctly and needed no change.

**Does the frontend remember who is logged in?** `AuthProvider` held `user`
in `useState<AuthenticatedUser | null>(null)` — in memory only, by its own
explicit, ADR-017-cited design comment: *"deliberately never localStorage
or sessionStorage... a hard page reload signs the user out and requires
logging in again... an accepted, disclosed trade-off."* I wrote a
throwaway RTL test rendering `LoginPage` and `AuthStatus` together under
one `AuthProvider`, submitted real credentials against a mocked
`authService.login`, and confirmed: **within the same page load, the state
update is immediate and correct** — `AuthStatus` switches to "Student" +
"Sign out" the instant `login.mutate`'s `onSuccess` calls `setUser`, no
re-render bug anywhere. What does *not* survive is a hard page reload,
by design — the exact shape of finding #1 ("the Sign in button becomes
active again"), and finding #2 falls out of the same cause: a developer
manually refreshing to double-check a change is the single most natural
way to trigger this, and every time it does, identity and role visibly
evaporate. Confirmed via the same investigation: there is no separate
in-app-navigation bug — `AuthProvider` sits above `RouterProvider`
(`AppProviders.tsx`), so it never remounts between routes.

**What `AuthStatus`/"Sign in" keys off.** `useAuth().isAuthenticated` (a
plain `user !== null` check) and `useAuth().user`. Already correct and
reactive — confirmed by the same test above; the "Sign in" button
reappearing traces to the missing persistence, not to `AuthStatus` itself.

**What `useCurrentActor` returns.** The dev-only "Acting as" preview role
(`ActorContext`/`ActorProvider`, backed by `localStorage["tutorflow.devActorRole"]`)
— never the real session. The real session lives in a separate context,
`useAuth()`/`AuthContext`, fed only by login/logout.

**How `NavSidebar` decides what to show.** `effectiveRole = authenticatedRole
?? actor.role`, computed inline in `NavList`. This precedence was *already
correct* — a real authenticated role always wins over the dev preview,
never the reverse (Task 2's "must not override" requirement was true
before this phase touched anything). The actual bug behind finding #3 is
narrower and more literal: several `NavEntry` objects — Students,
Parent/Guardians, Relationships — carried **no `roles` array at all**,
and `visibleEntries`'s filter (`!entry.roles || role === null ||
entry.roles.includes(role)`) shows an entry with no `roles` restriction to
*every* role unconditionally. Separately, `TutorDetailPage` rendered "Edit
offering" (Tutor-only) and Approve/Suspend (Admin-only) **unconditionally
for every viewer, role or no role** — no filtering logic at all, the
literal "a Student sees tutor Approve/Suspend, admin actions" finding.
`RoleSwitcher` itself shipped to production — no `import.meta.env.DEV`
gate anywhere, contradicting its own doc comment ("Development aid only")
and this phase's own assumption that "per earlier phases [it] must not
appear in a production build" — that claim was not yet true; it is now
(§2).

**Is there a "who am I" endpoint?** No, and none is needed:
`LoginResultDto`/`AuthenticatedUser` already carries `token, accountId,
role, expiresAtUtc` — confirmed live (§6) — and `AuthEndpoints.cs` defines
only `/auth/login`, `/auth/logout`, `/auth/accounts/{id}/reset-password`.
There is, however, **no name or email in that response at all** — ADR-017
leaves "the exact personal-data fields... beyond email" as its own open
question, so no display name can be shown without inventing a field the
backend doesn't return (§3 explains what is shown instead).

**A documentation-drift finding, reported per CLAUDE.md's stop
conditions, not silently resolved either way:** `docs/api/AUTHORIZATION_MATRIX.md`
states *"Zero endpoints are currently protected"* and marks every row's
WP3 Status "Unprotected." This is stale — `grep`-confirmed,
`RequirePermission(...)` is already wired into `IdentityEndpoints.cs`,
`SchedulingEndpoints.cs`, `OversightEndpoints.cs`, `AuditEndpoints.cs`, and
`AuthEndpoints.cs`, and `AuthorizationMiddleware` is registered in
`Program.cs`. The matrix's endpoint-to-permission mapping itself is not
wrong (verified against the code, §4/§6) — only its "has WP4 shipped yet"
status column has drifted from reality. Flagged here for the owner to
correct; not fixed by this phase, since it is documentation, not the
authorization decision itself.

## 2. Task 2 — The real session is now the source of truth

**Persistence.** `AuthProvider` now persists the session to
`sessionStorage` (`frontend/src/shared/context/AuthProvider.tsx`), not
`localStorage` — ADR-017 still rules that out for this bearer token,
same XSS-exposure reasoning. This is a deliberate, disclosed *update* to
this file's own prior policy, made because that policy's trade-off is
exactly finding #1: the brief explicitly directed persisting across a
reload while keeping `localStorage` off the table, and Task 2 itself
anticipated the mechanism might be "genuinely absent" and authorized
adding the minimum. `sessionStorage`: cleared on tab/window close, never
shared across tabs or browser restarts, and re-validated against the
token's own `expiresAtUtc` on every read — an already-expired stored
session is discarded, never silently restored (proven in
`AuthProvider.test.tsx`).

**RoleSwitcher no longer overrides — and no longer ships.** The "must not
override" half was already true (`useEffectiveRole`'s
`authenticatedRole ?? actor.role` precedence never lets the preview role
win once real auth exists) — verified, not changed. The "must not appear
in production" half was **not** true before this phase: `RoleSwitcher` is
now a `React.lazy` dynamic import gated behind `import.meta.env.DEV` in
`AppLayout.tsx`, the identical Phase D1 `StyleGuidePage` precedent — Vite's
`define` transform replaces `import.meta.env.DEV` with the literal `false`
before Rollup bundles, collapsing the whole ternary (dynamic `import()`
included) to dead code that gets tree-shaken out entirely. Verified the
same way Phase D1 verified `StyleGuidePage`'s exclusion (§6). It also now
renders `null` whenever a real session is signed in — a control that
visibly claims to change role but can no longer do anything (since
`useEffectiveRole` never lets it win) would only confuse a signed-in user.

**Cross-test contamination this change surfaced, fixed once, globally.**
`sessionStorage` persists across every test within a jsdom `window`
(unlike `localStorage`, which several test files already clear ad hoc per
file); adding real persistence without clearing it caused a real, silent
bleed — a session written by one test's `AuthHarness` was still present
for the *next* test's fresh `AuthProvider` mount, producing seemingly
random 15-second timeouts in totally unrelated test files
(`DeclareAvailabilityPage.test.tsx`, `BookSessionPage.test.tsx`,
`AdminResetPasswordPage.test.tsx`) whose rendered output silently
differed because of leftover auth state. Fixed once, globally, in
`frontend/src/test/setupTests.ts` (`afterEach(() => window.sessionStorage.clear())`)
rather than requiring every test file to remember its own `beforeEach`.

## 3. Task 3 — Showing who is logged in

`AuthStatus` (`frontend/src/layouts/AuthStatus.tsx`) now shows the role
label (`ACTOR_ROLE_LABEL`, e.g. "Parent/Guardian" rather than the raw
backend string `"ParentGuardian"`) in bold, plus the account id in a
smaller, monospace, muted line — hidden below the `sm` breakpoint, where
the AppBar is already tight (Phase D2 Task 5) — instead of Sign out. As
§1 states plainly: **no name is shown, because none exists to show.**
`LoginResultDto` carries no name or email, and inventing a display name
would be exactly the kind of new business-rule invention CLAUDE.md's stop
conditions rule out. The account id is the only additional identifying
value the backend actually returns, so that is what is shown — honestly
labeled, not disguised as a name.

## 4. Task 4 — Role-scoped navigation and a real route guard

**Nav-item visibility (UX only).** Students, Parent/Guardians, and
Relationships entries in `NavSidebar.tsx` gained the `roles` restriction
they were missing (`["Student", "ParentGuardian", "AdminStaff"]`, i.e.
Tutor excluded) — grounded in `AUTHORIZATION_MATRIX.md`'s own Addendum
Decision 3 (a Tutor is never Owner or a confirmed-Relationship party to
either record type) and `RolePermissionCatalog` (`InviteRelationship`/
`ConfirmRelationship` are Student/ParentGuardian only). `TutorDetailPage`'s
actions are now gated on the same `useEffectiveRole` signal — AdminStaff
sees Approve/Suspend, Tutor sees Edit offering, everyone else sees
nothing (booking itself remains a separate, later phase this one is a
prerequisite for, not wired here).

**The actual route guard — more than hiding a link.** A new
`RequireRole` component (`frontend/src/shared/components/RequireRole.tsx`)
wraps a route's element; reached by direct URL, a role outside its
allowed list gets `ForbiddenState` (a plain "Not authorized" `Alert` with
a link home) instead of the page's real content. Wired in
`router.tsx` for every route whose backend permission
(`RolePermissionCatalog`) grants specific roles only:
`tutorPending` → AdminStaff; `tutorEditPattern` → Tutor;
`studentDetailBase/Pattern`, `parentGuardianDetailBase/Pattern`,
`relationships` → Student/ParentGuardian/AdminStaff; `declareAvailability`
→ Tutor; `bookSession` → Student/ParentGuardian; `studentScheduleBase/Pattern`
→ Student/ParentGuardian/AdminStaff; `tutorScheduleBase/Pattern` →
Tutor/AdminStaff; `oversight.adminDashboard`, `oversight.globalSessions`,
`auth.resetPassword` → AdminStaff. Deliberately **not** guarded: routes
that are fine-grained-by-design with no single coarse role (Tutor detail,
Availability Slot detail, Session detail — all legitimately reachable by
any of the four parties, per-instance ownership enforced by the backend
handler itself) and the three public self-registration routes (a Tutor
legitimately registering as a Parent/Guardian too is an endorsed
multi-role scenario, not a gap).

**No new client-only permission model.** `RequireRole` reads the exact
same `useEffectiveRole` signal and the exact same role lists already
used for nav filtering — one signal, two consumers, not two models. It
denies by default when the role is unknown (`null`), the opposite of
`NavSidebar`'s own deliberately permissive "show everything until a role
is known" display convenience — a real access-control boundary must
default to deny, not to exploratory-browsing convenience. The dev preview
role can still satisfy the guard when *not* really authenticated (so a
developer can still preview any role's actual page, unauthenticated,
exactly as `RoleSwitcher` was always meant to support) — once a real
session exists, only the real role can.

**The server remains the real boundary, confirmed live, not assumed
(§6):** `GET /tutors/pending` returns `200` for the seeded AdminStaff
token and `403 Authorization.Forbidden` for the seeded Student token —
proving the client guard's role list for that route matches what the
backend actually enforces today, not a stale assumption.

## 5. Task 5 — Coverage

New: `AuthProvider.test.tsx` (persistence across a simulated reload,
expired-session discarding, malformed-JSON discarding, never touches
`localStorage`), `RequireRole.test.tsx` (allowed/denied/deny-by-default/
dev-preview-still-works), `AuthStatus.test.tsx` (role label + account id
shown, Sign in link when signed out). Updated:
`NavSidebar.test.tsx` (Tutor no longer sees Students/Parent-Guardians/
Relationships; Student still does), `RoleSwitcher.test.tsx` (renders
nothing once signed in), `AppLayout.test.tsx` (RoleSwitcher visible
signed-out, hidden signed-in), `App.test.tsx` ("Acting as (dev only)"
now resolves asynchronously — `RoleSwitcher` is lazy).

**Inverted, in the open, per Task 5's own instruction:**
`TutorDetailPage.test.tsx`'s pre-existing `"shows Admin approve/suspend
actions"` test asserted exactly the old, broken behavior — Approve/Suspend
rendering with **no role selected at all**. Rewritten (not silently
loosened, Phase 1B precedent) to require the AdminStaff dev role, with new
sibling cases for Tutor (sees Edit offering only), Student (sees nothing),
and no-role (sees nothing) — the literal finding #3 fix, tested.

## 6. Live proof — real login, at least two roles

No browser-automation tool exists in this environment (same constraint
noted in Phases 4.7 §6, 4.8 §5, D2 §5); this combines a real backend
(`dotnet run`, real PostgreSQL) exercised with the exact requests the
frontend sends, with the full RTL/jsdom test suite exercising the exact
click/type/render flow a browser would — the same evidentiary standard
this repo's frontend testing already uses throughout.

**Real login as the seeded Student — confirms the exact identity data
AuthStatus now displays:**

```
$ curl -s -X POST http://localhost:5046/auth/login -H "Content-Type: application/json" \
    -d '{"email":"student@tutorflow.dev","password":"Seed-Password-123!"}'

{"isSuccess":true,"value":{"token":"K85BASHPhplq1d1IAyUzZw4Wsj2EWzslhNsCSauplyU=",
 "accountId":"6ce0e5f6-b91d-48e4-ae57-2596a768adc3","role":"Student",
 "expiresAtUtc":"2026-07-27T05:38:15.5115947Z"}}
```

Given this response, `AuthStatus` renders `"Student"` (bold) +
`6ce0e5f6-b91d-48e4-ae57-2596a768adc3` (muted, monospace) + "Sign out" —
proven by `AuthStatus.test.tsx`'s "shows the signed-in identity" test using
this exact shape.

**Real login as the seeded AdminStaff, and the route guard's server-side
counterpart — confirms a second role, and that the guard's role list
isn't a client-only assumption:**

```
$ curl -s -X POST http://localhost:5046/auth/login -H "Content-Type: application/json" \
    -d '{"email":"admin@tutorflow.dev","password":"Dev-Admin-Password-123!"}'

{"isSuccess":true,"value":{"token":"dPr0nNKXwnNdl6LA3NNifDPrbcTOiMHgYpV46AtlVe8=",
 "accountId":"77bc3667-8a16-470f-830e-6f7bde99fdb6","role":"AdminStaff",
 "expiresAtUtc":"2026-07-27T05:38:15.9278108Z"}}

$ curl -s http://localhost:5046/tutors/pending -H "Authorization: Bearer <student token>" -w "\nHTTP %{http_code}\n"
{"isSuccess":false,"error":{"code":"Authorization.Forbidden", ...}}
HTTP 403

$ curl -s http://localhost:5046/tutors/pending -H "Authorization: Bearer <admin token>" -w "\nHTTP %{http_code}\n"
{"isSuccess":true,"value":{"items":[...6 pending Tutors...],"totalCount":6, ...}}
HTTP 200

$ curl -s http://localhost:5046/sessions -H "Authorization: Bearer <student token>" -w "\nHTTP %{http_code}\n"
{"isSuccess":false,"error":{"code":"Authorization.Forbidden", ...}}
HTTP 403
```

This is `RequireRole`'s `tutorPending` and `oversight.globalSessions`
guards' own role lists (`["AdminStaff"]`) checked against the real,
currently-enforced backend — a Student is rejected by the server exactly
as the client guard would already have shown `ForbiddenState` for, and
AdminStaff is let through by both layers identically.

**Full RTL/jsdom render of the exact UI flow, both roles** — this is the
"in a browser" claim's real evidentiary weight, given the tooling
constraint above:

- `AuthStatus.test.tsx`: renders "Sign in" signed-out; renders
  `"ParentGuardian"` → `"Parent/Guardian"`, the account id, and "Sign out"
  once `AuthHarness` simulates a real login — no "Sign in" link remains.
- `AppLayout.test.tsx`: the full shell (AppBar + RoleSwitcher +
  NavSidebar) shows `RoleSwitcher` signed-out, hides it entirely once
  signed in.
- `NavSidebar.test.tsx`: a real signed-in Tutor sees no
  Students/Parent-Guardians/Relationships links; a real signed-in Student
  still does.
- `RequireRole.test.tsx`: an AdminStaff-only guard renders its content for
  AdminStaff and `ForbiddenState` (not the content) for Student.
- `AuthProvider.test.tsx`: a **fresh** `AuthProvider` mount — the same
  thing a hard page reload produces — restores a still-valid session
  written by an earlier mount, and discards an expired one instead of
  restoring it. This is the direct proof for finding #1/#2's fix, since a
  literal `F5` cannot be scripted without browser automation.

**Production-exclusion proof (RoleSwitcher never ships), same method as
Phase D1's `StyleGuidePage` verification:**

```
$ npm run build && grep -rl "Clear role selection\|Now acting as\|None selected" dist/
(no matches, exit code 1)
```

Every file in `dist/` was searched for `RoleSwitcher`'s own implementation
strings — none found. (A plain prose string in `DashboardPage.tsx`
mentioning *"Select a role from Acting as (dev only) above..."* does match
a bare `RoleSwitcher` substring search, but is descriptive copy, not the
component — confirmed by reading the matched line.) No `RoleSwitcher-*.js`
chunk appears in the build's own asset listing either.

## 7. `scripts/verify.ps1`, real output, and merge recommendation

Frontend-only phase — no Domain, Application, Infrastructure, or Web
(backend) file was touched, confirmed by `git status`. Two unrelated
environment locks were hit and cleared before the runs below, same
category of stateless dev-process lock prior phases documented: a
leftover `TutorFlow.Web.exe` from this session's own curl-proof steps, and
a leftover `npm run dev` Vite process from an earlier, unrelated frontend
session — both killed via `Stop-Process` before re-running.

```
=================== VERIFY SUMMARY ===================
dotnet restore                                PASS                              2.1s
dotnet build (0 warnings)                     PASS                             10.3s
Backend: dotnet test TutorFlow.sln (run 1 of 2) PASS                              36s
Backend: dotnet test TutorFlow.sln (run 2 of 2) PASS                            26.4s
Backend: Postgres integration tests           PASS                              8.1s
npm ci                                        PASS                             42.5s
Frontend: npm run lint                        PASS                             21.7s
Frontend: npm run build                       PASS                             24.2s
Frontend: npm test -- --run                   PASS                             82.6s
-------------------------------------------------------
Total elapsed: 254.1s

RESULT: PASS
```

Frontend: 53 test files, 255 tests passed (236 before this phase + 19 net
new/inverted); `tsc -b && vite build` and `eslint .` both clean, 0
warnings. Backend totals unchanged from Phase 4.8 (no backend file
touched this phase).

**Ready to merge.** No new backend capability or business rule — the one
genuinely new mechanism, `sessionStorage`-backed persistence, is a
frontend policy update explicitly directed by this phase's own brief to
close a documented, disclosed trade-off that had become the live-browser
defect it exists to prevent (finding #1), not an invented one. No test was
weakened, skipped, or deleted; the one behavior-inverting test change
(`TutorDetailPage`'s old "shows Admin approve/suspend actions with no role
selected" assertion) is explained in its own right (§5), matching Phase
1B's precedent. One documentation-drift finding is flagged for the owner
(§1) rather than silently corrected: `AUTHORIZATION_MATRIX.md`'s WP3
Status column no longer matches the code, which already has
`RequirePermission` wired in. Booking itself remains untouched and
unblocked for its own later phase, which this one was explicitly written
to be a prerequisite for.

# ADR-0023: Browser Session, Token Storage, and Auth Scope for Identity

**Status:** Accepted

## Context

`ADR-0002` settled *who* issues identity (ASP.NET Core Identity + JWT, claims-based boundary) but
left the browser-session question open — at the time, no frontend auth code existed at all.
Since then, `useAuth()` and `AuthGateModal` were built against a guest-only stub, explicitly
documented (`ADR-0022` §"Explicitly deferred") as waiting for "a real session store."

That phase has arrived. Ground-truth audit of the current implementation (2026-10-05) found:

- `POST /api/identity/login` and `POST /api/identity/register` return the JWT only in the JSON
  response body. Nothing sets a cookie anywhere in the Host or Identity module.
- CORS (`Program.cs`) is origin-restricted but does **not** call `AllowCredentials()`.
- The frontend has zero storage mechanism of any kind — no `localStorage`, no cookie, no
  in-memory store (`document.cookie`/`localStorage`/`sessionStorage` grep: zero matches in
  `apps/web/src`).
- `apps/web/src/lib/api/client.ts` cannot send a request body on any verb — there is no
  `apiPost`, and `sendJson`'s `POST`/`PATCH` branches are unused dead code.
- No `Logout`, `RefreshToken`, `ConfirmEmail`, or `ResetPassword` endpoint exists anywhere in
  `src/` (repo-wide grep, zero matches).
- `RequireConfirmedEmail = false` is set explicitly; no email-sending infrastructure exists in
  any module.
- `/login` and `/signup` are literal `ComingSoon` stubs with no form.

This ADR makes the decisions `ADR-0002` deferred, scoped strictly to what the current product
needs: a real signup/login/logout/session loop for a same-site SPA + API, without inventing
refresh tokens, email verification, or social login the product hasn't asked for yet.

## Decision

### 1. Token transport: httpOnly cookie, not localStorage, not a dual-exposed body

The access token is set by the server as an `HttpOnly`, `Secure`, `SameSite=Lax` cookie
(`TutorFlowAuth`) on successful `/register` and `/login`, and cleared by a new
`POST /api/identity/logout`. It is **never** placed in a JSON response body, and the frontend
never reads or stores it directly.

- `Secure=true` always, including local dev — Chromium/Firefox/Safari all treat `localhost`
  as a secure context and will set/send `Secure` cookies over plain HTTP there. This avoids a
  dev/prod split in cookie policy, which is the actual risk `ADR-0002`'s "security over
  convenience" framing is guarding against.
- `SameSite=Lax` — the frontend (`localhost:3000` / future `app.tutorflow.*`) and backend
  (`localhost:5148` / future `api.tutorflow.*`) are cross-**origin** but same-**site**.
  `SameSite` restricts cross-*site* requests, not cross-origin ones, so `Lax` cookies are sent
  on the frontend's same-site `fetch` calls to the API. **Deployment note:** if frontend and API
  ever end up on genuinely different registrable domains, this must become
  `SameSite=None; Secure`, which additionally requires `AllowCredentials()` + an explicit (not
  wildcard) CORS origin — already true here.
- `Path=/`, `Expires` matching the JWT's own expiry (60 minutes, from `JwtOptions`).
- CORS (`Program.cs`) gains `.AllowCredentials()` on the existing `Frontend` policy. This is
  safe only because the policy already enumerates explicit origins (`Cors:AllowedOrigins`) —
  `AllowCredentials()` with a wildcard origin is rejected by the browser, so no change to that
  constraint is needed.
- `JwtBearerOptions.Events.OnMessageReceived` is configured to read the token from the
  `TutorFlowAuth` cookie when no `Authorization` header is present. Bearer-scheme validation
  (`TokenValidationParameters`) is otherwise completely unchanged — this is cookie-as-*transport*
  for the same stateless JWT, not a second parallel identity system, and does not conflict with
  `ADR-0002`.
- `Login.Response`/`Register.Response` drop the `AccessToken` field. They still return
  `UserId`, `Email`, `Roles`, `ExpiresAtUtc` — enough for the frontend to know who's signed in
  without ever handling the token itself. (`UserId` here is wire data for the API/session layer,
  not surfaced in any URL or UI control — consistent with the "internal IDs are not UX" rule.)

**Rejected:** `localStorage`/`sessionStorage` (XSS-exfiltratable, explicitly ruled out by the
Product Owner), and returning the token in the body *in addition to* the cookie (defeats the
point of `HttpOnly` the moment any component is tempted to read and cache it).

### 2. Logout: cookie-clear only, no server-side revocation list

`POST /api/identity/logout` clears the cookie and returns `204`. It is `[AllowAnonymous]` so a
client with an already-expired/missing cookie can still call it idempotently (e.g., a stale tab).

JWTs remain stateless — there is no token-blacklist table. A token already issued stays
cryptographically valid until its own 60-minute expiry even after "logout," which only stops the
browser from presenting it automatically. This is a known, explicitly accepted trade-off: the
alternative (a revocation store) is a real feature with its own storage/cleanup concerns, and the
short fixed expiry already bounds the exposure window without it. Flagged as a future hardening
item if the product ever needs stronger logout guarantees (e.g., "log out of all devices").

### 3. No refresh tokens this phase

A session simply expires 60 minutes after login; the user must log in again. No refresh-token
table, no rotation, no silent-renewal endpoint. This is the smallest change consistent with "the
safest architecture compatible with the existing backend" — a refresh-token system is a
meaningfully larger surface (storage, rotation, reuse-detection) that the product hasn't asked
for yet, and introducing it half-built would be worse than not having it. Tracked as a follow-up
once session length in practice proves too short.

### 4. Email verification and password reset: deferred, not implemented

Preply requires email verification before some actions and supports a token-based password
reset flow. Both are deliberately **not** built this phase:

- No transactional-email-sending infrastructure exists anywhere in the codebase (no SMTP/SendGrid
  client, no template system, in any module including `Notifications`). Building verification or
  reset emails on top of nothing would mean either faking delivery (explicitly forbidden) or
  quietly building an email subsystem as a side effect of an auth phase — out of this phase's
  scope ("no unrelated feature implementation").
- `RequireConfirmedEmail` stays `false`. Registration continues to create an immediately-usable
  account, matching the existing, already-tested behavior.
- **Recommendation for a future Identity sub-phase:** add email-sending infrastructure first
  (likely its own concern, not bolted onto `Notifications` speculatively), then build
  `ConfirmEmail`/`ForgotPassword`/`ResetPassword` as real `UserManager` token flows
  (`GenerateEmailConfirmationTokenAsync`, `GeneratePasswordResetTokenAsync` — both already
  available for free via `AddDefaultTokenProviders()`, already registered). Nothing in this ADR
  blocks that; the cookie/session model above is unaffected by when it lands.

### 5. OIDC / social login: deferred, boundary already compatible

Not implemented. `ADR-0002`'s claims-based boundary (`UserId` + role claims, not ASP.NET Identity
types leaking into other modules) already makes this additive later — a social/OIDC provider
would populate the same claim set through a different `SignInManager` path, with no change to how
other modules or the JWT-cookie transport work. No code changes made in support of this; it's a
statement that nothing above forecloses it.

### 6. Authorization model: unchanged, bug-fixed at the integration points

`StudentOnly`/`TutorOnly` policies, `RoleNames`, and the dual-role model are already correct and
already tested (`AuthorizationPolicyTests`, `User_CanHoldBothStudentAndTutorRoles_...`). No
backend authorization changes are needed. What *is* fixed: `useAuth()`'s frontend shape exposed a
singular `role: AuthRole | null`, which every Tutor Profile call site (`BookingPanel`) used as
`role === 'Student'` — a mutually-exclusive-role assumption baked into the UI layer, contradicting
`ADR-0002` §"Dual Role Accounts" even though the backend itself never assumed it. `useAuth()` now
exposes `roles: AuthRole[]`, and call sites check `roles.includes('Student')` /
`roles.includes('Tutor')` instead. One real behavior change falls out of this: `BookingPanel`
previously collapsed to a price-only view for *any* viewer with the Tutor role, which would have
hidden a dual-role (Student + Tutor) viewer's own booking/message/favorite actions on someone
else's profile. It now only collapses for a viewer who is Tutor **and not also** Student.

## Why not Next.js's own session pattern (Server Actions + `cookies()` + DAL)

Next.js's bundled authentication guide assumes Next's own server is the backend: Server Actions
call a database directly and set the session cookie via `next/headers`' `cookies()`. That doesn't
apply here — `ADR-0009` already settled Next.js as a pure client (`apps/web`) in front of a
separate .NET API (`TutorFlow.Host`), and the session cookie in this ADR is set by *that* API's
origin, not Next's. A browser never sends a cookie scoped to `api.tutorflow.*` to a request aimed
at `app.tutorflow.*` (or `localhost:5148` to `localhost:3000` in dev) — so Next's own server
(Server Actions, Route Handlers, Proxy/middleware, `cookies()`) literally cannot see this cookie
regardless of pattern choice. The only place that can see it is the browser itself, attaching it
automatically to `fetch` calls made *to* the API origin with `credentials: 'include'`. This is
why the frontend implementation below stays entirely client-side (React Query + the existing
typed `apiGet`/`apiPost` client), consistent with how `useAuth()`/`AuthGateModal`/Tutor Profile
already work — not a stylistic choice, a consequence of where the cookie's origin actually is.

## Frontend auth state: one real hook, session discovered via `/me`

`useAuth()` becomes a thin wrapper around a single React Query–backed session query
(`sessionQueryKey = ['session', 'currentUser']`, calling `GET /api/identity/me`). No separate
`AuthProvider`/context component was added — `QueryClientProvider` already wraps the whole tree
(`app/providers.tsx`), and React Query dedupes by query key, so every `useAuth()` call site
anywhere in the tree shares the exact same cached session automatically. This is the same pattern
every other shared read in this codebase already uses (e.g. `useFavoriteStatus`); a bespoke
context would have been a second, parallel mechanism for the same thing. Because the token lives
in an `HttpOnly` cookie the frontend can't read, this `/me` round-trip on mount is the *only* way
the SPA can know whether it's signed in — there is no synchronous "check localStorage" shortcut,
and that's intentional:

- `isLoading` is real (the query is in flight) — components must handle it, not assume synchronous
  guest-by-default.
- `401` from `/me` is treated as "not authenticated," not an error toast.
- Login/Register/Logout mutations call `queryClient.invalidateQueries(['session'])` (or directly
  `setQueryData`) so every consumer re-renders from one source of truth — no second parallel
  "am I logged in" flag anywhere.

This preserves the exact contract `ADR-0022` promised: `TutorProfileView`, `BookingPanel`, and
`TutorAvailability` do not change shape, only `useAuth()`'s implementation does (plus the
`role` → `roles` fix in §6 above, which **is** a call-site change, but a correctness fix mandated
by `ADR-0002`, not an architecture change).

## Auth gate UX: redirect-with-return-to, not an inline modal form

`AuthGateModal` keeps linking to `/login` and `/signup` (now real pages) rather than growing
inline email/password fields itself. Each link carries `?returnTo=<current path>`; `LoginPage`
and `SignupPage` read it and, on success, `router.replace(returnTo ?? '/')` instead of a fixed
redirect. This satisfies the required "anonymous → auth gate → login/signup → return to tutor
profile → intended action can continue" flow without duplicating full form validation/error UX
inside a modal — the modal's job stays "explain why, send you to the right place," the page's job
is the actual form. This is a deliberate product/UX call, made explicitly here rather than
silently: Preply itself favors inline modal auth, but building a second, modal-scoped copy of the
login/signup forms (with its own validation and error states) for marginal UX gain is more
surface than this phase's scope justifies.

## Consequences

- `Login.Response`/`Register.Response` change shape (drop `AccessToken`) — a breaking change to
  `IdentityEndpointsTests.cs`, updated alongside this ADR to assert a `Set-Cookie` header instead.
- `apps/web/src/lib/api/client.ts` gains `credentials: 'include'` on every request and a new
  `apiPost` helper that can send a JSON body — both were missing entirely.
- `useAuth()`'s public shape changes from `{ isAuthenticated, userId, role }` to
  `{ isAuthenticated, isLoading, userId, roles, logout }`; the one call-site fix this forces
  (`role` → `roles.includes(...)`) is covered above.
- A production deployment where frontend and API are **not** same-site must revisit the
  `SameSite=Lax` choice (→ `SameSite=None`) — flagged inline above, not a silent assumption.
- No new module, no new ADR-violating pattern, no schema beyond the existing `identity` schema
  (no new tables — cookie state is not persisted server-side).

## Open follow-ups (explicitly not resolved here)

- Email verification / password reset — blocked on email-sending infrastructure not existing yet.
- Refresh tokens / "log out of all devices" — not needed until session length proves too short.
- Social/OIDC login — not needed until a product requirement names a provider.

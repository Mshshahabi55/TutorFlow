# Phase 4.8 Report — Fix the CORS / Dev-Port Mismatch That Blocks Every Form

Branch: `develop` | Commits this phase: `531ebe0` (Task 1 — deterministic
dev port), `85ba5d3` (Task 2 — Development CORS widened to both dev
ports), `072feed` (Task 3 — the preflight regression guard). This report
is its own commit, no further code change; no code file was touched for
Task 4 (see §6: no misleading copy was found).

**Status: complete, 9/9 `scripts/verify.ps1` green.** Root cause,
confirmed live before any fix: the Vite dev server came up on
`http://localhost:5174` because `5173` was already in use (no
`strictPort`, so Vite silently fell back), while the backend's CORS
policy (`appsettings.Development.json`, from Phase 2.5) allowed only
`http://localhost:5173` — every preflight from the real dev server was
rejected, surfacing in the browser as an undifferentiated "Network
Error" on every form (registration, password reset, everything).

## 1. Clean Architecture validation

No Domain, Application, or Infrastructure file was touched. The only
backend file changed is `Web`'s own `appsettings.Development.json` — a
configuration value, not code — and `Web.Tests`. `Program.cs`'s CORS
wiring (`builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()`
→ `policy.WithOrigins(corsAllowedOrigins)...`) already reads an arbitrary-
length array; adding a second allowed origin required zero code change,
only a config value. No new package added anywhere (`dotnet build
TutorFlow.sln`: **0 Warnings, 0 Errors**, confirmed below). Production
`appsettings.json`'s `Cors.AllowedOrigins` remains `[]`, untouched by this
phase — verified by diff (§7).

## 2. Repository convention validation

Not applicable this phase — no repository, `DbContext`, or persistence
code was touched. No migration, no entity, no query path changed.

## 3. Task 1 — Deterministic dev port

`frontend/vite.config.ts`'s `server` block now sets `strictPort: true`
alongside the existing `port: 5173`:

```ts
server: {
  // Deterministic dev port: silently drifting to another port (Vite's
  // default fallback when 5173 is already taken) is exactly what broke
  // every form behind CORS in Phase 4.8 — the backend's
  // Cors:AllowedOrigins only ever matched the port that happened to be
  // free, not necessarily 5173. strictPort makes a port conflict a loud
  // startup failure instead of a silent drift.
  port: 5173,
  strictPort: true,
},
```

With `strictPort`, a second `npm run dev` (or any other process already
bound to `5173`) now fails Vite's own startup with `Error: Port 5173 is
already in use` instead of silently relocating to `5174` and leaving the
CORS mismatch to surface later, indirectly, as a browser network error.
This trades a same-instant loud failure for what was previously a
delayed, confusing one — the right trade for a locally-run dev server.

## 4. Task 2 & Task 3 — Development CORS widened, and the regression guard

**Task 2.** `backend/src/Web/appsettings.Development.json`'s
`Cors.AllowedOrigins` now lists two explicit named origins, not one:

```json
"Cors": {
  "AllowedOrigins": ["http://localhost:5173", "http://localhost:5174"]
}
```

`5173` remains the deterministic port Task 1 pins the dev server to;
`5174` is kept allowed as a deliberate margin — the one port Vite's own
fallback would have picked before Task 1, and the cheapest way to make
this failure mode non-recurring even for a developer running an older
checkout of `vite.config.ts` or a second concurrent instance. Both are
explicit named origins passed to `policy.WithOrigins(...)` — never
`AllowAnyOrigin`, per this phase's absolute rules. `appsettings.json`
(production defaults) is untouched: `"AllowedOrigins": []` before and
after, confirmed by `git diff`.

**Task 3.** `CorsConfigurationTests.cs` — the Phase 2.5 preflight-proof
pattern — is widened, not just re-pointed. Its single `CreateClientWithAllowedOrigin(origin)`
helper (which set exactly one `Cors:AllowedOrigins:0` setting) is replaced
by `CreateClientWithAllowedOrigins(params string[] origins)`, which sets
one `Cors:AllowedOrigins:{i}` per origin — mirroring how
`appsettings.Development.json`'s own array binds. The single `[Fact]` is
replaced by a `[Theory]` with `InlineData` for both `http://localhost:5173`
and `http://localhost:5174`, each asserting the real ASP.NET Core CORS
middleware's preflight response carries `Access-Control-Allow-Origin`
matching that exact origin, `Access-Control-Allow-Methods`,
`Access-Control-Allow-Headers`, and (unchanged assertion) no
`Access-Control-Allow-Credentials`. The existing negative test (an
unconfigured origin gets no `Access-Control-Allow-Origin` header) is kept,
reconfigured to run against both allowed origins configured at once —
proving the negative case still holds once there are two allowed origins,
not just one.

This closes the exact gap the brief names: this class of failure passed
every test before Phase 4.8 because CORS only manifests in a real browser
preflight, never in a same-origin test client or a handler-level unit
test. `CorsConfigurationTests` already used `WithWebHostBuilder` +
`UseSetting` to drive a genuine preflight through the real middleware
(Phase 2.5), so the fix was to make that coverage match the actual
`AllowedOrigins` array shape, not to invent a new test technique.
**Honest limit, stated plainly:** this guard proves the *policy*, not the
*real dev server's actual bound port*. If a developer's `vite.config.ts`
were edited to a third port never added to `Cors:AllowedOrigins`, this
test suite would stay green while the browser would still fail — the
same category of gap Task 1's `strictPort` closes from the other
direction (by making the port itself non-drifting), not something a
backend-only test can close alone.

## 5. Task 5 — Proof: a real cross-origin request now succeeds

Backend started via `dotnet run --launch-profile http`
(`ASPNETCORE_ENVIRONMENT=Development`, `http://localhost:5046`, real
`tutorflow_dev` Postgres database, per `dotnet user-secrets list`). No
browser-automation tool exists in this environment (same constraint noted
in Phase 4.7 §6 and Phase D2 §5), so the proof is `curl` sending the exact
headers a real browser sends for a cross-origin preflight and the
subsequent request — not a same-origin call, not a mock.

**Preflight from the pinned dev port (5173):**

```
$ curl -sD - -o /dev/null -X OPTIONS http://localhost:5046/students \
    -H "Origin: http://localhost:5173" \
    -H "Access-Control-Request-Method: POST" \
    -H "Access-Control-Request-Headers: content-type"

HTTP/1.1 204 No Content
Access-Control-Allow-Headers: content-type
Access-Control-Allow-Methods: POST
Access-Control-Allow-Origin: http://localhost:5173
Vary: Origin
```

**Preflight from the fallback dev port (5174) — proves the margin works too:**

```
$ curl -sD - -o /dev/null -X OPTIONS http://localhost:5046/students \
    -H "Origin: http://localhost:5174" \
    -H "Access-Control-Request-Method: POST" \
    -H "Access-Control-Request-Headers: content-type"

HTTP/1.1 204 No Content
Access-Control-Allow-Headers: content-type
Access-Control-Allow-Methods: POST
Access-Control-Allow-Origin: http://localhost:5174
Vary: Origin
```

**Preflight from an origin nowhere in configuration — confirms the policy still rejects everything else:**

```
$ curl -sD - -o /dev/null -X OPTIONS http://localhost:5046/students \
    -H "Origin: https://not-allowed.example" \
    -H "Access-Control-Request-Method: POST" \
    -H "Access-Control-Request-Headers: content-type"

HTTP/1.1 204 No Content
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Referrer-Policy: no-referrer
```
(no `Access-Control-Allow-Origin` header at all — a real browser would block this response from ever reaching frontend JS.)

**The real registration request itself — a live student self-registration through the previously-broken origin:**

```
$ curl -sD - -X POST http://localhost:5046/students \
    -H "Origin: http://localhost:5173" \
    -H "Content-Type: application/json" \
    -d '{"email":"phase48-1785122745@example.com","password":"Str0ng-Passw0rd!","isMinor":false}'

HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
Access-Control-Allow-Origin: http://localhost:5173
Vary: Origin

{"isSuccess":true,"isFailure":false,"error":null,"value":{"studentId":"2ee531bc-29aa-4047-9adb-ab9980b2d97d","isMinor":false}}
```

This is the exact request `RegisterStudentPage.tsx` sends
(`POST /students`, `Content-Type: application/json`) from the real Vite
dev origin, hitting the real `dotnet run` process against real
PostgreSQL, and receiving both a `200` body and the
`Access-Control-Allow-Origin` header a browser requires before it will
let frontend JS read that body. Before this phase's fix, the equivalent
request from `http://localhost:5174` (Vite's actual fallback port) would
have returned `200` with **no** `Access-Control-Allow-Origin` header —
CORS does not block the server from processing the request or from
persisting the write; it blocks the *browser* from letting JavaScript
read the response, which is exactly why the original symptom was an
opaque "Network Error" with no server-side error to debug from.

## 6. Task 4 — The email gap, audited: no misleading copy exists

Read every place a registration or password-reset flow could imply an
email is sent: `LoginPage.tsx` (no "forgot password" link exists at all —
nothing to word), `RegisterStudentPage.tsx`, `RegisterTutorPage.tsx`,
`RegisterParentGuardianPage.tsx` (each success state shows only
`"<Role> account registered."` plus the new id — no mention of email,
confirmation, or verification anywhere), and `AdminResetPasswordPage.tsx`
(its own doc comment already states plainly: *"Admin-assisted password
reset only — no self-service recovery flow exists yet
(docs/adr/ADR-017-authentication-mechanism-decision.md)"*, and its page
copy reads "Resetting a password immediately signs that account out of
every device" — never implying a reset email). `paths.ts` has no
`forgotPassword` route, only `resetPassword` (`/auth/reset-password`),
the already-honest admin-assisted page.

**Conclusion: no copy change was made, because none was needed.** Every
surface already matches ADR-017's decision (admin-assisted reset,
self-service recovery explicitly out of scope for RC1) — this predates
Phase 4.8. The owner's "doesn't send an email" report is best explained
by Phase 4.8's own root cause (§ above), not a UI-honesty gap: with every
form request failing before it left the browser (a CORS-rejected
preflight), a developer testing registration would have seen no success
state, no error detail, and no way to distinguish "nothing happened" from
"an email should have been sent but wasn't" — the opaque Network Error
gave no signal either way. §5's live proof shows registration now
succeeds and returns the same honest, already-existing "account
registered, here's your id" success state with no email promised. Per
this phase's absolute rules, no email-sending capability was built.

## 7. `scripts/verify.ps1`, real output, and merge recommendation

Two unrelated environment locks were hit and cleared before the green run
below — a leftover `TutorFlow.Web.exe` (from an earlier `dotnet build` in
this same session) and a leftover Vite dev server + its `esbuild.exe`
(from an earlier, unrelated frontend session) — both killed via
`Stop-Process` before re-running. Same category of stateless dev-process
lock Phases 4.6/4.7 already documented hitting and clearing, not a defect
in any code this phase changed.

```
=================== VERIFY SUMMARY ===================
dotnet restore                                PASS                              1.3s
dotnet build (0 warnings)                     PASS                                3s
Backend: dotnet test TutorFlow.sln (run 1 of 2) PASS                            28.2s
Backend: dotnet test TutorFlow.sln (run 2 of 2) PASS                            28.6s
Backend: Postgres integration tests           PASS                              7.3s
npm ci                                        PASS                             39.3s
Frontend: npm run lint                        PASS                             23.8s
Frontend: npm run build                       PASS                             24.3s
Frontend: npm test -- --run                   PASS                               75s
-------------------------------------------------------
Total elapsed: 230.9s

RESULT: PASS
```

Backend totals: Domain 76, Application 227, Infrastructure 61 + 10
Postgres, Web 184 (183 before this phase + 1 net new, since the single
`[Fact]` `CorsConfigurationTests` widened into a 2-case `[Theory]` plus
the kept negative `[Fact]` — 3 tests total, up from 2) — all passed, both
determinism-guard runs identical, 0 failed, 0 skipped. Frontend: 50 test
files, 236 tests passed; `tsc -b && vite build` and `eslint .` both clean.

**Ready to merge.** No design-system restyle, no payments code, and no
business-logic change anywhere in this phase — every file touched is
configuration (`vite.config.ts`, `appsettings.Development.json`) or test
coverage (`CorsConfigurationTests.cs`). No test was weakened, skipped, or
deleted — the two original `CorsConfigurationTests` assertions both still
run, widened to cover a second origin plus the port-drift scenario that
caused this phase. No credential or connection string was committed;
`appsettings.json`'s `REPLACE_ME` placeholder and empty `Cors.AllowedOrigins`
are both untouched. Task 4 required no code change — the UI was already
honest about email (ADR-017); this is recorded here so the owner's report
is answered, not silently dropped.

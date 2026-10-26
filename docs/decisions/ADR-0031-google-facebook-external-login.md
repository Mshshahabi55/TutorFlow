# ADR-0031: Google and Facebook External Login

**Status:** Accepted (implementation) / **Open** (items listed under "Unresolved — requires approval")

## Context

`ADR-0023` (Identity session and auth architecture) settled TutorFlow's real signup/login/logout
session loop and explicitly deferred social login: *"Not implemented... a social/OIDC provider
would populate the same claim set through a different SignInManager path, with no change to how
other modules or the JWT-cookie transport work. No code changes made in support of this; it's a
statement that nothing above forecloses it."* Its own "Open follow-ups" list still reads
*"Social/OIDC login — not needed until a product requirement names a provider."*

That follow-up is now **stale, not silently changed**: Google and Facebook were named as a real
product requirement, relayed through this project's ChatGPT-as-product-owner/Claude-as-executor
workflow (not a live Preply-verified fact — Preply's own signup/login pages were directly
observed offering Google/Facebook buttons, `docs/product/preply-reverse-engineering/`, but this
ADR's decision to build them follows from the explicit product request, not from Preply parity
alone). `ADR-0023` is not edited by this document — the same convention already used elsewhere in
this repo for a prior document whose assumptions were later superseded (`03-tutor-journey.md`'s
treatment of `docs/product/TUTOR_JOURNEY.md`: cross-reference by name, do not edit the original).

## Decision

### 1. Reuses the existing Identity module and session architecture — no parallel auth system

Google/Facebook sign-in produces a session through the exact same mechanism `Register`/`Login`/
`BecomeTutor` already use: `ITokenService.CreateAccessToken()` issues the JWT, `AuthCookie.Issue()`
sets it as the same `HttpOnly`/`Secure`/`SameSite=Lax` cookie (`ADR-0023 §1`). No second token
service, no parallel session model, no change to `JwtBearerOptions`/`TokenValidationParameters`.
`JwtBearerDefaults.AuthenticationScheme` remains the application's only *default* scheme — Google,
Facebook, and a temporary `ExternalLogin` cookie scheme are additional, named schemes invoked only
explicitly (`TypedResults.Challenge`/`HttpContext.AuthenticateAsync` with an explicit scheme name),
never as an `[Authorize]`/`RequireAuthorization()` fallback.

### 2. Account resolution — verified in code and tests, not assumed

`Features/ExternalLogin/CompleteExternalLogin.cs`'s `Handler`:
- Looks up an existing link via ASP.NET Core Identity's native `UserManager.FindByLoginAsync`
  against the **already-existing** `AspNetUserLogins` table (`Migrations/20261003185601_InitialCreate.cs`
  already creates it — confirmed by direct inspection, not assumed — because `IdentityDbContext`
  inherits the full `IdentityDbContext<TUser,TRole,TKey>`, not a stripped-down base). **No
  migration was needed or added.**
- Never auto-links a verified provider email to an existing password account — returns a distinct
  `EmailConflict` outcome instead, requiring the caller to log in to the existing account first (a
  secure linking flow is explicitly **not** built in this phase — approved decision, see
  "Unresolved" below for its own follow-up).
- Rejects account creation outright when no provider-verified email is present (`EmailRequired`
  outcome) — Google's `email_verified` claim is checked explicitly; Facebook's API is documented to
  never return an email at all unless Facebook itself already confirmed it, so presence is
  Facebook's own verification gate (package/platform documentation, **not empirically verified
  against a live app** — no real Facebook credentials exist in this environment; see "Unresolved").
- New accounts always receive exactly the `Student` role — the `Command` type has no role-carrying
  field at all, so client-supplied role elevation is structurally impossible, not merely checked.
- Guards the `CreateAsync` → `AddLoginAsync` → `AddToRoleAsync` sequence (no ambient DB transaction
  spans separate `UserManager` calls) with compensating cleanup (`DeleteAsync` on any later-step
  failure) plus `DbUpdateException`/`PostgresErrorCodes.UniqueViolation` catches on both the email
  and the `(LoginProvider, ProviderKey)` unique indexes, covering the concurrent-duplicate-request
  race — proven by `CompleteExternalLoginTests.HandleAsync_ConcurrentDuplicateExternalLogin_OnlyOneAccountIsEverCreated`
  (two separate DI scopes, matching how two real concurrent HTTP requests would each get their own
  `IdentityDbContext`), not just reasoned about.

### 3. `returnTo` — a backend mirror of the frontend's own sanitizer, not a new policy

`Infrastructure/ExternalLoginReturnTo.Sanitize()` applies the identical same-origin,
path-relative-only rule as `apps/web/src/features/auth/returnTo.ts`'s `sanitizeReturnTo` (same-origin,
reject `//`, `/\`, and any absolute URL) — necessary because this value round-trips through an
external provider and back, and is never trustworthy just because this app's own links always
construct a safe one. Exhaustively unit-tested (`ExternalLoginReturnToTests.cs`).

### 4. A critical, test-discovered correctness fix: conditional scheme registration

Registering `.AddGoogle()`/`.AddFacebook()` unconditionally, with empty placeholder credentials (no
real Google/Facebook app exists in this environment), was first tried and **broke all 19 of the
pre-existing `IdentityEndpointsTests`** (register/login/logout) — not just OAuth-specific routes.
Root cause, confirmed by direct testing: ASP.NET Core's `AuthenticationMiddleware` resolves every
registered `IAuthenticationRequestHandler` scheme (which includes any OAuth handler, for its own
`CallbackPath` interception) on **every single incoming request**, and resolving that handler
eagerly validates its `Options` — `OAuthOptions.Validate()` throws for an empty `ClientId`. The fix
(`Program.cs`): read `Authentication:Google:ClientId`/`ClientSecret` (and the Facebook equivalents)
directly from configuration *before* calling `AddAuthentication()`, and only chain `.AddGoogle()`/
`.AddFacebook()` when both values are actually present — each provider independently. An
unconfigured provider's scheme then genuinely does not exist, so `AuthenticationMiddleware` never
touches it. The challenge endpoints additionally check `IAuthenticationSchemeProvider.GetSchemeAsync`
before calling `TypedResults.Challenge`, returning a clean `503` instead of an unhandled exception
if a provider is requested while unconfigured.

### 5. Configuration — placeholders only, no real credentials committed

`appsettings.json` documents the shape (`Authentication:Google:{ClientId,ClientSecret}`,
`Authentication:Facebook:{ClientId,ClientSecret}`, both empty strings); `appsettings.Development.json`
adds `Frontend:BaseUrl` (`http://localhost:3000`, not a secret). Real credentials belong in
user-secrets/an environment-specific secret store, the same convention already governing `Jwt:Key`.

## Security boundaries (verified)

- Never a client-supplied role, email, or user ID trusted as authorization — `CompleteExternalLogin.Command`
  carries only `(Provider, ProviderKey, Email, EmailVerified)`, all derived server-side from the
  authenticated external principal, never from a query string or request body the browser controls.
- The JWT/access token is never placed in a redirect URL — `IssueSessionAndRedirect` builds the
  final `Location` header from only `frontendBaseUrl` + the sanitized `returnTo`; the token reaches
  the browser exclusively via `AuthCookie.Issue`'s `Set-Cookie` header, exactly like `Register`/
  `Login`/`BecomeTutor` already do. Verified by `ExternalLoginCallbackEndpointTests.GoogleCallback_NeverSetsTheSessionCookie_WhenTheExternalLoginFails`
  and by direct code inspection of the one method that builds that redirect.
- The temporary `ExternalLogin` cookie (holding the external principal only between the provider
  redirect and this app's own callback) is `HttpOnly`, `Secure`, `SameSite=Lax`, 10-minute expiry,
  and explicitly signed out of immediately after the callback reads it — never the session cookie.
- CSRF/state validation during the OAuth handshake itself is entirely framework-owned
  (`AddGoogle`/`AddFacebook`'s built-in correlation cookie) — no OAuth protocol logic is hand-rolled.

## Testing evidence

54/54 tests pass in `TutorFlow.Modules.Identity.Tests` (full project, not a filtered subset),
including all pre-existing tests unaffected by this change:
- `ExternalLoginReturnToTests` (6): the sanitizer, exhaustively.
- `CompleteExternalLoginTests` (9): new Google/Facebook account creation (Student role only),
  existing-login reuse, email-conflict rejection (no auto-link), missing/unverified-email
  rejection, and the concurrent-duplicate-request race (two real DI scopes).
- `ExternalLoginCallbackEndpointTests` (5): HTTP-level callback cancellation (the real code path a
  user cancelling on the provider's consent screen, or navigating to the callback URL directly,
  actually produces — not a simulation), no session cookie set on failure, challenge routes exist
  and never echo an attacker-supplied `returnTo` host.
- `IdentityEndpointsTests` (19): confirmed still passing after the conditional-registration fix —
  this is the regression this ADR's own §4 fix resolved.

Full-solution `dotnet build` and `dotnet test` results are reported separately in this task's
delivery report, not duplicated here.

## Consequences

- No new database table, no migration.
- No change to `JwtBearerOptions`, `TokenValidationParameters`, or any existing authorization
  policy (`StudentOnly`/`TutorOnly`/`AdminOnly` unchanged).
- `Program.cs` and `IdentityModule.cs` grow new, clearly-scoped sections; no existing endpoint's
  behavior changes.
- `ADR-0023`'s "Open follow-ups" §"Social/OIDC login" is superseded by this ADR for Google/Facebook
  specifically — `ADR-0023` itself is unedited; this paragraph is the cross-reference.

## Unresolved — requires product/architecture approval before being built

1. **Account linking.** An existing password account whose email matches a verified provider
   identity is currently rejected outright (`EmailConflict`), not offered a secure "log in, then
   link" flow — intentionally deferred this phase (approved decision), but the actual linking UX/
   endpoint is unbuilt and unspecified.
2. **Signup UI.** No Google/Facebook buttons exist on `SignupForm.tsx` yet — this phase is
   backend-only, per the explicit scope of the task that produced it.
3. **Facebook's verification-gate assumption.** "Facebook never returns an unverified email" is
   documented platform behavior, not verified against a live Facebook app (no real credentials
   exist in this environment). Should be sanity-checked against Meta's current developer docs, or
   against a real sandbox app, before this ships to production traffic.
4. **Provider-console setup.** Real Google Cloud OAuth Client ID/Secret and a real Meta for
   Developers app (with Facebook Login enabled, `email` permission — sandbox works without Meta's
   app-review step, production email access requires it) remain the project owner's own
   responsibility; this implementation cannot create them.

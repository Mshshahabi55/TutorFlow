# ADR-0002: Identity Provider Strategy

**Status:** Accepted

## Context

JWT bearer authentication is already wired at the Host level (`TutorFlow.Host/Program.cs`). What wasn't decided was *who issues the tokens and owns credentials*: a self-rolled `Identity` module (ASP.NET Core Identity under the hood) vs. an external IdP (Auth0, Clerk, Azure AD B2C, etc.).

## Decision

1. **ASP.NET Core Identity** owns account/user lifecycle (credentials, password hashing, email verification, account recovery) inside the `Identity` module. No custom-rolled authentication system.
2. **JWT bearer authentication** remains the mechanism the application API authenticates against (already wired at Host level).
3. The authentication boundary is designed so an external OIDC/IdP can be introduced later **without redesigning the domain**: other modules and the API surface depend only on a `UserId` + a set of role/capability claims carried in the JWT — never on ASP.NET Identity types directly. Swapping the token-issuing mechanism later (e.g. federating to an external IdP) means replacing what populates those claims, not redesigning how every other module consumes identity.
4. **Users may hold multiple roles simultaneously.** Role is a *set* on the `User` (e.g. a user can carry both `Student` and `Tutor` grants at once), not a single mutually-exclusive account-type field. See the **Dual Role Accounts** decision below.

## Product Decision — Dual Role Accounts

A single account **may** have both a Student role and a Tutor role. The roles are independent:

- The domain must not assume a user is exclusively a student or exclusively a tutor.
- Authorization is **role/capability-based**, not based on a mutually exclusive account-type discriminator.
- `RoleGrant` is modeled as a collection per `User`, not a single enum/FK.

This resolves Open Question #1 and Architect question A1 (see `docs/OPEN_QUESTIONS.md`).

## Rationale

- Avoids a vendor dependency and added integration surface before the product has traction.
- A self-rolled Identity module gives full control over the dual-role model required by the product decision above — some external IdPs model "account type" rigidly and would complicate a clean set-based role grant.
- JWT claims as the boundary (rather than leaking ASP.NET Identity types to other modules) keeps the door open to an external IdP later.

## Consequences

- `Identity` module owns password security, email verification, 2FA, and account-recovery flows directly — this is now in scope for the module's first feature, not deferred.
- `RoleGrant` must be designed as a collection (a user can hold `{Student}`, `{Tutor}`, or `{Student, Tutor}` simultaneously), not a single `AccountType` discriminator column.
- Authorization policies elsewhere (`docs/product/ROLES_AND_PERMISSIONS.md`) must be written as capability/policy checks against the claims set, never as "if AccountType == Student" branching.
- A future move to an external OIDC provider is an `Identity`-module-internal change (how claims get populated) as long as other modules keep consuming only `UserId` + role/capability claims.
- Admin sub-roles (Open Question #2) are a separate, still-deferred decision and are not resolved by this ADR.

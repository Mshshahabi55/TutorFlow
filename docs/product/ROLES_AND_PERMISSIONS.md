# Roles & Permissions

## Roles (proposed)

| Role | Granted via | Notes |
|---|---|---|
| `Guest` | No account / unauthenticated | Can browse public site, search tutors, view tutor profiles, cannot message/book/review |
| `Student` | Signup | Default role for anyone who signs up without applying as a tutor |
| `Tutor` | Tutor application → verified → published | Gains Tutor app access progressively through onboarding (see `TUTOR_JOURNEY.md`) — an unverified/unpublished tutor has a restricted subset of the Tutor app (onboarding wizard + submitted-profile view only) |
| `Admin` | Internal provisioning only, never self-service signup | **Implemented (ADR-0028)** as a single flat role — real seeded `IdentityRole`, `AdminOnly` policy, currently used by `Tutors`' admin review pipeline. No endpoint grants it to any account; still likely needs sub-roles (see below) |

**`[OPEN — Product Owner]`**: Does Admin need sub-roles (e.g. `SupportAgent` who can view/respond to disputes but not issue refunds; `ContentModerator` who can suspend tutors but not touch payments; `SuperAdmin` who can do everything including feature flags)? The brief lists a wide admin surface (refunds, disputes, payouts, feature flags, system settings) that in most marketplaces is deliberately *not* given to every admin user. Until this is answered, `Admin` is modeled as a single flat role, which is almost certainly wrong for a production marketplace handling money.

**Resolved** (`ADR-0002`): one person can hold both `Student` and `Tutor` simultaneously (see `PRODUCT_VISION.md` persona note). Role is a **set** on the `User` (independent `RoleGrant`s), not a singular account-type property. Authorization must be written as role/capability checks against that set — never as "if the account is a Student account" branching that assumes mutual exclusivity.

## Permission boundaries by surface (proposed, high-level)

| Capability | Guest | Student | Tutor | Admin |
|---|---|---|---|---|
| Browse/search tutors | ✅ | ✅ | ✅ | ✅ |
| View tutor profile | ✅ | ✅ | ✅ (own + others) | ✅ |
| Message a tutor | ❌ | ✅ | ✅ (reply) | ✅ (support context only — `[OPEN]` scope) |
| Book trial/lesson | ❌ | ✅ | ❌ (cannot book self) | ❌ |
| Manage own availability | — | — | ✅ (own only) | ❌ |
| Edit own tutor profile | — | — | ✅ (own only, fields TBD — some may be admin-locked post-verification, e.g. identity fields) | ✅ (any, for moderation) |
| View own earnings/payouts | — | — | ✅ (own only) | ✅ (any, for support/finance) |
| Issue refund | ❌ | ❌ | ❌ | ✅ (sub-role? — see open question above) |
| Verify/suspend tutor | ❌ | ❌ | ❌ | ✅ |
| Manage taxonomy (subjects/languages/specialties) | ❌ | ❌ | ❌ | ✅ |
| Feature flags / system settings | ❌ | ❌ | ❌ | ✅ (likely `SuperAdmin`-only — `[OPEN]`) |

This table is a starting point for the Architect to formalize as authorization policies (per `dotnet-claude-kit` `authentication` skill guidance: policy-based, not role-string checks) once Identity is scaffolded. It is **not** an authorization specification ready for implementation — field-level edit permissions on the tutor profile in particular need Product Owner confirmation (e.g., can a tutor change their stated years of experience after verification without re-review?).

## Module ownership of identity data

Per `docs/domain/DOMAIN_MODEL.md`, `Identity` owns authentication/authorization (credentials, roles, sessions, JWT issuance) via ASP.NET Core Identity + JWT bearer (`ADR-0002`). `Students` and `Tutors` own the respective *profile* data for a given `UserId` — they do not duplicate credential data. Admin is not a separate bounded context (`ADR-0008`, Accepted) — admin capabilities are exposed by each owning module's own contract, not by a god "Admin" module reaching into everyone else's data.

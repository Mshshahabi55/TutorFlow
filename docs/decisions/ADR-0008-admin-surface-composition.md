# ADR-0008: Admin Surface Composition

**Status:** Accepted

## Context

Admin is a wide product surface (`docs/product/ADMIN_JOURNEY.md`) but not one of the 14 bounded contexts.

## Decision

**Composed Admin (option 1) is adopted.** No `Admin` business bounded context is created.

- Each owning business module exposes its own admin-facing contracts/read operations (e.g. `Tutors` exposes a verify/suspend endpoint, `Payments` exposes refund/dispute/payout endpoints).
- The admin application composes these capabilities into a unified back-office UI — a BFF/aggregation layer with no business logic of its own.
- This preserves the 14 bounded contexts and the no-direct-cross-module-reach-in rule applied everywhere else (`ADR-0001`).

## Rejected option

**Dedicated Admin module (15th bounded context)** that reads/writes across other modules' data directly — rejected because it would require exactly the kind of cross-module reach-in the architecture forbids everywhere else, reintroducing the "god module" problem.

## Consequences

- `Promotions`, `Support`-as-ticketing, `Reports`, `Feature Flags`, and `Audit Logs` still don't map onto any existing module — these remain explicitly unresolved and are **not** assigned to a catch-all Admin module as a side effect of this decision. They stay tracked in `docs/OPEN_QUESTIONS.md` / `docs/domain/DOMAIN_MODEL.md` as genuine gaps, each needing its own narrowly-scoped resolution later.
- Admin tooling for a given business module should land after that module's core workflow exists, not before (e.g. no `Payments` admin refund screen before `Payments` has any payments).
- Resolves Architect question A8 (`docs/OPEN_QUESTIONS.md`).
- A centralized Audit Logs subscriber module (A9) remains a non-binding recommendation only, not decided by this ADR.

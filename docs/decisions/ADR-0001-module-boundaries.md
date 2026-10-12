# ADR-0001: Module Boundaries & Cross-Module Communication

**Status:** Accepted (already reflected in the initialized codebase and `CLAUDE.md`)

## Context

TutorFlow is built as a .NET 10 modular monolith with 14 fixed bounded contexts: Identity, Students, Tutors, Subjects, Search, Availability, Bookings, Lessons, Subscriptions, Payments, Messaging, Reviews, Notifications, Learning.

## Decision

- Each module is its own project with its own `DbContext` and its own PostgreSQL schema.
- Modules never reference each other's projects, DbContexts, or internal types directly.
- Cross-module communication happens exclusively through integration events defined in `TutorFlow.Shared`.
- `TutorFlow.Shared` contains only contracts and cross-cutting primitives (e.g. `IIntegrationEvent`, and anticipated `Money`/timezone-pairing primitives — see `docs/domain/DOMAIN_MODEL.md`), never business logic.
- No microservices; this is a single deployable with an explicit evolution path to extract a module later if independent scaling/ownership demands it.

## Consequences

- Every new feature's first design question is "which module owns this," not "where's convenient to put this." The gaps identified in `docs/domain/DOMAIN_MODEL.md` (Classroom, Admin, Promotions, Support, Reports, Feature Flags, Audit Logs) exist precisely because this discipline is being applied rather than defaulting to a catch-all module.
- Cross-module read models (e.g. `Search` needing `Tutors` + `Subjects` data) must be built as denormalized projections fed by events, not live cross-module queries.

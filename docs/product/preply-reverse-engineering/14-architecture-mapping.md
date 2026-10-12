# Architecture Mapping

Documentation only — no architecture change is made or proposed as "decided" by this file. This maps *product* capabilities discovered in this directory onto the *already-approved* .NET Claude Kit modular-monolith architecture, per the permanent governance rule that the Kit is the engineering benchmark and competing patterns are never introduced.

## Approved architecture (unchanged, restated for this directory's own completeness)

.NET 10 / C# 14, ASP.NET Core Minimal APIs, EF Core + PostgreSQL (one DbContext/schema per module), ASP.NET Identity, Stripe, OpenTelemetry, Serilog (not yet wired), Wolverine (integration events + transactional outbox), Next.js/TypeScript/React/React Query, xUnit v3 + Testcontainers + WebApplicationFactory. Modules: Identity, Students, Tutors, Subjects, Search, Availability, Bookings, Lessons, Subscriptions, Payments, Messaging, Reviews, Notifications, Learning. Strict module boundaries — zero inter-module `ProjectReference`, communication only via `TutorFlow.Shared` integration events or narrow read-only query contracts.

## Mapping newly-discovered or newly-confirmed capabilities to this architecture

| Capability | Module | Mechanism | New abstraction needed? |
|---|---|---|---|
| Lesson auto-confirmation (15min/72h) | **Lessons** | A time-driven transition on the existing `Lesson` entity — mirrors Bookings' own "lazy sweep" convention (check-on-access rather than a new background-worker pattern) | No — reuses an existing pattern |
| Tutor earnings ledger | **Payments** | A new entity tracking accrued balance, keyed by `TutorUserId`, written on `PaymentSucceeded` consumption | No — same module, same event-driven convention |
| Tutor payout/withdrawal | **Payments** | Stripe Connect integration — explicitly blocked on the existing, unresolved commission/take-rate Product Owner decision (`ADR-0018`, `ADR-0027`) | Blocked, not a new pattern once unblocked |
| Tutor Insights dashboard | Frontend-only (BFF composition) | New pages reading existing data across Bookings/Payments/Tutors — no new backend aggregator, per the precedent already set for the Classroom UI composition pattern | No |
| Block/report | **Students** | New entity + endpoints, mirrors the existing `FavoriteTutor` pattern exactly | No |
| Profile visibility tiering (voluntary hide / auto-hide / suspend) | **Tutors** | Extends `TutorApplication` with a `VisibilityStatus` dimension distinct from its existing `Status` | No — additive field, same aggregate |
| Super/Professional Tutor criteria computation | **Tutors** | A computed/scheduled read over existing booking/review/response data once those modules exist | No |
| Profile Score | **Tutors** | A computed field over existing profile completeness data | No |
| Classroom | New real-time capability, likely **Lessons**-adjacent per the project's own prior architecture decision that Classroom's live-session lifecycle belongs to Lessons, with durable materials/homework split into Learning | A new external-provider abstraction (video) is required — this is a genuinely new category of infrastructure, analogous to how Stripe was the one new infrastructure Payments needed. Requires a Product Owner decision on the provider (managed vs. self-hosted vs. third-party embed) before any code — see `16-product-decisions-and-open-questions.md` |
| Subscriptions economy | **Subscriptions** | A credit-ledger pattern (immutable grant/consumption/expiry entries, balance derived not stored) — already specified in the pre-existing `docs/product/SUBSCRIPTION_JOURNEY.md`, now with real cycle/rollover numbers to implement against | No new pattern — the ledger shape was already correctly anticipated |
| Taxes & Fees presentation | **Bookings**/**Payments** (pricing) | Pure additive field on the existing `Money`-shaped pricing snapshot, once a Product Owner decision defines the computation | No new pattern, blocked on product decision |

## What this mapping does not introduce

No new module. No new cross-module communication mechanism beyond the existing integration-event/query-contract conventions. No competing Result pattern, endpoint convention, or persistence abstraction. The one genuinely new piece of infrastructure identified (a video/Classroom provider) is architecturally analogous to Stripe's own introduction for Payments — a single new external-provider abstraction, isolated behind a narrow internal interface, not a second competing architecture.

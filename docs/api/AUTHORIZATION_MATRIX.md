**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-002-domain-boundaries.md`, `docs/adr/ADR-003-authentication-and-authorization.md` (including all three Addenda), `docs/adr/ADR-010-api-boundary.md`, `docs/api/API_SPECIFICATION.md`. This document restates ADR-003's Permission Model and its three Addenda in exhaustive, endpoint-by-endpoint form against the concrete HTTP surface implemented in `backend/src/Web/Endpoints/` — it decides no new business rule and invents no permission beyond what ADR-003 (as corrected and extended by its Second and Third Addenda) already establishes. Where ADR-003 does not yet answer a row, that row is marked **Open** rather than filled with an assumption, consistent with `PROJECT_CONSTITUTION.md`: Core Principle 2 ("no assumptions").

**Status:** Accepted — 2026-07-21, ratified as a governance item of Priority 2, WP3's Final Gate verification, before Priority 2 WP4 (Endpoint Authorization) implementation begins.

---

# 1. Purpose

This is the single reference WP4 uses to decide, endpoint by endpoint, what `RequirePermission(...)` call (if any) to attach and what fine-grained check (if any) the owning bounded context's Application-layer handler must additionally enforce. It exists so that WP4 implements exactly what has been decided — never more, never less, never invented at implementation time.

# 2. How to Read This Matrix

- **Auth Required** — whether an unauthenticated caller may ever reach this endpoint's data or action.
- **Coarse-Grained Permission** — the `Permission` enum value (`Application/Authorization/Permission.cs`) `AuthorizationMiddleware` (WP3) can check via `RequirePermission(...)`. `None` means the endpoint is intentionally public; `N/A (fully fine-grained)` means no role-only check is meaningful — every caller of that role may or may not be permitted depending on resource state.
- **Fine-Grained Check Required** — whether a resource-instance-specific rule must additionally be enforced in the owning bounded context's Application-layer handler (never in Presentation or Infrastructure, per ADR-003's Risks section). "No" means the coarse-grained check alone is sufficient.
- **Source** — the ADR-003 Permission Model row, Addendum Decision, or Second Addendum Decision/Correction that governs this row. **Open** rows cite the closest related principle but are not resolved by any of them.
- **WP3 Status** — originally tracked WP4's endpoint-by-endpoint rollout of `RequirePermission(...)` against this document's 2026-07-21 snapshot, when zero endpoints were protected. **Updated 2026-07-29** to reflect current implementation state: WP4 is complete — every row below has been re-verified directly against `backend/src/Web/Endpoints/*.cs` (grep for `RequirePermission`/absence thereof plus a read of the owning Application handler for fine-grained-only rows). "Protected" means the coarse-grained `RequirePermission(...)` call shown is present in code (or, for `N/A (fully fine-grained)` rows, the fine-grained check is present in the owning handler with deliberately no coarse-grained gate). The column name is kept for continuity with the row-by-row history below rather than renamed.

# 3. Permission Catalog Reference

The complete, current `Role → Permission` grant (`Application/Authorization/RolePermissionCatalog.cs`, as corrected by ADR-003's Second Addendum, Correction 1):

| Permission | Student | Tutor | Parent/Guardian | Admin/Staff | Source |
|---|:---:|:---:|:---:|:---:|---|
| `BookSession` | ✓ | | ✓ | | IDR-5, IDR-6, SCH-5 |
| `CancelSession` | ✓ | ✓ | ✓ | ✓ | SCH-7 |
| `RescheduleSession` | ✓ | ✓ | ✓ | ✓ | SCH-7 |
| `CompleteSession` | | ✓ | | ✓ | Addendum Decision 1 |
| `MarkSessionNoShow` | | ✓ | | ✓ | Addendum Decision 1 |
| `InviteRelationship` | ✓ | | ✓ | | IDR-4; Second Addendum Correction 1 |
| `ConfirmRelationship` | ✓ | | ✓ | | IDR-4; Second Addendum Correction 1 |
| `ManageTutorOffering` | | ✓ | | | SCH-3 |
| `DeclareAvailability` | | ✓ | | | SCH-3, SCH-5 |
| `ApproveTutor` | | | | ✓ | ADM-1 |
| `SuspendTutor` | | | | ✓ | ADM-2 |
| `ViewAllSchedules` | | | | ✓ | ADM-3 |
| `ResolveBookingConflict` | | | | ✓ | ADM-4 |
| `ManageUserAccounts` | | | | ✓ | ADM-5 |
| `ViewAuditEntries` | | | | ✓ | Second Addendum Decision 6 |
| `UseMessaging` | ✓ | ✓ | ✓ | ✓ | `ADR-022` |
| `ManageMeetings` | | ✓ | | | `ADR-023` |

Every coarse-grained cell above still requires whatever fine-grained check Section 4 lists for the specific endpoint it gates — this table states role eligibility only, never resource-instance eligibility.

# 4. Endpoint Matrix

## 4.1 Identity & Relationship

| Endpoint | Method | Auth Required | Coarse-Grained Permission | Fine-Grained Check Required | Source | WP3 Status |
|---|---|---|---|---|---|---|
| `/tutors` | POST | No | `None` | No | IDR-1 (self-registration) | Protected (correct — intentionally Open) |
| `/tutors/{id}/approve` | POST | Yes | `ApproveTutor` | No | ADM-1 | Protected |
| `/tutors/{id}/suspend` | POST | Yes | `SuspendTutor` | No | ADM-2 | Protected |
| `/tutors/{id}/hourly-rate` | PATCH | Yes | `ManageTutorOffering` | **Yes** — caller must be the Tutor named by `{id}` | SCH-3; ARCHITECTURE.md §17 item 3 (resource ownership) | Protected |
| `/tutors/{id}/subject` | PATCH | Yes | `ManageTutorOffering` | **Yes** — same | SCH-3; ARCHITECTURE.md §17 item 3 | Protected |
| `/tutors/{id}/language` | PATCH | Yes | `ManageTutorOffering` | **Yes** — same | SCH-3; ARCHITECTURE.md §17 item 3 | Protected |
| `/tutors/{id}/location` | PATCH | Yes | `ManageTutorOffering` | **Yes** — same | SCH-3; ARCHITECTURE.md §17 item 3 | Protected |
| `/tutors/{id}/offered-durations` | PATCH | Yes | `ManageTutorOffering` | **Yes** — same | SCH-3; ARCHITECTURE.md §17 item 3 | Protected |
| `/tutors/{id}/personal-info` | PATCH | Yes | `ManageTutorOffering` | **Yes** — same | ADR-024; ARCHITECTURE.md §17 item 3 | Protected |
| `/tutors/{id}/teaching-info` | PATCH | Yes | `ManageTutorOffering` | **Yes** — same | ADR-024; ARCHITECTURE.md §17 item 3 | Protected |
| `/tutors/{id}/media` | PATCH | Yes | `ManageTutorOffering` | **Yes** — same | ADR-024; ARCHITECTURE.md §17 item 3 | Protected |
| `/tutors/{id}/pricing` | PATCH | Yes | `ManageTutorOffering` | **Yes** — same | ADR-024; ARCHITECTURE.md §17 item 3 | Protected |
| `/tutors/{id}/submit` | POST | Yes | `ManageTutorOffering` | **Yes** — same | ADR-024; ARCHITECTURE.md §17 item 3 | Protected |
| `/students` | POST | No | `None` | No | IDR-1 (self-registration) | Protected (correct — intentionally Open) |
| `/parent-guardians` | POST | No | `None` | No | IDR-1 (self-registration) | Protected (correct — intentionally Open) |
| `/relationships` | POST | Yes | `InviteRelationship` | **Yes** — caller must be a party to the Relationship being created | IDR-4; Second Addendum Correction 1 | Protected |
| `/relationships/{id}/confirm` | POST | Yes | `ConfirmRelationship` | **Yes** — caller must be the *other* party to the specific invitation named by `{id}` (not the party who invited) | IDR-4; Second Addendum Correction 1 | Protected |
| `/tutors/pending` | GET | Yes | `ApproveTutor` | No | Third Addendum Decision 8 | Protected |
| `/tutors/{id}` | GET | Conditional | `N/A (fully fine-grained)` | **Yes** — approved Tutor is Public; pending/suspended is Owner + Admin only | Addendum Decision 2 | Protected (fine-grained only, by design) |
| `/tutors` | GET | No | `None` | No — query itself is scoped to discoverable Tutors only | Cross-Context Authorization Rules (Discovery is Public) | Protected (correct — intentionally Open) |
| `/students/{id}` | GET | Conditional | `N/A (fully fine-grained)` | **Yes** — Owner + confirmed-Relationship counterpart + Admin | Addendum Decision 3 | Protected (fine-grained only, by design) |
| `/parent-guardians/{id}` | GET | Conditional | `N/A (fully fine-grained)` | **Yes** — Owner + confirmed-Relationship counterpart + Admin | Addendum Decision 3 | Protected (fine-grained only, by design) |
| `/relationships/{id}` | GET | Yes | `N/A (fully fine-grained)` | **Yes** — either named party (`ParentGuardianId` or `StudentId`), regardless of Invited/Confirmed status, or Admin | Third Addendum Decision 9 | Protected (fine-grained only, by design) |
| `/accounts/{id}/relationships` | GET | Yes | `N/A (fully fine-grained)` | **Yes** — the named account itself, or Admin | Third Addendum Decision 10 | Protected (fine-grained only, by design) |

## 4.2 Authentication

| Endpoint | Method | Auth Required | Coarse-Grained Permission | Fine-Grained Check Required | Source | WP3 Status |
|---|---|---|---|---|---|---|
| `/auth/login` | POST | No | `None` | No | Must be reachable to authenticate at all | Protected (correct — intentionally Open) |
| `/auth/logout` | POST | No | `None` | No | Token-based, not identity-based | Protected (correct — intentionally Open) |
| `/auth/accounts/{id}/reset-password` | POST | Yes | `ManageUserAccounts` | No | ADM-5; `ADR-017` (Admin-assisted reset only) | Protected — verified `.RequirePermission(Permission.ManageUserAccounts)` present (`AuthEndpoints.cs`); the "live account-takeover exposure" language above described the pre-WP4 2026-07-21 snapshot, not current code |

## 4.3 Scheduling & Booking

| Endpoint | Method | Auth Required | Coarse-Grained Permission | Fine-Grained Check Required | Source | WP3 Status |
|---|---|---|---|---|---|---|
| `/availability-slots` | POST | Yes | `DeclareAvailability` | **Yes** — `TutorId` in the request body must equal the caller's own Tutor identity | SCH-3, SCH-5; ARCHITECTURE.md §17 item 3 | Protected |
| `/sessions` | POST | Yes | `BookSession` | **Yes** — if the Student is a minor, a confirmed Parent/Guardian Relationship must exist (IDR-6) | SCH-5; IDR-5, IDR-6 | Protected |
| `/sessions/{id}/reschedule` | POST | Yes | `RescheduleSession` | **Yes** — caller must be a party to that specific Session (Tutor, Student, or booking Parent/Guardian) or Admin | SCH-7 | Protected |
| `/sessions/{id}/cancel` | POST | Yes | `CancelSession` | **Yes** — same as reschedule | SCH-7 | Protected |
| `/sessions/{id}/complete` | POST | Yes | `CompleteSession` | **Yes** — caller must be the Tutor assigned to that specific Session, or Admin | Addendum Decision 1 | Protected |
| `/sessions/{id}/no-show` | POST | Yes | `MarkSessionNoShow` | **Yes** — same as complete | Addendum Decision 1 | Protected |
| `/availability-slots/{id}` | GET | Conditional | `N/A (fully fine-grained)` | **Yes** — declaring Tutor + booked Student + managing Parent/Guardian + Admin only | Addendum Decision 4 | Protected (fine-grained only, by design) |
| `/sessions/{id}` | GET | Conditional | `N/A (fully fine-grained)` | **Yes** — same parties as Decision 4 | Second Addendum Decision 7 | Protected (fine-grained only, by design) |
| `/students/{id}/schedule` | GET | Yes | `N/A (fully fine-grained)` | **Yes** — the named Student, a Parent/Guardian with a confirmed Relationship to that Student, or Admin | Third Addendum Decision 11 | Protected (fine-grained only, by design) |
| `/tutors/{id}/schedule` | GET | Yes | `N/A (fully fine-grained)` | **Yes** — the named Tutor only, or Admin — no other authenticated user, per CONST-5 ("Tutor cannot see another Tutor's schedule") | Third Addendum Decision 12 | Protected (fine-grained only, by design) |
| `/tutors/{id}/availability-slots` | GET | Yes | `N/A (fully fine-grained)` | **Yes** — any authenticated caller when the Tutor is discoverable; the named Tutor or Admin only when not discoverable | Third Addendum Decision 13 | Protected (fine-grained only, by design) |

## 4.4 Discovery

| Endpoint | Method | Auth Required | Coarse-Grained Permission | Fine-Grained Check Required | Source | WP3 Status |
|---|---|---|---|---|---|---|
| `/tutors/search` | GET | No | `None` | No — query itself is scoped to discoverable Tutors only | Cross-Context Authorization Rules (Discovery is Public); DISC-1 | Protected (correct — intentionally Open) |

## 4.5 Marketplace Oversight

| Endpoint | Method | Auth Required | Coarse-Grained Permission | Fine-Grained Check Required | Source | WP3 Status |
|---|---|---|---|---|---|---|
| `/sessions` | GET | Yes | `ViewAllSchedules` | No — deliberately unscoped, "all schedules" (ADM-3) | ADM-3 | Protected |
| `/sessions/status-counts` | GET | Yes | `ViewAllSchedules` | No — same audience/scope as `GET /sessions` above, aggregated instead of listed | ADM-3 | Protected |

## 4.6 Audit

| Endpoint | Method | Auth Required | Coarse-Grained Permission | Fine-Grained Check Required | Source | WP3 Status |
|---|---|---|---|---|---|---|
| `/audit-entries` | GET | Yes | `ViewAuditEntries` | No — deliberately unscoped, Admin/Staff read all entries | Second Addendum Decision 6 | Protected |

WP4 resolved the footnote this row originally carried by adding a dedicated `Permission.ViewAuditEntries` enum value (`Application/Authorization/Permission.cs`), Admin/Staff-only (`RolePermissionCatalog.cs`) — not `ManageUserAccounts`, avoiding the capability-conflation this row's Source decision anticipated. `AuditEndpoints.cs` uses it directly.

## 4.7 Platform

| Endpoint | Method | Auth Required | Coarse-Grained Permission | Fine-Grained Check Required | Source | WP3 Status |
|---|---|---|---|---|---|---|
| `/health` | GET | No | `None` | No | Addendum Decision 5 | Unprotected (correct, verified) |

## 4.8 Communication

New bounded context, RC5.1. `Permission.UseMessaging` is granted to all four roles (`RolePermissionCatalog`) — coarse-grained protection alone never permits or denies anything by itself here; every row below additionally requires the fine-grained, resource-instance check its own Application-layer handler enforces, per `docs/adr/ADR-022-communication-and-notifications-architecture.md`.

| Endpoint | Method | Auth Required | Coarse-Grained Permission | Fine-Grained Check Required | Source | WP3 Status |
|---|---|---|---|---|---|---|
| `/conversations` | POST | Yes | `UseMessaging` | **Yes** — a genuinely new Conversation may only be started Student/Parent-Guardian → Tutor, or Admin/Staff → anyone; a Tutor may never unilaterally start one. Re-requesting an existing pair returns that Conversation instead (idempotent) | ADR-022 | Protected |
| `/conversations/mine` | GET | Yes | `UseMessaging` | No — inherently scoped to the caller's own Conversations | ADR-022 | Protected |
| `/conversations/{id}/messages` | GET | Yes | `UseMessaging` | **Yes** — caller must be one of the two participants on that specific Conversation | ADR-022 | Protected |
| `/conversations/{id}/messages` | POST | Yes | `UseMessaging` | **Yes** — same | ADR-022 | Protected |
| `/conversations/{id}/read` | POST | Yes | `UseMessaging` | **Yes** — same | ADR-022 | Protected |
| `/notifications/mine` | GET | Yes | `UseMessaging` | No — inherently scoped to the caller as recipient | ADR-022 | Protected |
| `/notifications/{id}/read` | POST | Yes | `UseMessaging` | **Yes** — caller must be the Notification's own recipient | ADR-022 | Protected |
| `/notifications/mark-all-read` | POST | Yes | `UseMessaging` | No — inherently scoped to the caller as recipient | ADR-022 | Protected |

## 4.9 Meetings

New bounded context, RC5.3 (`docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md`). `Permission.ManageMeetings` is Tutor-only (`RolePermissionCatalog`) — Student/Parent-Guardian/Admin-Staff never create or modify a Meeting, only read one (Admin: read-only, per that ADR's own instruction).

| Endpoint | Method | Auth Required | Coarse-Grained Permission | Fine-Grained Check Required | Source | WP3 Status |
|---|---|---|---|---|---|---|
| `/sessions/{id}/meeting` | POST | Yes | `ManageMeetings` | **Yes** — caller must be the named Session's own Tutor ("Start Lesson," idempotent — a repeat call returns the already-started Meeting) | ADR-023 | Protected |
| `/sessions/{id}/meeting` | GET | Conditional | `N/A (fully fine-grained)` | **Yes** — same parties as `GET /sessions/{id}` itself (Tutor, Student, booking Parent/Guardian, or Admin/Staff) | ADR-023 | Protected (deferred to handler by design, same shape as `GET /sessions/{id}`) |
| `/conversations/{id}/active-meeting` | GET | Yes | `UseMessaging` | **Yes** — caller must be one of the two participants on that specific Conversation | ADR-023 | Protected |

# 5. Summary

- **37 endpoints total** (36 application endpoints + `/health`).
- **14 rows fully resolved by an existing governing decision** (coarse- and fine-grained, where applicable) and ready for WP4 to implement directly: registration endpoints (public, 3), `/auth/login`, `/auth/logout`, `/auth/accounts/{id}/reset-password`, `/tutors/{id}/approve`, `/tutors/{id}/suspend`, `/tutors` (search list), `/tutors/search`, `/sessions` GET (Oversight), `/audit-entries`, `/health`, `/tutors/pending`.
- **23 rows resolved as fine-grained-by-design**, deferred to the owning bounded context's Application-layer handler per ADR-003's own architecture — WP4 attaches the coarse-grained `RequirePermission(...)` shown (where applicable) and the handler enforces the rest.
- **0 rows remain Open.** The six rows previously Open — `GET /tutors/pending`, `GET /relationships/{id}`, `GET /accounts/{id}/relationships`, `GET /students/{id}/schedule`, `GET /tutors/{id}/schedule`, `GET /tutors/{id}/availability-slots` — were each resolved by `ADR-003`'s Third Addendum, Decisions 8–13 (2026-07-21) (see individual row Source columns above) and are ready for WP4 implementation.
- **Zero endpoints were protected as of this document's original 2026-07-21 publication** — confirmed by solution-wide search for `RequirePermission` at the time (WP3 Final Gate report, Section 5). This matrix changed no runtime behavior on its own; it was the reference WP4 implemented against.
- **Updated 2026-07-29: WP4 is complete.** Every row in Sections 4.1–4.9 has been re-verified directly against `backend/src/Web/Endpoints/*.cs` and its owning Application handler — all 54 endpoints (see the RC5.1/RC5.3/Onboarding/Admin-KPI addenda below for the running total) are Protected as described. 0 rows remain Open, 0 rows remain unimplemented.

---

*Status: Accepted — 2026-07-21. Ratified as a Priority 2, WP3 Final Gate governance item. All 37 rows (this document's original scope) were resolved and ready for WP4 as written — the 6 rows previously Open were resolved by `ADR-003`'s Third Addendum, Decisions 8–13 (2026-07-21). **WP4 implementation itself is now complete as of 2026-07-29** — see the updated Summary above and the per-row "WP3 Status" column, both re-verified against current code, not just the original design intent.*

## Addendum — RC5.1, 2026-07-28

Section 4.8 (Communication) added: 8 new endpoints for the new Communication bounded context (`docs/adr/ADR-022-communication-and-notifications-architecture.md`), bringing the total to **45 endpoints** (44 application endpoints + `/health`). All 8 rows are resolved, non-Open, and implemented with `RequirePermission(Permission.UseMessaging)` plus fine-grained checks in the owning handlers — none of Section 5's original 2026-07-21 counts above are restated or corrected, since they describe that date's snapshot of Sections 4.1–4.7 only.

## Addendum — RC5.3, 2026-07-28

Section 4.9 (Meetings) added: 3 new endpoints for the new Meetings bounded context (`docs/adr/ADR-023-online-lesson-meeting-provider-architecture.md`), bringing the total to **48 endpoints** (47 application endpoints + `/health`). All 3 rows are resolved, non-Open — one Tutor-only `RequirePermission(Permission.ManageMeetings)` row, one fully-fine-grained row mirroring `GET /sessions/{id}`'s own shape, and one `RequirePermission(Permission.UseMessaging)` row for the Conversation-page linkage — with fine-grained checks in the owning handlers, same as every other row in this document.

## Addendum — Tutor Onboarding Wizard, 2026-07-28

Section 4.1 (Identity & Relationship) gained 5 new rows for `ADR-024-tutor-profile-enrichment-and-onboarding-wizard.md` (Accepted): `/tutors/{id}/personal-info`, `/tutors/{id}/teaching-info`, `/tutors/{id}/media`, `/tutors/{id}/pricing` (all PATCH), and `/tutors/{id}/submit` (POST), bringing the total to **53 endpoints** (52 application endpoints + `/health`). All 5 rows reuse the exact same `RequirePermission(Permission.ManageTutorOffering)` + resource-ownership fine-grained check the five existing Tutor-self-service PATCH rows above already use — no new `Permission` enum value was needed.

## Addendum — Admin Dashboard KPI, 2026-07-29

Section 4.5 (Marketplace Oversight) gained 1 new row: `GET /sessions/status-counts` (platform-wide Session counts grouped by status, for the Admin dashboard's status-breakdown chart), bringing the total to **54 endpoints** (53 application endpoints + `/health`). Reuses the exact same `RequirePermission(Permission.ViewAllSchedules)` coarse-grained gate and "deliberately unscoped" reasoning `GET /sessions` above already has — no new `Permission` enum value needed, no resource-instance owner for a platform-wide aggregate to check against.

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
- **WP3 Status** — always **Unprotected** for every row as of this document's publication, since WP3 built only the enforcement mechanism; zero endpoints call `RequirePermission(...)` yet (verified by solution-wide search — see the WP3 Final Gate report). This column exists so WP4 can be checked off row by row as protection is added.

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

Every coarse-grained cell above still requires whatever fine-grained check Section 4 lists for the specific endpoint it gates — this table states role eligibility only, never resource-instance eligibility.

# 4. Endpoint Matrix

## 4.1 Identity & Relationship

| Endpoint | Method | Auth Required | Coarse-Grained Permission | Fine-Grained Check Required | Source | WP3 Status |
|---|---|---|---|---|---|---|
| `/tutors` | POST | No | `None` | No | IDR-1 (self-registration) | Unprotected (correct) |
| `/tutors/{id}/approve` | POST | Yes | `ApproveTutor` | No | ADM-1 | Unprotected |
| `/tutors/{id}/suspend` | POST | Yes | `SuspendTutor` | No | ADM-2 | Unprotected |
| `/tutors/{id}/hourly-rate` | PATCH | Yes | `ManageTutorOffering` | **Yes** — caller must be the Tutor named by `{id}` | SCH-3; ARCHITECTURE.md §17 item 3 (resource ownership) | Unprotected |
| `/tutors/{id}/subject` | PATCH | Yes | `ManageTutorOffering` | **Yes** — same | SCH-3; ARCHITECTURE.md §17 item 3 | Unprotected |
| `/tutors/{id}/language` | PATCH | Yes | `ManageTutorOffering` | **Yes** — same | SCH-3; ARCHITECTURE.md §17 item 3 | Unprotected |
| `/tutors/{id}/location` | PATCH | Yes | `ManageTutorOffering` | **Yes** — same | SCH-3; ARCHITECTURE.md §17 item 3 | Unprotected |
| `/tutors/{id}/offered-durations` | PATCH | Yes | `ManageTutorOffering` | **Yes** — same | SCH-3; ARCHITECTURE.md §17 item 3 | Unprotected |
| `/students` | POST | No | `None` | No | IDR-1 (self-registration) | Unprotected (correct) |
| `/parent-guardians` | POST | No | `None` | No | IDR-1 (self-registration) | Unprotected (correct) |
| `/relationships` | POST | Yes | `InviteRelationship` | **Yes** — caller must be a party to the Relationship being created | IDR-4; Second Addendum Correction 1 | Unprotected |
| `/relationships/{id}/confirm` | POST | Yes | `ConfirmRelationship` | **Yes** — caller must be the *other* party to the specific invitation named by `{id}` (not the party who invited) | IDR-4; Second Addendum Correction 1 | Unprotected |
| `/tutors/pending` | GET | Yes | `ApproveTutor` | No | Third Addendum Decision 8 | Unprotected — **ready for WP4 implementation** |
| `/tutors/{id}` | GET | Conditional | `N/A (fully fine-grained)` | **Yes** — approved Tutor is Public; pending/suspended is Owner + Admin only | Addendum Decision 2 | Unprotected (deferred to handler by design) |
| `/tutors` | GET | No | `None` | No — query itself is scoped to discoverable Tutors only | Cross-Context Authorization Rules (Discovery is Public) | Unprotected (correct) |
| `/students/{id}` | GET | Conditional | `N/A (fully fine-grained)` | **Yes** — Owner + confirmed-Relationship counterpart + Admin | Addendum Decision 3 | Unprotected (deferred to handler by design) |
| `/parent-guardians/{id}` | GET | Conditional | `N/A (fully fine-grained)` | **Yes** — Owner + confirmed-Relationship counterpart + Admin | Addendum Decision 3 | Unprotected (deferred to handler by design) |
| `/relationships/{id}` | GET | Yes | `N/A (fully fine-grained)` | **Yes** — either named party (`ParentGuardianId` or `StudentId`), regardless of Invited/Confirmed status, or Admin | Third Addendum Decision 9 | Unprotected — **ready for WP4 implementation** |
| `/accounts/{id}/relationships` | GET | Yes | `N/A (fully fine-grained)` | **Yes** — the named account itself, or Admin | Third Addendum Decision 10 | Unprotected — **ready for WP4 implementation** |

## 4.2 Authentication

| Endpoint | Method | Auth Required | Coarse-Grained Permission | Fine-Grained Check Required | Source | WP3 Status |
|---|---|---|---|---|---|---|
| `/auth/login` | POST | No | `None` | No | Must be reachable to authenticate at all | Unprotected (correct) |
| `/auth/logout` | POST | No | `None` | No | Token-based, not identity-based | Unprotected (correct) |
| `/auth/accounts/{id}/reset-password` | POST | Yes | `ManageUserAccounts` | No | ADM-5; `ADR-017` (Admin-assisted reset only) | **Unprotected — live account-takeover exposure; highest-priority WP4 item** |

## 4.3 Scheduling & Booking

| Endpoint | Method | Auth Required | Coarse-Grained Permission | Fine-Grained Check Required | Source | WP3 Status |
|---|---|---|---|---|---|---|
| `/availability-slots` | POST | Yes | `DeclareAvailability` | **Yes** — `TutorId` in the request body must equal the caller's own Tutor identity | SCH-3, SCH-5; ARCHITECTURE.md §17 item 3 | Unprotected |
| `/sessions` | POST | Yes | `BookSession` | **Yes** — if the Student is a minor, a confirmed Parent/Guardian Relationship must exist (IDR-6) | SCH-5; IDR-5, IDR-6 | Unprotected |
| `/sessions/{id}/reschedule` | POST | Yes | `RescheduleSession` | **Yes** — caller must be a party to that specific Session (Tutor, Student, or booking Parent/Guardian) or Admin | SCH-7 | Unprotected |
| `/sessions/{id}/cancel` | POST | Yes | `CancelSession` | **Yes** — same as reschedule | SCH-7 | Unprotected |
| `/sessions/{id}/complete` | POST | Yes | `CompleteSession` | **Yes** — caller must be the Tutor assigned to that specific Session, or Admin | Addendum Decision 1 | Unprotected |
| `/sessions/{id}/no-show` | POST | Yes | `MarkSessionNoShow` | **Yes** — same as complete | Addendum Decision 1 | Unprotected |
| `/availability-slots/{id}` | GET | Conditional | `N/A (fully fine-grained)` | **Yes** — declaring Tutor + booked Student + managing Parent/Guardian + Admin only | Addendum Decision 4 | Unprotected (deferred to handler by design) |
| `/sessions/{id}` | GET | Conditional | `N/A (fully fine-grained)` | **Yes** — same parties as Decision 4 | Second Addendum Decision 7 | Unprotected (deferred to handler by design) |
| `/students/{id}/schedule` | GET | Yes | `N/A (fully fine-grained)` | **Yes** — the named Student, a Parent/Guardian with a confirmed Relationship to that Student, or Admin | Third Addendum Decision 11 | Unprotected — **ready for WP4 implementation** |
| `/tutors/{id}/schedule` | GET | Yes | `N/A (fully fine-grained)` | **Yes** — the named Tutor only, or Admin — no other authenticated user, per CONST-5 ("Tutor cannot see another Tutor's schedule") | Third Addendum Decision 12 | Unprotected — **ready for WP4 implementation** |
| `/tutors/{id}/availability-slots` | GET | Yes | `N/A (fully fine-grained)` | **Yes** — any authenticated caller when the Tutor is discoverable; the named Tutor or Admin only when not discoverable | Third Addendum Decision 13 | Unprotected — **ready for WP4 implementation** |

## 4.4 Discovery

| Endpoint | Method | Auth Required | Coarse-Grained Permission | Fine-Grained Check Required | Source | WP3 Status |
|---|---|---|---|---|---|---|
| `/tutors/search` | GET | No | `None` | No — query itself is scoped to discoverable Tutors only | Cross-Context Authorization Rules (Discovery is Public); DISC-1 | Unprotected (correct) |

## 4.5 Marketplace Oversight

| Endpoint | Method | Auth Required | Coarse-Grained Permission | Fine-Grained Check Required | Source | WP3 Status |
|---|---|---|---|---|---|---|
| `/sessions` | GET | Yes | `ViewAllSchedules` | No — deliberately unscoped, "all schedules" (ADM-3) | ADM-3 | Unprotected |

## 4.6 Audit

| Endpoint | Method | Auth Required | Coarse-Grained Permission | Fine-Grained Check Required | Source | WP3 Status |
|---|---|---|---|---|---|---|
| `/audit-entries` | GET | Yes | `ManageUserAccounts`\* | No — deliberately unscoped, Admin/Staff read all entries | Second Addendum Decision 6 | Unprotected |

\* No existing `Permission` enum value names "read the audit trail." `ManageUserAccounts` is the closest already-defined Admin/Staff permission, but reusing it here would conflate two distinct capabilities (managing accounts vs. reading audit history) under one name. **This matrix does not invent a new enum value** — adding a dedicated `Permission.ViewAuditTrail` (or equivalent) is a WP4 implementation detail, not a governance decision, since Decision 6 already settles *who* (Admin/Staff) and WP2's precedent (one enum value per distinct capability) already settles *how* it should be modeled once WP4 reaches this row.

## 4.7 Platform

| Endpoint | Method | Auth Required | Coarse-Grained Permission | Fine-Grained Check Required | Source | WP3 Status |
|---|---|---|---|---|---|---|
| `/health` | GET | No | `None` | No | Addendum Decision 5 | Unprotected (correct, verified) |

# 5. Summary

- **37 endpoints total** (36 application endpoints + `/health`).
- **14 rows fully resolved by an existing governing decision** (coarse- and fine-grained, where applicable) and ready for WP4 to implement directly: registration endpoints (public, 3), `/auth/login`, `/auth/logout`, `/auth/accounts/{id}/reset-password`, `/tutors/{id}/approve`, `/tutors/{id}/suspend`, `/tutors` (search list), `/tutors/search`, `/sessions` GET (Oversight), `/audit-entries`, `/health`, `/tutors/pending`.
- **23 rows resolved as fine-grained-by-design**, deferred to the owning bounded context's Application-layer handler per ADR-003's own architecture — WP4 attaches the coarse-grained `RequirePermission(...)` shown (where applicable) and the handler enforces the rest.
- **0 rows remain Open.** The six rows previously Open — `GET /tutors/pending`, `GET /relationships/{id}`, `GET /accounts/{id}/relationships`, `GET /students/{id}/schedule`, `GET /tutors/{id}/schedule`, `GET /tutors/{id}/availability-slots` — were each resolved by `ADR-003`'s Third Addendum, Decisions 8–13 (2026-07-21) (see individual row Source columns above) and are ready for WP4 implementation.
- **Zero endpoints are currently protected** — confirmed by solution-wide search for `RequirePermission` (WP3 Final Gate report, Section 5). This matrix changes no runtime behavior; it is the reference WP4 implements against.

---

*Status: Accepted — 2026-07-21. Ratified as a Priority 2, WP3 Final Gate governance item. All 37 rows are now resolved and ready for WP4 as written — the 6 rows previously Open were resolved by `ADR-003`'s Third Addendum, Decisions 8–13 (2026-07-21).*

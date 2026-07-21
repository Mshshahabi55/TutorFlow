**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001` through `ADR-016` (all Accepted, except `ADR-003`, which remains itself Proposed), `docs/adr/ADR-011-authentication-strategy.md` (the ADR this document resolves). This ADR makes the eight decisions `ADR-011` Section 4 identifies as required before any authentication code may be written. It chooses a specific mechanism; it does not reopen `ADR-003`'s separate, still-Proposed authorization model.

---

# ADR-017: Authentication Mechanism Decision

## Status

**Accepted — 2026-07-20.**

**Business decision, made explicitly by the Project Director through the Constitution's Decision-Making Process (Launch Preparation phase, Priority 1):**

| `ADR-011` Section | Decision |
|---|---|
| 5 — Credential mechanism | **Password**, hashed server-side. No external identity provider, no passwordless delivery channel. |
| 6 — Token/session mechanism | **Reference (opaque) token**, generated server-side, stored hashed in PostgreSQL (`ADR-013`, Accepted), looked up per request. Not a self-contained signed token; not a browser cookie. |
| 7 — Token lifetime | **Short-lived, sliding.** 30 minutes of inactivity before expiry; each authenticated request extends expiry by the same window. No absolute maximum session length is imposed beyond continued activity. |
| 8 — Refresh strategy | **None.** Sliding expiration (above) provides continuous renewal for an active session; no separate refresh-token artifact, endpoint, or second token type exists. |
| 9 — Signing/key management | **Not applicable.** The chosen token (Section 6) is an opaque, unsigned, server-side-looked-up value — there is no signing key to generate, store, rotate, or scope per environment. |
| 10 — Logout strategy | **Server-side token invalidation.** Logout deletes (or marks revoked) the presented token's row; the token is unusable for any subsequent request immediately. |
| 11 — Revocation | **Same mechanism as logout**, additionally invocable by an Admin/Staff action against another Account's active tokens (e.g., on suspension), consistent with the already-approved `ADM-2` suspension capability. |
| 4, Item 8 — Admin/Staff provisioning | **Manually seeded/provisioned only.** No public Admin/Staff self-registration endpoint exists at any point. An Admin/Staff Account is created by the business owner (initial seed) or by an already-authenticated Admin/Staff creating another — provisioning is always an act by someone already trusted. |

Two additional decisions, surfaced while completing the Database Logical Architecture Review this ADR's Section 5 decision requires, and answered through the same process:

| Decision | Answer |
|---|---|
| Login identifier field | **Email address.** Stored on the Account itself; not sent to the user at login time (no delivery channel is needed for a Password mechanism), so international email deliverability into the target launch market carries no login-path risk. Would matter only if a future self-service password-reset flow used it as a delivery channel — see Password Reset, below. |
| Email uniqueness scope | **Unique per role, not globally unique.** The same email may be used to create at most one Account per role (one Tutor Account, one Student Account, one Parent/Guardian Account, one Admin/Staff Account), but the same email may span more than one role — e.g., a Tutor who is also a Parent/Guardian booking for their own child. Each role's Account remains a fully independent aggregate (`DOMAIN_DATA_MODEL.md` Section 9); this does not merge them. |

**Password reset/recovery — explicitly out of scope for this decision and for RC1.** Not listed among Priority 1's deliverables. For this release, a user who forgets their password has no self-service recovery path; an Admin/Staff member resets the password hash directly, using the same manually-provisioned trust already established for that role. Self-service reset (email/SMS-based) is deferred as its own future decision, not silently missing — building it now would import exactly the delivery-channel, code/link-lifetime, and rate-limiting open questions `ADR-011` Section 5 already flags for passwordless authentication, just relocated into a reset flow instead of login.

**Failed-login auditing — decided.** `ADR-011` Open Question 10, and `ADR-003` Open Question 6's authentication-specific analogue, are both resolved: a failed login attempt (wrong password, or an email matching no Account) is recorded in the audit trail, alongside the already-audited successful mutating actions (`ADR-009`).

## Context

`ADR-011` (Proposed, Not Accepted) enumerated eight Structural decisions required before any authentication code could be written, and recorded why a prior implementation attempt ("Phase 16") that skipped this step was fully reverted. This ADR is the successor document `ADR-011`'s own Future Evolution section anticipates: each of those eight decisions, made explicitly by the Project Director rather than invented during implementation, recorded here before Backend Implementation begins.

## Decision Rationale

- **Password over passwordless/external IdP:** vendor-independent. The target launch market (Iran) carries material reliability risk for both international OAuth/OIDC identity providers and, to a lesser extent, cross-border SMS/email delivery — a risk that matters far less for Password, since no message needs to be delivered at the login step itself.
- **Reference/opaque token over signed token or cookie:** trivially revocable and auditable (`ADR-011` Section 6's own stated advantage), and its previously-cited blocker — needing an undecided persistence technology — no longer applies, since `ADR-013` (PostgreSQL) is Accepted. A signed, self-contained token would need its own revocation-list mechanism to achieve the same revocability, effectively reintroducing server-side state anyway.
- **Sliding expiration, no separate refresh token:** the reference-token mechanism makes renewal a trivial per-request expiry update, so a second token type/endpoint would add real complexity (`ADR-011` Section 8) without a corresponding security or UX benefit here.
- **No signing key management:** a direct, mechanical consequence of choosing an opaque token — Section 9 of `ADR-011` is not a decision this ADR makes; it is a section that no longer applies.
- **Manually-seeded Admin/Staff:** consistent with least privilege (`PROJECT_CONSTITUTION.md` Security Principle 2) — an Admin/Staff Account is powerful (`ADM-1` through `ADM-5`), so its creation should never be a public, self-service action.
- **Email unique per role, not globally:** a globally-unique constraint would force one person to use multiple email addresses to hold multiple legitimate roles (e.g., Tutor and Parent/Guardian), which no approved document requires and which would create unnecessary user friction without a corresponding security or integrity benefit.

## Consequences

- `ADR-011` Sections 5–11 and Section 4 Item 8 are now Decided; `ADR-011` itself is superseded by this ADR for those items and should be read alongside it, not as still-open.
- `ADR-003`'s Authentication Principles ("every authenticated identity corresponds to exactly one Account... authentication must always resolve to a durable, attributable identity") are now implementable under a concrete mechanism.
- Every existing self-registration command (`RegisterTutorCommand`, `RegisterStudentCommand`, `RegisterParentGuardianCommand`) must be extended to accept an email and a password — a breaking change to those three commands' shapes, addressed in the Database Logical Architecture Review and Backend Architecture Review that follow this ADR.
- `ADR-003`'s own separate, still-open decisions (Admin/Staff permission tiering, exact personal-data fields beyond email, Tutor-approval criteria, the Parent-Student invitation mechanism, denied-authorization-attempt auditing) are **not** resolved by this ADR and remain open; this ADR resolves only what `ADR-011` itself scoped (authentication, not authorization).

## Non-Goals

Does not decide `ADR-003`'s Authorization Principles, Role Model tiering, or any Permission Model detail — those remain `ADR-003`'s own, separately Proposed scope (Priority 2 of the Launch Preparation phase). Does not decide the exact personal-data fields an Account collects beyond the email required for login (`PRODUCT_REQUIREMENTS.md` Section 10.5, Item 17 remains otherwise open). Does not decide the age threshold distinguishing an adult from a minor Student (`DOMAIN_MODEL.md` Open Question 1) — irrelevant to authentication itself, relevant only to booking-time authorization.

---

## Addendum: Account Lockout & Absolute Session Lifetime (Accepted — 2026-07-21)

Two security parameters were implemented during Launch Preparation without a recorded governance decision, surfaced by the Final Architecture Review Board's production-readiness audit. This addendum resolves both explicitly, decided by the Project Director through the Constitution's Decision-Making Process — the same discipline this ADR itself already establishes for authentication generally. Nothing above this section is altered.

**Decision A — Account Lockout Policy.** Ratified as implemented: 5 consecutive failed login attempts against a single Account lock it for 15 minutes (`Account.MaxFailedLoginAttempts`, `Account.LockoutDuration`). Rationale: a conventional, conservative brute-force mitigation consistent with `ADR-003` Security Principle 2 (least privilege applied to credential-guessing resistance); does not alter the credential mechanism (Password) decided above.

**Decision B — Absolute Session Lifetime (Correction).** The Token lifetime row above states: "No absolute maximum session length is imposed beyond continued activity." This is corrected: a session is also capped at a 12-hour absolute maximum from issuance (`AuthToken.AbsoluteLifetime`), regardless of continued sliding renewal — ratifying the already-implemented, already-tested behavior. This is a correction of a documentation defect in the original Token lifetime row (`PROJECT_CONSTITUTION.md`: Documentation Governance, Item 3 — a contradiction between an approved document and an already-shipped, already-tested implementation is resolved by determining which is authoritative and correcting the other), not a new business decision — the same class of correction as `ADR-003`'s Second Addendum, Correction 1. Rationale for keeping the cap rather than removing it: it bounds how long a single continuously-active session can be kept alive by sliding renewal alone, without requiring re-authentication; a genuine new session (fresh login) remains always available once the ceiling is hit, so this is a ceiling on one continuous session, not a limit on how often someone may sign in.

**Corrected Token lifetime statement:** "Short-lived, sliding. 30 minutes of inactivity before expiry; each authenticated request extends expiry by the same window, capped at a 12-hour absolute maximum from issuance regardless of continued activity."

*Status: Accepted — 2026-07-21. Corrects the Token lifetime row's absolute-maximum statement (Decision B); ratifies Account lockout (Decision A). Nothing else in this ADR is altered.*

---

*Status: Accepted — 2026-07-20. Mechanism: Password credential, server-side reference-token session, 30-minute sliding expiration, no refresh token, manually-provisioned Admin/Staff. See Addendum above (2026-07-21) for Account Lockout and the corrected Token lifetime statement.*

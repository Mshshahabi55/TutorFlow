**Authoritative sources (in priority order):** `PROJECT_CONSTITUTION.md`, `PRODUCT_REQUIREMENTS.md`, `BUSINESS_MODEL.md`, `DOMAIN_MODEL.md`, `ARCHITECTURE.md`, `docs/adr/ADR-001-architecture-style.md`, `docs/adr/ADR-002-domain-boundaries.md`, `docs/adr/ADR-003-authentication-and-authorization.md`, `docs/adr/ADR-004-persistence-strategy.md`, `docs/adr/ADR-005-application-boundary.md`, `docs/adr/ADR-006-domain-events.md`, `docs/adr/ADR-007-validation-strategy.md`, `docs/adr/ADR-008-error-handling.md`, `docs/adr/ADR-009-audit-and-observability.md`, `docs/adr/ADR-010-api-boundary.md` (all approved and immutable, except `ADR-003`, which is itself Proposed). This ADR chooses no credential mechanism, no token mechanism, no vendor, and no library. It exists to enumerate the architectural decisions that remain missing before any authentication code may be written, and to record why a prior implementation attempt was reverted. Where a decision is not yet made, it is recorded under Open Questions or as an explicitly UNRESOLVED section, never invented.

---

# ADR-011: Authentication Strategy — Missing Decisions

## Status

**Proposed — pending user approval. Not Accepted.**

This ADR resolves nothing by itself. It exists to make the missing decisions visible and to prevent further implementation until they are made.

## Context

`ADR-003` (itself Proposed) established that authentication produces a single verified identity tied to exactly one Account, and separated authentication from authorization — but explicitly chose no authentication mechanism, deferring it to `ARCHITECTURE.md` Section 21, Item 4. No approved document since then has resolved that deferral. In the interim, an implementation ("Phase 16") was built ahead of that decision: it accepted a claimed `AccountId` with no credential verification and issued a signed token for it. It was reverted in full once recognized as an authentication mechanism decided implicitly, through implementation, rather than through the Constitution's Decision-Making Process — contradicting `PROJECT_CONSTITUTION.md`'s Decision-Making Process (Items 3, 6) and `ADR-003` Security Principle 6, which require Structural/security decisions to be explicitly proposed and approved before implementation. This ADR exists to make the missing decisions explicit before any further implementation attempt, and to prevent a repeat of that sequencing error.

## 1. Problem Statement

`ADR-003` establishes that authentication produces a single verified identity tied to exactly one Account, and separates authentication from authorization — but explicitly chooses no authentication mechanism, deferring it to `ARCHITECTURE.md` Section 21, Item 4. No approved document since then has resolved that deferral. A prior implementation ("Phase 16") attempted to build an authentication foundation anyway, ahead of that decision. This ADR documents why that attempt was architecturally invalid, and lists precisely which decisions must be made — by the user, through the Constitution's Decision-Making Process — before any authentication code is written again.

## 2. Why Phase 16 Was Reverted

Phase 16 implemented a `POST /authentication/login` endpoint that accepted a bare `AccountId` and, if that id matched an existing Tutor, Student, or Parent/Guardian record, issued a signed bearer token for it. This was reverted in full, not patched or extended, for the following reasons:

- **No credential was ever verified.** Authentication, by definition, verifies that the caller is who they claim to be. Accepting a claimed `AccountId` with no password, no possession factor, no external identity assertion — nothing but the id itself — verifies nothing. `AccountId` is not a secret: obtaining or being exposed to a valid `AccountId` (for example, through another endpoint's response, a log, or an authorized party's own view of it) is sufficient to bypass this mechanism entirely — no brute-force guessing is required, since the id was never designed to be unguessable in the first place. This is impersonation-by-design, not authentication.
- **It preempted a decision `ADR-003` explicitly deferred.** `ADR-003` (Proposed) is itself pending approval, and even that ADR does not choose a credential or token mechanism — it names the mechanism choice as a separate, not-yet-made Architectural Decision Candidate (`ARCHITECTURE.md` Section 21, Item 4). Implementing "a" mechanism, even labeled a stub, forecloses that decision in practice: it establishes a shape (`AccountId`-only input, a custom bearer token format, no expiration) that the real decision might not have chosen, and that later code and tests would come to depend on.
- **Passing tests did not make it correct.** The prior report noted 100% test pass rates and used that as evidence of soundness. Tests validated only that the code did what it was written to do; they could not and did not validate that what it was written to do was the right thing to build. Architectural validity is not established by test coverage.
- **It violated the project's own decision discipline.** The Constitution requires that Structural/consequential decisions (which an authentication mechanism plainly is — `ADR-003` Security Principle 6) be explicitly proposed and approved before implementation, not settled ad hoc inside a phase whose stated scope was narrower than the decision it ended up making.

Reverting removed: `AuthenticateCommand`, `AuthenticateCommandHandler`, `AuthenticateResult`, `IAuthenticationService`/`AuthenticationService`, `SimpleBearerToken`, `BearerTokenCurrentUserProvider`, `AuthenticationEndpoints`, all related DI registrations, the `Authentication:SigningKey` configuration entry, and the `Microsoft.Extensions.Configuration.Abstractions` package reference added only to support it. `NullCurrentUserProvider` and all DI/`Program.cs`/`ApplicationEndpointRegistration.cs` registrations were restored to their pre-Phase-16 state. Build and full test suite were verified green after the revert, with no changes to Domain, Application business logic, or any other previously-approved feature.

## 3. Authentication vs. Authorization

Restated from `ADR-003` because conflating the two is precisely the mistake Phase 16 made in miniature (it treated "an Account exists" as sufficient grounds to issue a token, collapsing identity verification into identity lookup):

- **Authentication** answers "who is making this request, verifiably?" It must produce a durable, attributable identity tied to exactly one Account (`ADR-003`: Authentication Principles) — and it must actually verify a credential to do so, not merely resolve an id to a record.
- **Authorization** answers "what may this already-verified identity do?" It is a separate, downstream decision made in the Domain layer of the resource-owning bounded context (`ADR-003`: Authorization Principles, Resource Ownership).
- This ADR concerns only the first question. No authorization/role/policy/claims logic is in scope here, exactly as it was out of scope for Phase 16.

## 4. Required Architectural Decisions Before Implementation

Before any authentication code is written, the following must each be explicitly decided and approved, in this order of dependency:

1. **Credential mechanism** (Section 5) — what proves the caller is who they claim to be.
2. **Token/session mechanism** (Section 6) — how a successful authentication is carried across subsequent requests.
3. **Token lifetime** (Section 7).
4. **Refresh strategy** (Section 8), if the token/session mechanism has a lifetime at all.
5. **Signing/key management** (Section 9), if the chosen mechanism is signature-based.
6. **Logout strategy** (Section 10).
7. **Session/token revocation** (Section 11).
8. **Admin/Staff account provisioning** — a prerequisite specific to that one role, not part of the seven-item dependency chain above: no mechanism decided in Sections 5–11 makes Admin/Staff authenticable unless the still-open question of how an Admin/Staff account is created in the first place (`ADR-003` Open Question 2; `DOMAIN_MODEL.md` Open Question 15) is separately resolved. This can be resolved independently and in parallel with items 1–7, but it must be resolved before Admin/Staff, specifically, can be authenticated under any mechanism eventually chosen for the other three roles.

Each of these is a Structural decision under the Constitution's Decision-Making Process and requires its own explicit proposal and approval — none may be resolved by inventing a default inside an implementation phase.

## 5. Credential Mechanism Candidates

No approved document chooses among these. Listed as candidates only, not evaluated or ranked:

- **Password** — the caller supplies a shared secret established at registration. Requires deciding storage (hashing algorithm/parameters), complexity policy, reset flow, and how it interacts with the still-unresolved registration/personal-data fields (`DOMAIN_MODEL.md` Open Question 14; `ADR-003` Open Question 7).
- **Passwordless** (e.g., magic link, one-time code via email/SMS) — requires deciding delivery channel, code/link lifetime, and rate-limiting/abuse controls; also depends on which contact fields are collected at registration, which is itself unresolved.
- **External Identity Provider** (e.g., a third-party OAuth/OIDC provider) — requires deciding which provider(s), account-linking behavior for the four existing role types, and data-residency/vendor implications for the GDPR-grade personal data handling `ADR-003` already requires (Security Principle 3).

## 6. Token/Session Mechanism Candidates

No approved document chooses among these:

- **Cookie-based session** — server-held or signed-cookie session state; implies CSRF-protection decisions and a statefulness choice inconsistent with a purely stateless API unless explicitly accepted.
- **Self-contained cryptographically signed token** (for example, though not preferentially, a JWT) — stateless, but requires a revocation strategy (Section 11) since a self-contained token cannot be un-issued without one. JWT is cited here only as one widely recognized example of this general category, not as a preferred or default solution; it is the category itself — not any specific standard, library, or vendor — that is the candidate.
- **Reference token** (opaque token, server-side lookup) — trivially revocable, but requires a token store, which is itself a persistence decision layered on top of the still-open persistence-technology decision (`ARCHITECTURE.md` Section 21, Item 2).

## 7. Token Lifetime — UNRESOLVED

No approved document specifies how long an authenticated session or token remains valid. This was the exact gap the Phase 16 instructions required stopping on rather than inventing a default, and it remains unresolved here. Any value chosen (minutes, hours, "no expiration") is a security-relevant decision the user must make explicitly.

## 8. Refresh Strategy — UNRESOLVED

No approved document specifies whether a short-lived token may be silently renewed (e.g., via a refresh token), whether renewal requires re-authentication, or whether renewal exists at all. This is only decidable after Section 6 and Section 7 are settled, since a mechanism with no lifetime has nothing to refresh.

## 9. Signing Key Management — UNRESOLVED

No approved document specifies how a signing key (if the chosen token mechanism uses one) is generated, stored, rotated, or scoped per environment. Phase 16's fallback development placeholder key is not a decision on this question — it was an implementation-time expedient inside an implementation that has now been reverted, and must not be read as precedent.

## 10. Logout Strategy — UNRESOLVED

No approved document specifies what "logging out" means under any of the candidate token mechanisms above. For a stateless, self-contained token, logout is not inherently well-defined without a revocation mechanism (Section 11); for a reference token or cookie-session, it may be a straightforward server-side invalidation. Which applies depends on decisions not yet made.

## 11. Session/Token Revocation — UNRESOLVED

No approved document specifies whether an individual session/token can be invalidated before its natural expiry (e.g., on suspected compromise, password change, or Admin action against a Tutor/Student account). Tutor suspension itself is not an open question — it is an already-approved capability (`PRODUCT_REQUIREMENTS.md` ADM-2) — but revocation would still interact with the still-open Admin/Staff account-creation and permission-tiering questions (`ADR-003` Open Questions 2, 3), since those determine who can even hold an Admin/Staff session to revoke, and with the credentials/criteria a Tutor submits for approval (`ADR-003` Open Question 4 — a question about Tutor **approval**, not Tutor suspension; see Section 13, Open Question 9). This is a prerequisite for any credible security posture and cannot be deferred indefinitely, but it is not decided here.

## 12. Security Considerations

Carried forward from `ADR-003`'s Security Principles and applied to why this ADR exists rather than a chosen implementation:

- **Security by design** requires the mechanism be decided before code is written, not discovered by writing code and testing it — this is the specific principle Phase 16 violated. (`ADR-003` Security Principle 1)
- **Accountability for access** requires every mutating action be attributable to a genuinely verified identity (`ADR-003` Security Principle 4; `PRODUCT_REQUIREMENTS.md` CONST-2) — an unverified `AccountId` lookup cannot satisfy this, since it attributes actions to whoever supplied a valid-looking id, not whoever actually controls that Account.
- **Protection of personal data** (`ADR-003` Security Principle 3; CONST-4) is directly affected by the credential-mechanism choice (Section 5) — password storage, third-party data sharing with an external IdP, or contact-channel exposure via passwordless delivery all carry different data-protection implications that must be weighed as part of that decision, not after it.
- **Security decisions are owned, not implicit** (`ADR-003` Security Principle 6) is the principle this entire ADR exists to uphold: the mechanism, once chosen, requires its own explicit proposal and approval.

## Alternatives Considered

These are alternatives to *how this documentation gap itself was handled*, not alternatives among the still-open mechanism candidates in Sections 5–11 — this ADR chooses no mechanism, and none is weighed here either:

- **Patch or incrementally harden the reverted Phase 16 implementation** (for example, adding a password field to `AuthenticateCommand` without a broader mechanism decision) — rejected. This would still be choosing a mechanism through implementation rather than through the Constitution's Decision-Making Process, and would not remove the risk of code and tests becoming load-bearing on an undecided shape.
- **Leave the reverted implementation in place with a documented caveat (e.g., a TODO comment) rather than a full revert** — rejected. A caveat does not stop the shape (`AccountId`-only input, a custom token format, no expiration) from being depended upon by future work; only a full revert removes that risk, consistent with `PROJECT_CONSTITUTION.md` Engineering Principle 5 (incremental, reversible delivery).
- **Silently drop the authentication effort without a document explaining why** — rejected. This would violate Documentation Governance (transparency of decisions) and leave no discoverable record for future implementers of why Phase 16 no longer exists.
- **Revert the implementation in full and record the missing decisions in a dedicated ADR (chosen)** — the only option consistent with `PROJECT_CONSTITUTION.md`'s Decision-Making Process, Documentation Rules, and Security Principle 6, and the same discipline already applied to every other Structural decision in this project.

## Consequences

- No authentication code of any kind may be written until this ADR's Sections 5–11 (and, for Admin/Staff specifically, Section 4 Item 8) are each explicitly decided and approved.
- Any future implementer proposing a mechanism must do so as its own Structural proposal, with rationale and trade-offs, per the Constitution's Decision-Making Process — not as a byproduct of an unrelated feature or phase.
- Work that depends on a genuinely authenticated identity (for example, authorization enforcement or identity-attributed audit records) remains blocked until the mechanism is chosen; this is an accepted cost of not inventing a shape prematurely.
- This ADR itself changes no code and no previously approved document; it only records the gap and the reasoning for the Phase 16 revert.

## Risks

- **Indefinite delay risk:** leaving the mechanism undecided blocks any future work that depends on a genuinely authenticated identity, including authorization enforcement (`ADR-003`) and identity-attributed audit records (`ADR-009`); if this decision is not prioritized, downstream work has no path forward.
- **Repeat of the Phase 16 error:** without this ADR being consulted, a future implementer could again build an authentication stub ahead of an explicit decision; this ADR's existence mitigates but does not eliminate that risk.
- **Admin/Staff remains permanently unauthenticatable** until the still-open Admin/Staff account-creation mechanism (`ADR-003` Open Question 2) is separately resolved, regardless of which credential/token mechanism is eventually chosen for the other three roles.
- **Partial-resolution risk:** because Sections 5–11 are interdependent in places (for example, signing-key management applies only if a signature-based token mechanism is chosen), resolving them out of order or in isolation risks an internally inconsistent mechanism; this is a process risk to manage when they are eventually decided, not an argument for resolving any of them here.

## Future Evolution

Once the credential mechanism, token/session mechanism, lifetime, refresh strategy, signing/key management, logout strategy, and revocation strategy (Sections 5–11) are each explicitly proposed and approved through the Constitution's Decision-Making Process, they should be recorded either as amendments to this ADR or as one or more successor ADRs that supersede it. Any such future ADR must also resolve how the chosen mechanism interacts with the still-open Admin/Staff account-creation question (Section 4, Item 8) and with the authentication-event audit question raised in Open Question 10 below. This ADR does not itself schedule, prioritize, or presuppose the outcome of that future work.

## 13. Open Questions

1. Which credential mechanism (Section 5) will TutorFlow use? Not addressed by any approved document.
2. Which token/session mechanism (Section 6) will carry an established identity across requests? Not addressed by any approved document.
3. What is the token/session lifetime (Section 7)? Not addressed by any approved document.
4. Is there a refresh strategy (Section 8), and if so, what triggers it and what are its limits? Not addressed by any approved document.
5. How is a signing/session key generated, stored, rotated, and scoped per environment (Section 9)? Not addressed by any approved document.
6. What does logout mean under the eventually-chosen mechanism (Section 10)? Not addressed by any approved document.
7. Can a session/token be revoked before natural expiry, and under what circumstances (Section 11)? Not addressed by any approved document.
8. How does the credential mechanism interact with the still-unresolved Account personal-data fields (`DOMAIN_MODEL.md` Open Question 14; `ADR-003` Open Question 7) and Admin/Staff account-creation mechanism (`ADR-003` Open Question 2)?
9. All Open Questions already carried by `ADR-003` remain unresolved and are not restated in full here but are incorporated by reference — most directly Open Question 4 (Tutor approval credentials/criteria, a different but easily-confused use of the word "credential") and Open Question 2 (Admin/Staff account creation, which affects whether Admin/Staff can be authenticated at all).
10. Must a successful or failed **authentication** attempt (for example, an invalid credential submitted at login) be recorded in the audit trail? This is distinct from the already-tracked question of whether a denied **authorization** attempt must be audited (`ADR-003` Open Question 6; `ADR-007` Open Question 3; `ADR-008` Open Question 3; `ADR-009` Open Question 1) — authentication failure and authorization denial are different events at different points in a request's lifecycle, and no approved document addresses the former. `ADR-009`'s Business Event Audit Rules list only booking/availability/Tutor-approval/suspension events; login is not among them and is not established as excluded either. Whichever mechanism is eventually chosen (Sections 5–11), this question must be resolved alongside it, not assumed.

## 14. Decision Status

**Status: Proposed. Not Accepted.**

No authentication code — foundation, stub, or otherwise — is to be written until this ADR, and the specific mechanism decisions it enumerates (Sections 5–11, and the Admin/Staff prerequisite in Section 4 Item 8), are explicitly approved by the user through the Constitution's Decision-Making Process. This ADR itself makes no implementation choice and must not be treated as authorization to build any of the candidates it lists.

## Traceability

| Element of this ADR | Source |
|---|---|
| Deferred authentication mechanism | `ARCHITECTURE.md` Section 21, Item 4 |
| Authentication produces a verified identity, distinct from authorization | `ADR-003`: Decision, Authentication Principles, Authorization Principles |
| Security decisions require explicit proposal and approval | `ADR-003` Security Principle 6; `PROJECT_CONSTITUTION.md`: Decision-Making Process |
| Structural decisions require explicit approval before implementation | `PROJECT_CONSTITUTION.md`: Decision-Making Process, Items 3, 6 |
| GDPR-grade protection of personal data affected by credential-mechanism choice | `PRODUCT_REQUIREMENTS.md` CONST-4; `ADR-003` Security Principle 3 |
| Accountability for access requires a genuinely verified identity | `PRODUCT_REQUIREMENTS.md` CONST-2; `ADR-003` Security Principle 4 |
| Admin/Staff account-creation as an unresolved prerequisite | `DOMAIN_MODEL.md` Open Question 15; `ADR-003` Open Question 2 |
| Tutor approval credentials/criteria (distinct from Tutor suspension) | `DOMAIN_MODEL.md` Open Question 4; `ADR-003` Open Question 4 |
| Business-event audit scope, as it may extend to authentication events | `ADR-009`: Business Event Audit Rules |
| Incremental, reversible delivery favoring full revert over partial patch | `PROJECT_CONSTITUTION.md`: Engineering Principle 5 |

---

*Status: Proposed — pending user approval. No further ADRs or code are to be created until this ADR is approved.*

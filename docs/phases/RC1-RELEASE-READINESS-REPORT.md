# RC1 Release Readiness Report

**Date:** 2026-07-30
**Branch audited:** `develop` @ `6da290f`
**Method:** six independent, parallel audit passes (authorization & security; EF Core tracking correctness; layer-dependency & no-stub compliance; audit-trail coverage; money/time/i18n compliance; test coverage & build/CI health), each scoped to a distinct CLAUDE.md non-negotiable rule or Definition-of-Done requirement. Findings merged, deduplicated, classified P0/P1/P2, then P0s fixed, low-risk/high-value P1s fixed, P2s deferred, full verification pipeline re-run.

---

## Summary

| Audit | P0 | P1 | P2 | Outcome |
|---|---|---|---|---|
| Authorization & security | 0 | 0 | 1 | Matrix matches code in both directions; no secrets; no IDOR/injection/over-posting found |
| EF Core tracking correctness | 0 | 0 | several | `.AsNoTracking()` discipline intact across all bounded contexts, including newest ones |
| Layer dependency & no-stub | 0 | 0 | 2 | Zero layer violations; zero TODO/FIXME/NotImplementedException |
| Audit-trail coverage | 0 | 3 | 2 | 3 pre-existing governance events remain unaudited, gated on an already-open ADR question |
| Money/time/i18n compliance | 0 | 0 | 0 | Fully clean — no findings at all |
| Test coverage & build/CI health | 0 | 2 | 0 | Both fixed (see below) |

**No P0s were found in this audit.** No duplicate findings across agents — each covered a distinct dimension.

---

## Release Blockers

**None.** Build, lint, and full test suite are green across every layer (see Verification Pipeline below), and five of six audits returned clean.

### Audit trail coverage for `RelationshipConfirmed`, `SessionCompleted`, `SessionMarkedNoShow` — flagged, not a blocker

These three Domain Events mutate governance-relevant state — confirming who may transact with whom (including on behalf of a minor), and determining a Session's financial/dispute-bearing outcome — but are not wired into `AuditDomainEventHandler`. This is **not** a regression or an oversight in new code: it is a long-standing, explicitly documented open question (`ADR-009` Open Question 4, carried forward from `ADR-006` Open Question 4 — "do the Domain Events not explicitly named in CONST-2's wording... each require an independent audit entry, or are some covered indirectly?"). CLAUDE.md's Stop Conditions bar inventing an answer to an unresolved open governance question, so this was escalated rather than fixed.

**Owner decision recorded 2026-07-30** (`docs/decisions/PENDING-OWNER-DECISIONS.md` Section 6): this affects audit-*policy* completeness, not runtime correctness, and is decoupled from RC1 codebase readiness — it does not block tagging. Whether these three events ultimately need independent audit entries remains a separate, still-open question, tracked in that document for resolution independent of the RC1 timeline. If and when the owner decides they do, the implementation is mechanical — extend `AuditDomainEventHandler`'s existing switch statement following the identical pattern already used for the 16 currently-audited event types (no new `AuditEntry` shape or infrastructure needed).

All other bounded contexts checked — Communication (`ADR-022`), Meetings (`ADR-023`) — correctly audit every governance-relevant event and correctly *exclude* the ones their own ADRs say should be excluded (message body content, `NotificationCreated`).

---

## Fixed

**P0:** none to fix.

**P1 (low-risk, high-value — fixed):**
1. **Missing Infrastructure.Tests coverage for `SessionRepository.GetStatusCountsAsync`** (introduced in `2430803`, whose own commit message claimed full-layer coverage but never touched `SessionRepositoryTests.cs`). Added a direct SQLite-backed test covering all four `SessionStatus` values, following the file's existing pattern. Commit `6da290f`.
2. **Six `react-refresh/only-export-components` ESLint warnings** in `frontend/src/layouts/navSections.tsx` — a data/lookup-function module with no actual component, incorrectly flagged because its exported arrays embed JSX icons. Disabled the rule for the file with an explanatory comment. Commit `6da290f`. Lint is now 0 warnings, 0 errors.

---

## Remaining Technical Debt (P2 — deferred)

1. **`docs/adr/ADR-016-audit-durability-strategy.md`'s Scope Note is stale.** It still lists only the original six audited event types; ten more (`AvailabilitySlotReopened`, `TutorProfileSubmitted`, `LoginSucceeded`/`LoginFailed`, `AccountLocked`, `PasswordReset`, `ConversationStarted`, `MessageSent`, `MeetingCreated`/`MeetingUpdated`/`MeetingCancelled`) were added by later ADRs and are correctly wired in code, just never folded back into ADR-016's own text. Documentation-only; no runtime impact.
2. **`.devcontainer/docker-compose.yml` sets a default `POSTGRES_PASSWORD: postgres`.** Not reachable outside the container network, dev/CI-only per the Section 5 decision — no action needed, noted for completeness.
3. **Frontend Vitest suite has a known, pre-existing, machine-load-triggered worker-timeout flake** when run immediately back-to-back with `npm run build` (reproduced once during this audit: 1 file / 1 test failed with a worker-pool timeout; the identical suite passed 136/136 files, 721/721 tests on an isolated re-run). This is the same class of environment sensitivity already documented in `README.md`, not a new or code-level issue.
4. Several `.AsNoTracking()` usages flagged for review by the tracking-correctness audit were all confirmed correctly scoped to genuinely read-only paths with no mutation risk — no action needed, listed here only so this audit's full scope is visible.

---

## Known Limitations (pre-existing, out of this audit's scope to resolve)

- **Never run against a real PostgreSQL instance.** All backend tests run against in-memory SQLite; the 14 EF Core migrations have never been applied via `dotnet ef database update` against real Postgres. A dev/CI-only Docker Compose setup was approved 2026-07-29 (`docs/decisions/PENDING-OWNER-DECISIONS.md` Section 5) but is itself not yet build-verified in every environment.
- **`ADM-4` ("Admin can resolve booking conflicts") is unimplemented.** `Permission.ResolveBookingConflict` exists and is granted to Admin/Staff, but no endpoint/handler exists — a deliberate, consistently documented gap pending `DOMAIN_MODEL.md` Open Question 13.
- **Learning Plans & Payments, Reviews & Ratings, and Disputes/Moderation are all explicitly out of RC1 scope**, per owner decisions recorded 2026-07-29 in `docs/decisions/PENDING-OWNER-DECISIONS.md`.
- **Recurring availability, blocked dates, buffer time, and a "working hours" concept remain unresolved** (`ADR-025` Questions 1–6); only minimum booking notice / maximum booking horizon (Questions 7–9) shipped.
- **Audit trail coverage gap** for `RelationshipConfirmed`/`SessionCompleted`/`SessionMarkedNoShow` — see Release Blockers above.

---

## Verification Pipeline (this session, post-fix)

| Check | Result |
|---|---|
| `dotnet build` (backend) | 0 warnings, 0 errors |
| `dotnet test` (backend) | **792/792 passed**, 0 failed — Domain.Tests 147, Application.Tests 302, Infrastructure.Tests 73 (+1 new), Web.Tests 270 |
| `npm run build` (frontend) | Success |
| `npm run lint` (frontend) | 0 warnings, 0 errors |
| `npm test -- --run` (frontend) | **721/721 passed**, 136/136 files, 0 failed (on isolated re-run; one back-to-back run hit the known worker-timeout flake noted above) |

All Definition-of-Done build/test gates are green.

---

## Recommended Git Tag

**`v1.0.0-rc1`** — standard SemVer pre-release form, consistent with the "RC1" terminology this repository already uses throughout (`README.md`, `docs/decisions/PENDING-OWNER-DECISIONS.md`) to mean the full v1 feature set described in `PROJECT_CONSTITUTION.md`/`PRODUCT_REQUIREMENTS.md`. No git tags currently exist in this repository, so this would be the first.

---

## Final Recommendation

**Ready for RC1.** No technical release blockers were identified. Every governance rule audited (authorization/security, EF Core persistence correctness, layer dependencies, no-stub discipline, money/time/i18n isolation) came back clean across all newest bounded contexts, and the full build/lint/test pipeline is green in every layer.

One governance decision (`ADR-009` Open Question 4 — audit coverage for `RelationshipConfirmed`/`SessionCompleted`/`SessionMarkedNoShow`) remains pending. Per the owner decision recorded 2026-07-30 (`docs/decisions/PENDING-OWNER-DECISIONS.md` Section 6), this affects audit-policy completeness rather than runtime correctness and may be resolved independently of the RC1 codebase.

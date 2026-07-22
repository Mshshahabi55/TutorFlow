# CLAUDE.md — Governance for AI Sessions Working on TutorFlow

This file governs how any AI coding session (or human contributor) should operate in this repository. It does not restate product or architecture content — see the documents it points to for that.

## Authority precedence

When any two documents disagree, the higher one in this list wins. Do not average, blend, or split the difference between them:

1. **`PROJECT_CONSTITUTION.md`** — immutable, top authority. Nothing in this repo may contradict it.
2. **`docs/adr/` (Architecture Decision Records)** — the binding technical/structural decisions. An Accepted ADR is as authoritative as the Constitution on the technical question it settles.
3. **`PRODUCT_REQUIREMENTS.md`**
4. **`DOMAIN_MODEL.md`**
5. **`ARCHITECTURE.md`**
6. **The code** — the code is a *reflection* of the five documents above, never a source of truth over them. If the code disagrees with an Accepted ADR, the code is wrong, not the ADR — fix the code or raise a new ADR, don't quietly follow the code.

`docs/api/AUTHORIZATION_MATRIX.md` is the single reference for every endpoint's authorization requirement — restated from the ADRs, in exhaustive endpoint-by-endpoint form. Treat it as authoritative for "what does this endpoint require," but if it and an ADR ever disagree, the ADR wins and the matrix has drifted and needs correcting.

## Non-negotiable rules

- **Never violate the layer dependency rule.** `Domain ← Application ← Infrastructure/Web`. Domain has zero outward dependency. Application depends only on Domain. Never make Domain depend on Application, Infrastructure, or Web for any reason, including "just this once."
- **Never introduce a `TODO`, `FIXME`, or `NotImplementedException`.** Either implement the thing fully, or stop and ask the owner. A stub left for "later" is exactly how `ADM-4`'s `ResolveBookingConflict` permission ended up granted with zero implementation behind it — don't repeat that pattern.
- **Never add a package without stating why in the commit message.** No silent dependency additions.
- **Every new endpoint must appear in `docs/api/AUTHORIZATION_MATRIX.md` with a non-Open authorization row before it is considered done.** An endpoint with no row, or an "Open" row, is not shippable.
- **Every new Domain Event that mutates governance-relevant state must be covered by the audit trail** (see `docs/adr/ADR-016-audit-durability-strategy.md` for what "covered" means and which events currently qualify).
- **Never commit secrets.** Connection strings, API keys, and credentials live in user-secrets or environment variables, never in `appsettings.json`, `appsettings.*.json`, or any tracked file. `appsettings.json`'s connection string is a `REPLACE_ME` placeholder by design — keep it that way.
- **Tests are part of "done."** Domain, Application, and Web layers each require test coverage for new behavior — not just one layer. Follow the existing pattern: a Domain test for the invariant, an Application test for the handler's orchestration/authorization, a Web test for the HTTP contract.
- **A test that asserts a write succeeded MUST re-read the entity from a fresh `DbContext` and assert the persisted state. Asserting that `SaveChangesAsync` was called is not evidence that anything was saved.** See `docs/phases/PHASE-01B-REPORT.md`: `Application.Tests`' `FakeUnitOfWork` only counts `SaveChangesAsync` calls, so every handler using it looked correct while `Approve`/`Suspend`/`SetHourlyRate`/`SetSubject`/`SetLanguage`/`SetLocation`/`SetOfferedDurations`/`Login`/`AdminResetPassword` silently never persisted their mutations in the real EF Core path.
- **`.AsNoTracking()` is permitted only on read-only query paths. Any repository method whose result is mutated and saved must return a tracked entity.** A no-tracking read handed to a command handler that mutates it and calls `IUnitOfWork.SaveChangesAsync` produces no `UPDATE` at all — `EfUnitOfWork` never re-attaches the aggregates it's given, it only relies on the entity already being tracked from its read (see `README.md`'s "Known pitfalls" section and `docs/phases/PHASE-01B-REPORT.md`).
- **`Application.Tests` proves orchestration, authorization, and validation only. Any claim that a write persisted must be proven in `Web.Tests` by re-reading from a fresh `DbContext` scope.** `FakeUnitOfWork` has no concept of EF Core change tracking — it cannot tell a mutation that will actually reach the database from one that's about to vanish (`docs/phases/PHASE-01C-REPORT.md`).
- **English is the only UI language. Never introduce i18n in v1.**
- **Never add a dependency on a service unreachable from Iran.**
- **All timestamps are stored in UTC. Tehran is UTC+03:30 with no DST — never assume whole-hour offsets.**
- **All times are stored and transmitted in UTC. All times displayed to or entered by a user are Asia/Tehran (UTC+03:30, no DST). Conversion happens only in `frontend/src/shared/time/` — never inline.**
- **Single currency. Never introduce multi-currency logic in v1.**

See `docs/adr/ADR-018-regional-deployment-and-market-scope.md` for the full market-scope decision these four rules restate.

## Definition of Done

A unit of work is not done until all of the following are true:

- [ ] Implements the full behavior — no stub, no partial case silently unhandled
- [ ] Follows the layer dependency rule (see above) — verify no new `using` crosses it
- [ ] Every new/changed endpoint has a non-Open row in `docs/api/AUTHORIZATION_MATRIX.md`
- [ ] Every new Domain Event that should be audited is wired into the audit path
- [ ] Domain, Application, and Web tests all added/updated as applicable
- [ ] `dotnet build` — 0 warnings, 0 errors
- [ ] `dotnet test` (run from `backend/`, not just the changed project) — 0 failures
- [ ] Frontend: `npm run build`, `npm run lint`, `npm test -- --run` all clean, if frontend was touched
- [ ] No secret, credential, or real connection string committed
- [ ] Commit message follows the convention below and states *why*, not just *what*

## Commit convention

[Conventional Commits](https://www.conventionalcommits.org/): `type: short description`

- `feat:` — a new capability
- `fix:` — a bug fix
- `chore:` — tooling, config, dependency, repo-hygiene changes with no product behavior change
- `docs:` — documentation-only changes
- `test:` — test-only changes
- `refactor:` — internal restructuring with no behavior change

Keep commits logical and separate — do not squash an entire multi-part change into one commit. Each commit should stand on its own and describe one coherent change.

## Stop conditions

Stop and ask the owner instead of deciding, whenever:

- The work touches an **unresolved Open Question** in `DOMAIN_MODEL.md` or `PRODUCT_REQUIREMENTS.md` (e.g., cancellation notice periods, who may mark a Session Completed/No-Show, the age threshold for "minor," conflict-resolution mechanics). Do not invent an answer to fill the gap.
- The work would require **a new ADR** — a new Structural or architectural decision not already settled by an Accepted ADR. Propose the decision; don't implement around it.
- The work would **change the public API contract** in a way not already specified — a new required field, a changed status code, a renamed route, a changed response shape for an existing endpoint.
- The work would require **a new business rule or a new `Permission` enum value** not already documented in an Accepted ADR.
- A governing document appears to contradict another, or a document's own stated status (e.g., "Accepted" vs. "Proposed") disagrees with how it's actually treated elsewhere in the repo. Report the contradiction; don't silently pick a side without flagging it.

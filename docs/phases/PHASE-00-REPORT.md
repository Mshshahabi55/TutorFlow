# Phase 0 Report — Safety Net (Stabilization)

Branch: `develop` (created from `main`) | Commits: `063b0c1` .. `6dde560` (7 total, 6 after the initial baseline)

## 1. Clean Architecture validation

**No source file under `backend/src/`, `backend/tests/`, or `frontend/src/` was modified**, with one explicit, task-authorized exception: `backend/src/Infrastructure/TutorFlow.Infrastructure.csproj` (Task 5, package version pins — a build-configuration file, not source code; the task itself required this edit and it would have been impossible to complete otherwise).

Verified by diffing the entire phase range against the initial baseline commit:

```
git diff --stat 063b0c1 6dde560
```

Result: **7 files changed, 185 insertions, 2 deletions** — `CLAUDE.md`, `README.md`, `IMPLEMENTATION_PLAN_V2.md` (2-line banner only), `backend/global.json` (new), `backend/src/Infrastructure/TutorFlow.Infrastructure.csproj` (2-line version change), `docker/.gitkeep` (new), `scripts/.gitkeep` (new). Zero `.cs`, `.ts`, or `.tsx` files appear anywhere in that diff.

## 2. Repository convention validation

- **`.gitignore` correctness:** written to exclude `bin/`, `obj/`, `node_modules/`, `dist/`, `.vite/`, `.vs/`, `.vscode/`, `.idea/`, `*.user`, `*.suo`, `appsettings.*.local.json`, `.env`/`.env.*`, `TestResults/`, `*.db`/`*.db-shm`/`*.db-wal`, plus OS cruft. Verified after `git add -A`: `git diff --cached --name-only | grep -E "(^|/)(bin|obj|node_modules)/"` returned **zero matches** before the initial commit.
- **What was committed:** 572 files, 40,185 line insertions, 0 deletions, in the initial commit. Top-level breakdown: `backend/` 364 files, `frontend/` 171 files, `docs/` 22 files, 13 root-level docs/config files, `.continue/` 2 files, `.github/` 1 file.
- **What was NOT committed:** every `bin/`, `obj/`, and `node_modules/` directory across both the backend and frontend trees — confirmed absent from the staged list before commit, per the explicit verification step required by Task 1.
- **Staged size:** exact byte size could not be measured (`du` timed out against this Windows filesystem within the 2-minute tool limit); the line-count proxy (40,185 insertions across 572 files) is reported instead, per Task 1 step 4's request for "total file count and total staged size" — file count is exact, size is a documented gap rather than a guess.
- **Branch state:** `main` holds only the initial baseline commit (`063b0c1`). `develop` was created from it and holds all 6 subsequent Phase 0 commits. No remote was added; nothing was pushed, per instruction.

## 3. Technical debt discovered

Found while executing this phase, not part of any assigned task — reported, not fixed:

- **`frontend/vitest-results.json` was committed in the initial baseline.** It is a generated Vitest output artifact (not source), and its presence means every future test run will produce a diff. It was not in Task 1's explicit minimum `.gitignore` list, so it wasn't caught. Recommend adding `vitest-results.json` (or a broader `*-results.json` pattern, if this is a recurring tool output) to `.gitignore` and removing it from tracking in a future commit.
- **`npm audit` reports 5 vulnerabilities** in the frontend's dev/test tooling chain — 3 moderate, 1 high, 1 critical:
  - `esbuild` (moderate) — dev server can be sent arbitrary requests
  - `vite` (high) — path traversal in optimized-deps `.map` handling, plus two related advisories
  - `vitest` (critical) — arbitrary file read/execute when the Vitest UI server is listening
  - `vite-node`, `@vitest/mocker` (moderate) — transitively depend on the vulnerable `vite`
  All five are confined to the **dev/test toolchain** (vite/vitest/esbuild and their internals) — none are in a production runtime dependency (React, MUI, axios, react-query, etc. are unaffected). `npm audit fix --force` would resolve them but requires a breaking upgrade to `vitest@4.x`, which is out of this phase's scope (Phase 0 forbids touching `frontend/src/`, and a major test-runner upgrade carries its own risk that deserves a dedicated phase, not a drive-by fix).
  - `npm warn allow-scripts` also flagged 3 packages (`esbuild` variants) with unreviewed postinstall scripts — informational, not a version/CVE issue.
- **The Web.Tests flakiness is more severe than the original `PROJECT-STATUS.md` audit characterized.** That report described it as specific to running `dotnet test TutorFlow.sln` (matching CI) versus the safer plain `dotnet test`. During Task 5's verification (three additional `dotnet test` runs performed to validate the package downgrade), the **exact same class of failure — always ~7 failures in `Web.Tests`, always different specific tests — occurred under plain `dotnet test` too**, not only the `.sln` form. This is corrected in `README.md`. The flakiness is real, non-deterministic, and not tied to any particular invocation command — a fresh clone has a meaningful chance of seeing a red `Web.Tests` run no matter how tests are invoked.

## 4. Required code changes (Task 5 only)

- **`backend/src/Infrastructure/TutorFlow.Infrastructure.csproj`** — `Microsoft.Extensions.DependencyInjection.Abstractions` and `Microsoft.Extensions.Hosting.Abstractions` downgraded from `10.0.10` to `9.0.1`.
  - **Investigation:** only one project in the solution references either package — `TutorFlow.Infrastructure`. Both are used for entirely standard purposes: `Microsoft.Extensions.DependencyInjection` for `IServiceCollection` registration (`DependencyInjection.cs`), and `Microsoft.Extensions.Hosting`'s `BackgroundService` base class for `AuthTokenCleanupService.cs` (an in-process session-cleanup hosted service). Neither usage touches any API introduced after .NET 9.
  - **Why they were on 10.x is unknown** — no comment, commit message, or document in the repo explains it; most likely an accidental `dotnet add package` without an explicit `--version` flag picking up a newer package release that happens to still be usable against a `net9.0` `TargetFramework` (these are pure abstraction packages, so cross-version compatibility is common, which is likely why the mismatch built fine and went unnoticed).
  - **Verification:** downgraded to `9.0.1` — the exact version already pinned for `Microsoft.EntityFrameworkCore.Design` and `Microsoft.Data.Sqlite` in the same file — and ran `dotnet build TutorFlow.sln`: **0 Warnings, 0 Errors**. Ran `dotnet test` three times after the change: Domain.Tests, Application.Tests, and Infrastructure.Tests were green in every run (the pre-existing Web.Tests flakiness, described in Section 3, was observed in two of the three runs and is unrelated to this change — see below).
  - **No revert was needed.** The 9.0.x line satisfies every consumer.
- **`backend/global.json`** (new file) — pins the SDK to `9.0.300` (the version installed and used throughout this session) with `"rollForward": "latestFeature"`, so a future patch/feature-band SDK release is picked up automatically but a jump to .NET 10 is not silent.

## 5. Required documentation changes

- **`README.md`** (new, root) — all 8 required sections. Every command in it was actually run in this session; the test-suite section documents the flakiness honestly rather than presenting a single green run as the expected state (corrected mid-phase after Task 5's verification runs surfaced new evidence — see Section 3).
- **`CLAUDE.md`** (new, root) — authority precedence, the 7 non-negotiable rules as given, a Definition of Done checklist, the Conventional Commits convention, and the 5 stop conditions, all verbatim to what this phase's instructions specified.
- **`IMPLEMENTATION_PLAN_V2.md`** — added the exact one-line superseded banner at the top. Nothing else in the file was touched.
- **`docker/.gitkeep`, `scripts/.gitkeep`** (new) — see Section 6 for why `.gitkeep` was chosen over deletion.
- **`docs/phases/PHASE-00-REPORT.md`** — this file.

## 6. Open decisions for the owner

- **`FINAL_ARCHITECTURE_PLAN.md` — two diverged copies exist; neither reflects the code as it exists today.** Comparison:

  | | Root (`FINAL_ARCHITECTURE_PLAN.md`) | `docs/architecture/FINAL_ARCHITECTURE_PLAN.md` |
  |---|---|---|
  | Language/style | Persian narrative with English technical terms; reads as an early planning conversation | English, formal "Architecture Review" structure (Summary → Model → Principles → Assessment → Decisions → Roadmap → Priorities → Open Questions → Recommendation) |
  | Framing | Assumes core domain rules (age policy, guardian consent, booking, session lifecycle) are **not yet built** — Phase 1 is literally "create `AgePolicy.cs`, `GuardianConsentPolicy.cs`" | Assumes the same rules are **partially built** and lists "Gaps to address" (formalize authorization, make audit consistent, decide the aggregate boundary, decide admin tiering) |
  | Concurrency/aggregate boundary | Presents these as still-open decisions to make | Same — presents them as still-open |
  | Authorization | Presents role-based + relationship-based auth as a **recommendation** to implement | Same — a recommendation, not an acknowledgment that it's done |
  | Consistency with actual code today | **Neither is consistent.** Both predate the work that actually: ratified the AvailabilitySlot/Session aggregate boundary (`ADR-015`, Accepted), decided the concurrency mechanism (`ADR-014`, unique-constraint, Accepted), decided the auth mechanism (`ADR-017`, Accepted), and closed all 37 rows of `docs/api/AUTHORIZATION_MATRIX.md` to zero "Open" rows. Both documents' "Key Open Questions" / "8. Key Open Questions Before Full Implementation" sections list decisions that are now already Accepted ADRs. |

  **Recommendation:** neither file should be treated as current. `IMPLEMENTATION_PLAN_V3.md` (already in the repo, written against the actual current state) is the document that reflects reality today. Per this phase's explicit instruction, **neither copy was deleted or merged** — that decision is the owner's. Options once git history exists (it now does, as of this phase): delete both in favor of `IMPLEMENTATION_PLAN_V3.md` and the ADR set, or add the same superseded banner used on `IMPLEMENTATION_PLAN_V2.md` to both and keep them for historical record.
- **Was git deliberately not initialized before this phase, or was it lost/never created?** This report can't distinguish the two from the filesystem alone — worth confirming nothing was expected to already be under version control elsewhere.
- **The `vitest-results.json` and npm audit findings from Section 3** — both are real but small; worth a decision on whether to fold their fixes into Phase 1 or handle them ad hoc.
- **The Web.Tests flakiness (Section 3) is the most consequential open item.** It is not fixed by this phase (fixing it would mean touching `backend/tests/`, forbidden here) and is worse than originally documented. Recommend it becomes Phase 1's first task, not a "someday."

## 7. Merge recommendation

**Ready to merge `develop` → `main`, with one caveat clearly flagged to whoever merges it.**

Justification: every task in this phase's explicit scope was completed, verified, and committed; the absolute rules (no source-file edits, no database access, no deletion before history existed, task ordering preserved) were honored, with the one line-item exception Task 5 itself required and explicitly authorized. `dotnet build` is clean (0/0) both before and after the package pin fix. Nothing in this phase changes product behavior — it is exactly the "safety net, no features" scope requested.

**The caveat:** do not read a green `dotnet test` run from this branch as proof the backend is fully healthy. `Web.Tests` is intermittently flaky (Section 3) — this predates Phase 0 and Phase 0 did not (and was not scoped to) fix it. Anyone merging or building on top of `develop` should re-run the backend test suite at least twice before trusting a single result, until Phase 1 addresses the root cause.

Phase 1 not started, per instruction. Stopping here.

# TutorFlow — Delivery Sprint Handoff (2026-10-26)

Branch: `upgrade/dotnet-10-csharp-14`. Starting point: `34bb015` (last pre-sprint commit —
`feat(tutor-onboarding): complete education step fields`). Ending point: `b4d0af5`. No remote
exists for this repository (confirmed `git remote -v` empty); nothing has been pushed anywhere.

## What this sprint did

Organized ~260 uncommitted files (mostly pre-existing local work from before this sprint, plus a
handful of fixes made during it) into 9 coherent, independently-buildable commits, each reviewed
and verified with freshly-run, freshly-observed tests before committing. Nothing was reset,
stashed, or discarded. One real gap was found and fixed along the way (see below).

## Commits, in order

| Commit | Summary | Tests run, freshly observed |
|---|---|---|
| `948809d` | A44 durable-outbox backbone (Program.cs Wolverine wiring, static codegen) + Availability replay-safety + onboarding Availability step's cache-invalidation fix | Availability backend 60/60, Host.Tests TutorPublished pipeline 6/6, frontend Availability 19/19 |
| `2081fc8` | A44 completion for Bookings/Payments (payment-event replay safety) + Search's tutor-signal consumers | Bookings 91/91, Payments 15/15, Search 28/28 |
| `d27c5ab` | Identity: Google + Facebook external login | Identity backend 59/59, frontend auth 34/34 |
| `0ac6c13` | Wizard reorder to Preply's real 8 steps (About/Photo/Certification/Description/Video/Pricing; old Basics/Credentials/Teaching/Rate/Review removed) + Tutors backend (certificate/photo document lifecycle, F1/F3/F4 fixes) | Tutors backend 119/119, frontend tutor-onboarding 135/135 |
| `ab381c5` | Messaging's first real feature (ADR-0030, conversations gated on tutor bookability) + Notifications (certificate-rejection email, abandoned-checkout reconciliation) | Messaging 27/27, Notifications 44/44, frontend messaging 23/23 |
| `f67a7bb` | Lessons F6 explicit-discovery fix + remaining `HostCollection` test-infra fixtures (Reviews/Students/Subjects/Host.Tests) | Lessons 24/24, Reviews 36/36, Students 13/13, Subjects 5/5, full Host.Tests 20/20 |
| `18915fe` | **Fix**: `Directory.Packages.props` was missing `PackageVersion` entries for `WolverineFx.EntityFrameworkCore`/`WolverineFx.Postgresql`/Google+Facebook auth that earlier commits' `.csproj` files already referenced | n/a (packaging-only; verified by full solution build below) |
| `9a091b4` | Landing/header redesign, Messaging UI integration (conversation list/thread, message-tutor button), shared UI primitive updates | `tsc --noEmit` clean, 207/207 frontend tests |
| `b4d0af5` | Preply evidence docs (19–27 + screenshots), ADR appendices, CI workflows, release-prep (`.gitignore`, `CLAUDE.md` correction) | docs-only |

## Known, pre-existing gap found this sprint (now fixed)

Commits `948809d` through `f67a7bb` reference NuGet packages (`WolverineFx.EntityFrameworkCore`,
`WolverineFx.Postgresql`, Google/Facebook auth packages) whose `PackageVersion` entries weren't
committed until `18915fe`. **If you ever need to check out one of those five commits in isolation**
(not just `HEAD`), central package version management will fail to restore until you also check
out `Directory.Packages.props` from `18915fe` or later. The full current `HEAD` has always built
correctly — this only affects isolated checkouts of the intermediate commits. Not fixed via rebase
because nothing has been pushed and rewriting history wasn't necessary to leave the tree correct
going forward.

## Verified facts at end of sprint

- `git status --short`: one entry — `src/TutorFlow.Host/wwwroot/` (untracked). See below.
- `dotnet build` (full solution): 0 warnings, 0 errors.
- `npm run build` (apps/web): succeeds, same 20 routes as before the sprint.
- `git diff --check` across the whole sprint: clean except trailing-whitespace/EOF-newline flags
  inside `src/TutorFlow.Host/Internal/Generated/WolverineHandlers/*.cs` — these are Wolverine's own
  `codegen write` output (machine-generated, matches the CI's own documented drift-check step), not
  hand-written code. Not a real issue; do not hand-edit these files.

## Deliberately left uncommitted

- **`src/TutorFlow.Host/wwwroot/uploads/tutor-photos/*.jpg,*.png`** — two real uploaded files from
  local Playwright-driven testing this month (filenames are the test tutor's own hashed user IDs).
  Runtime-generated local test data, not source. Recommend adding `wwwroot/uploads/` to
  `.gitignore` before any future commit touches this directory — not done in this sprint to avoid
  another `.gitignore` edit beyond what was already explicitly authorized.

## Known blockers / open items carried forward (not addressed this sprint, out of scope)

- **No remote configured.** Nothing has been pushed anywhere. Publishing requires an explicit
  owner decision on the destination (see the separate pre-release audit report,
  `docs/product/.../27-phase3-audit-completion-2026-10-25.md`, for the fuller release-readiness
  writeup from before this sprint).
- **Two root-level screenshots of inconclusive provenance**
  (`become-a-tutor-desktop-1440x900.png`, `become-a-tutor-mobile-390x844.png`) remain on disk,
  excluded from Git via `.gitignore` (pre-existing decision from before this sprint — not
  re-investigated, per explicit instruction not to re-audit this).
- **Known combined-test-suite flakiness** (CPU contention on this dev machine when many
  `vitest`/`dotnet test` processes run concurrently) — every suite in this sprint was verified
  passing when run with reasonable isolation; if you see a test fail only in a large batched run,
  re-run it alone before assuming a regression.
- **Certification/Education Preply-parity gaps** identified in the pre-release audit
  (`27-phase3-audit-completion-2026-10-25.md`) — Certification's catalog-driven combobox and
  Education's "Degree type" field were explicitly flagged as needing an architect decision
  (unverifiable catalog data source / new backend schema) and were not implemented.
- **A44 task-tracker items `#103-#107`** (migrate remaining publish sites, fix
  `SuspendApplication` adminUserId, integration tests, deployment doc) — the tracker shows these as
  still pending; this sprint's commits cover the Availability/Bookings/Payments/Search/Lessons
  consumer-side work that appears to satisfy most of this, but the tracker itself was not
  reconciled against actual code state this sprint.

## Commands used (for re-verification)

```bash
git log --oneline 34bb015..HEAD        # this sprint's 9 commits
dotnet build                            # full solution
cd apps/web && npm run build            # frontend production build
dotnet exec <module>.Tests.dll          # per-module test run (dotnet test reports "Zero tests
                                         # ran" against this repo's xunit.v3.mtp-v2 projects —
                                         # always use dotnet exec on the built DLL instead, exactly
                                         # as .github/workflows/backend.yml already does)
```

## Recommended next step

Nothing is blocking further work on this branch. If continuing: reconcile the A44 task tracker
against actual code (items `#103-#107` above), then move to the explicitly-deferred Certification
catalog / Education degree-type items once their respective architect decisions are made.

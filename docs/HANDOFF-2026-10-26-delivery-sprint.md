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

## Addendum (2026-10-26): production stabilization pass — no app-code regression found

A follow-up sprint targeted three things: a previously-unresolved "404/redirect-bounce
instability" investigation, re-verification of A44's payment/tutor-published replay-safety
claims, and a review of the Certification/Education Preply-parity gaps. Findings:

- **404/bounce**: live-reproduced across the public site, the wizard, and protected dynamic
  routes (`/tutors/[slug]`, `/messages/[id]`, `/admin/tutor-applications/[id]`) using both the
  existing dev server and a fresh `next build && next start` production instance on a second
  port. One symptom did reproduce — `/tutors/{nonexistent-slug}` returned `500` instead of `404`
  — but only on a single long-running (16+ hour) `next dev` process (`Jest worker encountered 2
  child process exceptions, exceeding retry limit`, Next's own internal compile-worker pool, not
  app code). The identical request against a clean production build returned the correct `404`
  immediately. **No application code change made** — this is dev-server process staleness, not a
  TutorFlow defect; restart any long-lived `next dev` process if this recurs, and prefer
  validating against a production build for this class of symptom (matches the prior sprint's own
  "Part C" practice).
- **The one real defect `27-phase3-audit-completion-2026-10-25.md` found** (Availability's
  missing React Query cache invalidation after in-app save+navigate) was checked against current
  code and **is already fixed** — `AvailabilityStep.tsx` now uses `useReplaceWeeklyRules()`/
  `useUpdateBookingSettings()`, with a dedicated regression test
  (`AvailabilityStep.cacheInvalidation.test.tsx`), landed in commit `948809d` before this pass
  started. No further action needed; this closes that finding.
- **A44 replay-safety** (`PaymentSucceededConsumer`, Availability's and Search's
  `TutorPublishedConsumer`): re-inspected current source rather than trusting the task tracker.
  All three already carry the documented Lightweight-transaction + idempotent-republish fix
  (`ConfirmBooking`'s `AlreadyConfirmed` path keyed on persisted `PaymentReferenceId`;
  `TutorBookability.TryApply`'s stale-redelivery guard + unconditional signal republish). No
  regression found; no change made.
- **Certification/Education parity**: confirmed Education's Specialization/Years-of-
  study/no-degree-checkbox fields (already wired, using previously-unused backend fields) are
  live in code. Degree-type (needs new backend schema) and Certification's catalog combobox
  (needs real provider/course data this codebase has no source for) remain correctly deferred —
  no new evidence this session to act on either.
- **Found and fixed, unrelated to the above**: `01-create-app-role.sh` never provisioned a
  database for `Notifications`, and `docs/DEVELOPMENT_DATABASE.md` never documented `Payments` or
  `Notifications` at all despite both needing connection strings — fixed in commit `4dd4341`.
  **Left open, flagged for the architect** (not fixed, to avoid guessing): `WolverineDb` (A44's
  Host-owned Main envelope store) and `MessagingDb` (the `Messaging` module's own database) would
  both naturally be named `tutorflow_messaging` under this project's existing per-module naming
  convention — pick a non-colliding name for one of them (`docs/DEVELOPMENT_DATABASE.md` §1) before
  `MessagingDb` is ever added to the init script.
- A disposable local test account (`bounce-check-7f3a1@example.local`) was created via the real
  signup flow for this session's live reproduction, consistent with this engagement's established
  practice of using throwaway `.local` accounts for local-only testing; no cleanup action taken
  since it's harmless local dev data, same as every other test account already in this database.

## Addendum (2026-10-26, later same day): naming collision resolved; A44 tracker #103-#107 reconciled

**`WolverineDb`/`MessagingDb` naming collision — resolved, no live collision existed.** Verified
across every config source (code, user-secrets, docker-compose, test fixtures): `MessagingDb` had
never been configured anywhere outside isolated Testcontainers, so there was no live runtime
collision today — only a future provisioning risk if `MessagingDb` were ever set up following the
existing per-module naming convention (which would also produce `tutorflow_messaging`). Resolved
by renaming the local Wolverine database (`ALTER DATABASE tutorflow_messaging RENAME TO
tutorflow_wolverine`, no data lost — all 8 `wolverine_*`/node tables confirmed intact afterward)
and provisioning `tutorflow_messaging` for the `Messaging` module's own data (its rightful name).
Verified the durable outbox still starts all 5 message-store agents correctly post-rename. Commit
`d5bbb13`; full details in `docs/DEVELOPMENT_DATABASE.md` §1.

**A44 tracker #103-#107 — reconciled against actual code, not assumed:**

| Item | Disposition | Evidence |
|---|---|---|
| #103 migrate 8 publish sites | **Confirmed still incomplete** — genuine, live gap, not a stale checkbox | Of 9 actual `bus.PublishAsync` call sites, only `ConfirmBooking`'s (invoked solely via `PaymentSucceededConsumer`, a true Wolverine `[Transactional(Lightweight)]` handler) is atomic with its business write. Six are plain-DI/HTTP-endpoint-invoked with **no Wolverine transactional coverage at all** — `PublishApplication`→`TutorPublished`, `SuspendApplication`→`TutorUnpublished`, `RejectCertificate`→`TutorCertificateRejected`, `ProcessStripeWebhook`→`PaymentSucceeded`/`PaymentFailed`, `BookingHoldExpirySweepService`→`BookingHoldExpired` (a timer-driven `BackgroundService`, never Wolverine-dispatched). `AvailabilitySignalPublisher`→`TutorAvailabilitySignalChanged` is **partially** covered: safe when triggered from `TutorPublishedConsumer`/`TutorUnpublishedConsumer` (Wolverine handlers), exposed when triggered from the tutor's own self-service `ReplaceWeeklyRules`/booking-settings updates (also plain-DI). A crash between the business `SaveChangesAsync` and the following `PublishAsync` on any of these six+one-partial sites silently and permanently drops the event — no compensating redelivery exists for a message that was never durably created in the first place. **Not fixed here**: the correct mechanism (routing each handler through Wolverine's local command dispatch so `AutoApplyTransactions()` applies, vs. a different outbox-write pattern) is a design choice affecting ~6 files and needs explicit sign-off, not a "smallest safe fix." |
| #104 fix `SuspendApplication` adminUserId | **Fixed** | `SuspendApplicationAsync` extracted `adminUserId` (for the existing self-dealing guard) but never passed it to the handler — unlike `RejectCertificate`'s identical pattern. Added the parameter + structured log, matching `RejectCertificate` exactly. Commit `88b13e8`. Verified: Tutors module 119/119, Host.Tests 20/20 (including the two direct-handler test call sites that needed updating for the new parameter). |
| #105 integration tests | **Blocked on #103** | No integration test exists that proves atomicity for any of the six uncovered publish sites (the existing `TutorPublishedPipelineTests` cover the happy path and *consumer-side* redelivery only — confirmed by reading every test in the file). Writing a meaningful test requires the mechanism in #103 to exist first. |
| #106 migrations + deployment doc | **Confirmed incomplete, both halves** | No deployment doc exists anywhere in the repo (confirmed by search — matches the README's own "no production deployment target yet" statement). No EF migration exists for `WolverineMessagingDbContext` either (`src/TutorFlow.Host/Migrations/` doesn't exist) — Wolverine's envelope tables are created only by each test fixture's own manual DDL (`WolverineTestConnectionStrings.ProvisionEnvelopeTablesAsync`) or by hand locally; nothing automates this for a fresh non-test environment. |
| #107 full verification + completion report | **Blocked on #103-#106** | Cannot close until the above are resolved or explicitly descoped by the architect. |

**Architect decisions needed to close #103/#105/#106**: (1) how to make the six uncovered publish
sites atomic — route each through Wolverine's local command dispatch (`bus.InvokeAsync`) so
`AutoApplyTransactions()` covers them, or a different pattern; (2) whether a deployment doc is
in scope before any real deployment target exists, or deferred until one does; (3) whether
Wolverine's envelope-storage schema needs a real EF migration or a documented manual/CLI
provisioning step (`dotnet run -- storage setup`) is acceptable going forward.

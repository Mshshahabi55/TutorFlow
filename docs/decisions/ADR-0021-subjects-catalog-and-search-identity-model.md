# ADR-0021: Subjects Real Catalog, ID-Based Search Identity, and First Cross-Module Event Pipeline

**Status:** Accepted

## Context

The first `Search` implementation (this session, 2026-10-04) filtered subject/specialty by
free-form display-name string, because `Tutors`' already-implemented `TutorApplication` stored
them as strings and `Subjects` was an empty stub with no catalog. That was reviewed and
corrected the same day: optimizing the product around an incomplete module's accidental storage
shape, rather than evolving that module to support the actual product requirement (Preply-level
Search parity — subjects/specialties/languages as real reference data with stable identity,
plus country-of-birth, Professional Tutor, and Super Tutor filtering/badges, per
`docs/design/PREPLY_SEARCH_PARITY_SPEC.md`), was backwards.

## Decision

### 1. `Subjects` becomes a real (minimal) catalog

`Subjects` now owns three reference entities, each with a stable `Guid` (or, for `Language`, the
already-established `LanguageCode` string) identity, a `Code` (slug) distinct from `DisplayName`
(the localization-ready seam — only `DisplayName` would ever vary per UI locale), `IsActive`, and
`SortOrder`:

- `Subject` (e.g. "english")
- `Specialty`, FK'd to a `Subject` within `Subjects`' own schema (e.g. "exam-prep" under "english")
- `Language` — unchanged from ADR-0020, the admin-curated enabled-languages reference list

No admin CRUD exists (Identity has no Admin role yet, A35) — V1 content is migration-seeded
reference data, explicitly not business/user data, consistent with "don't build an enormous
CMS." Public read endpoints (`GET /api/subjects`, `/api/specialties?subjectId=`, `/api/languages`)
are the only surface; Guests can browse (`ROLES_AND_PERMISSIONS.md`).

### 2. `Tutors` references the catalog by ID, not name

`TutorApplication.Subjects`/`Specialties`/`Languages` (`List<string>`) are replaced with
`SubjectIds`/`SpecialtyIds` (`List<Guid>`) and `SpokenLanguages` (`List<TutorSpokenLanguage>`,
finally implementing ADR-0020's design). `Tutors` never references `Subjects`' schema directly
(no `ProjectReference`, no FK) — the `Guid`/`LanguageCode` values are just data, validated at the
UI layer by only ever offering values fetched from `Subjects`' own public catalog endpoints.

Two new tutor attributes, both **platform-determined, never tutor-self-reported**:
`CountryOfBirthCode` is the one exception (self-reported, like any other profile fact).
`IsProfessionalTutor`/`IsSuperTutor` are booleans with a private setter and a dedicated domain
method (`SetProfessionalTutorStatus`/`SetSuperTutorStatus`) — no caller exists yet, because the
real eligibility computation (Preply's course-or-certificate criteria; the 9-metric rolling-90-day
Super Tutor criteria) is explicit future work, not built here per instruction. The *badge state*
is real; the *algorithm that sets it* is not.

### 3. `Search`'s read model and API follow the same identity model

`TutorSearchDocument.SubjectIds`/`SpecialtyIds` are `Guid` lists; the free-text `q` filter is
reserved for keyword matching against `DisplayName`/`Headline` only — catalog concepts are never
matched by name. `CountryOfBirthCode`/`IsProfessionalTutor`/`IsSuperTutor`/`LessonCount` are new
fields (the last always `0` until `Lessons`/`Bookings` publish something, §5 below).

Resolving a `SubjectId`/`SpecialtyId` to a display name for rendering is the **frontend's** job —
it calls `Subjects`' public catalog endpoints directly and joins client-side. `Search` does not
denormalize catalog display names into its own read model; doing so would require consuming a
second event stream (`Subjects`' own catalog-changed events) that isn't built yet (§5).

### 4. First real Wolverine introduction (resolves A34)

`Tutors` publishes `TutorPublished` (`TutorFlow.Shared`) via Wolverine's `IMessageBus` whenever
`TutorApplication.Publish()` succeeds; `Search` consumes it (a constructor-injected, DI-discovered
handler, `TutorPublishedConsumer`) and upserts its own `TutorSearchDocument` row. This is the
first cross-module event pipeline in the codebase — proven end-to-end by
`tests/TutorFlow.Host.Tests/TutorPublishedPipelineTests.cs`, which boots both modules' real
schemas against one Testcontainers Postgres database and asserts the row lands correctly.

Two things this pipeline deliberately does **not** yet have, both flagged as follow-ups rather
than silently accepted as final:

- **No durable transactional outbox.** Wolverine's default local in-process transport is used;
  a crash between `Tutors`' `SaveChangesAsync` and the publish is possible. A Postgres-backed
  outbox (`WolverineFx.EntityFrameworkCore`) is the documented long-term approach
  (`CLAUDE.md`, `ADR-0015` "Messaging infrastructure").
- **No static codegen.** Wolverine runs in `TypeLoadMode.Dynamic` (Roslyn-based runtime
  compilation, via the referenced `WolverineFx.RuntimeCompilation` package) for simplicity.
  Pre-generating static handler code and dropping the Roslyn dependency
  (`dotnet run -- codegen write` + `TypeLoadMode.Static`) is the better production choice.
- A narrow, explicit exception to Wolverine's default handler-codegen policy was required:
  `opts.CodeGeneration.AlwaysUseServiceLocationFor<SearchDbContext>()`, because `AddDbContext`
  registers `DbContextOptions<T>` via an opaque lambda factory that Wolverine's strict-by-default
  codegen otherwise refuses to resolve. Allow-listed for this one type only, not a global policy
  relaxation.

`PublishApplication.Handler` (the publish-triggering handler) deliberately has **no mapped HTTP
endpoint** — the real production trigger is an admin-approval action, which needs an Admin
role/policy that doesn't exist in `Identity` yet (`OPEN_QUESTIONS.md` A35). Building that is out
of this task's scope; the handler is fully implemented and tested independent of who calls it.

### 5. What's designed but not implemented

Four more producer events are identified and documented (not built) — see
`docs/design/PREPLY_SEARCH_PARITY_MATRIX.md` for the full status per field:

- `Subjects` → `Search`: catalog-changed events, needed before `Search` can denormalize display
  names itself (currently the frontend's job, §3).
- `Availability` → `Search`: the coarse `HasUpcomingAvailability` signal (already anticipated by
  `ADR-0007`/`SEARCH_SPEC.md`).
- `Reviews` → `Search`: rating aggregate/review count.
- `Lessons`/`Bookings` → `Search`: lesson count.

None of these block the `Search` contract itself — every corresponding field and filter already
exists and functions correctly against an empty/zero-default value; wiring a producer later
requires no API or frontend change.

## Consequences

- `docs/domain/SEARCH_SPEC.md` is rewritten to the ID-based contract (supersedes its original
  string-based version).
- `docs/domain/DOMAIN_MODEL.md`'s `Subjects`/`Tutors`/`Search` rows updated.
- `OPEN_QUESTIONS.md` A34 (Wolverine) resolved; new items tracked for the four undone producer
  pipelines and the durable-outbox/static-codegen follow-ups.
- No `ProjectReference` was added between any modules; `Specialty`'s FK to `Subject` is
  intra-module (both in `Subjects`' own schema), not a cross-module reference.
- This does not widen `ADR-0014`'s read-only query-contract pattern — the new cross-module
  mechanism here is an integration event (Wolverine), the sanctioned mechanism for anything with
  a side effect, consistent with `ADR-0001`.

## Addendum (F6 production reliability fix — static codegen implemented)

§4 flagged two gaps as "not silently accepted as final": no durable transactional outbox, and no
static codegen. This addendum closes the second; the first remains open — see `OPEN_QUESTIONS.md`
A44, now split accordingly.

**Root cause, confirmed with evidence, not assumption:** `TypeLoadMode.Dynamic` compiles each
message type's handler chain lazily, via Roslyn, on its first dispatch. Two different message
types' first-ever compilation racing concurrently corrupts JasperFx's internal frame-arranging
state — a known upstream bug class (JasperFx/wolverine#1734). Reproduced independently against
two unrelated pipelines (`TutorCertificateRejected`, and separately `TutorPublished`/
`TutorAvailabilitySignalChanged` in `TutorPublishedPipelineTests`) under repeated full-suite runs,
confirming this was never specific to one handler — it was a property of Dynamic mode itself,
affecting the whole messaging pipeline.

**Fix:** `opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Static`, applied unconditionally in
every environment (including tests) — not gated behind `IsProduction()`, so the code the test
suite exercises is the code that ships. Handler dispatch code for all 8 integration-event types
is pre-generated via `dotnet run --project src/TutorFlow.Host -- codegen write` into
`src/TutorFlow.Host/Internal/Generated/WolverineHandlers/`, committed as ordinary C# (not
hand-authored or hand-patched), and verified for drift in CI (`backend.yml`).

**Two follow-on fixes `codegen write`'s ahead-of-time, every-discovered-type pass surfaced, that
Dynamic mode's lazy, only-on-actual-dispatch compilation had never exercised:**
1. Wolverine's default "type name ends with Handler" discovery convention was matching every
   ordinary CQRS feature `Handler` class across every module (not just the 10 real `*Consumer`
   classes), misreading each one's first method parameter as "the message type." Fixed via
   `opts.Discovery.CustomizeHandlerDiscovery(query => query.Excludes.WithNameSuffix("Handler"))`
   — every real consumer already matches the still-active "Consumer" suffix rule, so this is a
   pure subtraction of a false-positive match, not a narrowing of real coverage. All 10 real
   consumers are additionally marked `[WolverineHandler]` as the documented "more deliberate
   selection" marker.
2. `RunJasperFxCommands(args)` (required to replace `app.Run()` so the `codegen write` verb is
   reachable at all) exercises Wolverine's automatic "extension assembly" discovery
   (`WolverineOptions.DiscoverAndApplyExtensions`) for the first time, which throws
   `CultureNotFoundException` under this project's `InvariantGlobalization=true` when it
   encounters culture-named satellite-resource folders in the build output (present in every test
   project's own output, from the test framework itself) — a version-specific bug in the
   installed JasperFx 2.79.1 (upstream's current main branch already special-cases this). Fixed
   via `ExtensionDiscovery.ManualOnly` on `UseWolverine` — correct regardless of the upstream bug,
   since this app has zero real Wolverine-extension NuGet packages to auto-discover in the first
   place.

**Verified, not merely asserted:** 10 consecutive clean full-suite runs of
`TutorFlow.Modules.Tutors.Tests` (119 tests each), 5 of `TutorFlow.Host.Tests` (18 tests each,
including the previously-flaky `TutorPublishedPipelineTests`), 3 of
`TutorFlow.Modules.Notifications.Tests` (44 tests each) — zero recurrences of the race, zero
`MissingPreBuiltTypesException`, zero `CultureNotFoundException`. Cross-checked against Bookings/
Lessons/Payments/Search/Availability too; no new Static-mode-related failures in any of them (and
Search's own long-standing, unrelated Serilog `ReloadableLogger` flake measurably improved —
7/28 failing versus a previously-confirmed 20/28 — a side effect of no longer paying Roslyn's
startup cost, not something this fix targeted or can claim credit for by design).

**`WolverineFx.RuntimeCompilation` removed** from `TutorFlow.Host.csproj` — confirmed via
repository-wide search to have zero direct code references anywhere (it only ever provided the
Roslyn compiler Dynamic mode needed); full solution build and all of the above test runs succeed
without it.

**Known limitation, not fixed here and out of scope for F6:** Wolverine's local transport remains
at-least-once, not exactly-once, regardless of TypeLoadMode — confirmed by observing a single
`TutorCertificateRejected` HTTP call produce two identical notification sends in one live run,
and a `BookingHoldExpired`-driven duplicate-delivery-triggered error-queue entry in Bookings'
test suite (pre-existing behavior, not introduced by this change — `SendAbandonedCheckoutRecoveryEmail`
already has its own claim/lease idempotency guard for exactly this reason; `SendCertificateRejectedEmail`
deliberately does not, by prior design decision). No idempotency mechanism or delivery-semantics
change was added as part of this fix, per explicit instruction — the durable outbox
(`WolverineFx.EntityFrameworkCore`) remains the documented path to close this gap, tracked
separately in `OPEN_QUESTIONS.md` A44.

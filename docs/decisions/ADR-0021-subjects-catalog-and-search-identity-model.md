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

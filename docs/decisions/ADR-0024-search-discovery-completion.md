# ADR-0024: Search & Discovery — Closing the Favorites/Availability/Suspension/Actions Gaps

**Status:** Accepted

## Context

A ground-truth audit (2026-10-05) found that most of the "Search & Discovery" product surface
is **already built and matches Preply parity well**: subject/specialty/language/native-speaker/
country/price/rating/Professional/Super filters, keyword search with trigram relevance ranking,
sort, pagination (stable under ties), shareable URL state, and a mobile filter drawer are all
implemented and tested (`docs/design/PREPLY_SEARCH_PARITY_MATRIX.md`, `SEARCH_SPEC.md`). This ADR
is **not** a greenfield design — it closes five specific, previously-documented-as-deferred gaps,
re-verified against a fresh live Preply benchmark this phase:

1. **Favorites are entirely disconnected from search results.** `TutorResultCard` has no heart
   icon at all (Parity Matrix: "explicitly excluded from this task"). The existing Students
   `FavoriteTutor` feature (add/remove/status) is single-`tutorUserId`-keyed only — no bulk
   lookup exists, so wiring a heart into a 20-card result grid naively would mean 20 round-trips.
2. **`HasUpcomingAvailability` is permanently `false`.** `Availability` has the exact computation
   needed (`GetTutorAvailabilityPreview`, already used by the Tutor Profile page) but nothing
   ever triggers it for the search read model — no `Tutors → Availability` consumer exists at all
   (`TutorBookabilityProjector.ApplyBookabilityChangedAsync` has zero callers), so
   `TutorBookabilityRecords` is permanently empty and the preview always answers "not bookable."
3. **Suspended tutors stay visible in search forever.** `TutorApplication.Suspend()` is a complete
   domain method with no caller anywhere (no endpoint — blocked on Identity's missing Admin role,
   `OPEN_QUESTIONS.md` A35 — same precedent as `Publish()` before this phase) and fires no event,
   so a suspended tutor's `TutorSearchDocument.IsPublished` never flips back to `false`.
4. **`TutorResultCard` has no Book Trial or Message CTA**, and no Favorite CTA — only a "View
   profile" link (Parity Matrix: both "deferred to Tutor Profile phase" / "not implemented,
   Messaging is still a stub"). Both of those blockers are now resolved (Tutor Profile and
   Identity/Auth phases are complete), so this is now the natural next increment, not scope creep.
5. **`IsAcceptingNewStudents`** exists on `TutorApplication` (with a working domain setter) but is
   never relayed through `TutorPublished` into the search read model, so search cards cannot
   reflect it the way the Tutor Profile page already does.

Live Preply re-benchmark this phase (full findings in the implementation report) confirmed the
existing filter taxonomy, card field set, and pagination approach already match the production
API's own facets closely; it did not surface anything requiring a redesign of what's already
built — only confirmed the five gaps above are real product gaps, not acceptable permanent state.

## Decisions

### 1. Availability → Search: a real, event-driven `HasUpcomingAvailability`

Two new shared integration events in `TutorFlow.Shared`:

```csharp
public sealed record TutorAvailabilitySignalChanged(Guid TutorUserId, bool HasUpcomingAvailability, DateTimeOffset OccurredAtUtc) : IIntegrationEvent;
public sealed record TutorUnpublished(Guid TutorUserId, DateTimeOffset OccurredAtUtc) : IIntegrationEvent;
```

**`Availability` gains two new consumers** (`Consumers/TutorPublishedConsumer.cs`,
`Consumers/TutorUnpublishedConsumer.cs`), extending the exact `Tutors → Search` precedent
(ADR-0021) to a second subscriber of the same `TutorPublished` event:

- On `TutorPublished`: calls the **existing** `TutorBookabilityProjector.ApplyBookabilityChangedAsync(tutorUserId, isBookable: true, ct)` (this finally gives that projector its first real caller — ADR-0011's original design, left unwired until now), then computes and publishes the availability signal.
- On `TutorUnpublished`: flips bookability to `false` via the same projector — a genuine correctness fix beyond Search alone: without this, `ITutorAvailabilityQuery`/`GetTutorSlots` would keep treating a suspended tutor as bookable.

**The signal computation itself reuses `GetTutorAvailabilityPreview.Handler` unchanged** (DI-injected into a new, tiny `AvailabilitySignalPublisher` internal service) — the exact same weekly-rules + time-off + `IActiveBookingIntervalsQuery` logic the Tutor Profile page already shows to guests. No new slot-computation logic is written; this is pure reuse, per Kit-reuse-first.

**Recompute triggers**, beyond initial publish: the signal is republished after each of the four self-service Availability mutation handlers (`ReplaceWeeklyRules`, `AddTimeOff`, `RemoveTimeOff`, `UpdateBookingSettings`) successfully saves — these are exactly the actions that could change whether a tutor has an upcoming open slot.

**Deliberately not a recompute trigger**: a specific slot being consumed by an actual booking/hold. `Bookings` does not publish anything `Availability` could subscribe to, and adding one would mean either a live cross-module query (forbidden — `ADR-0007`) or indexing Bookings' full hold/booking stream into a third module (disproportionate for a "soon" signal). This matches `SEARCH_SPEC.md` §5's own framing exactly: *"a real 'bookable at 3pm Tuesday' filter requires either a live cross-module query (forbidden) or indexing every tutor's full slot grid (disproportionate)."* The signal is honest about what it is — "this tutor currently maintains an open weekly schedule with no near-term open slot", not a live booking-availability guarantee — and is computed from real data at well-defined trigger points, not faked.

**Window**: reuses the tutor's own already-configured `BookingSettings.MaxBookingWindowDays` (the same horizon `GetTutorSlots`/`GetTutorAvailabilityPreview` already use) rather than inventing a second, separate "search lookback window" concept. This resolves `OPEN_QUESTIONS.md` #37 with the narrow, justified default the item itself said could ship without further Product Owner input — documented here, not silently assumed. `OPEN_QUESTIONS.md` updated accordingly.

**`Search` gains two new consumers** mirroring the existing `TutorPublishedConsumer` pattern exactly: `TutorAvailabilitySignalChangedConsumer` (updates `HasUpcomingAvailability` on the matching document; no-ops if the document doesn't exist yet) and `TutorUnpublishedConsumer` (sets `IsPublished = false`). No migration needed for the availability signal — the column already exists, it was simply never written to.

### 2. Suspension removes search visibility

`Tutors` gains `Features/SuspendApplication/SuspendApplication.cs` — a handler-only feature with
**no mapped HTTP endpoint**, explicitly following the exact precedent `PublishApplication.cs`
already established for the same reason (admin-approval trigger needs a role Identity doesn't
have yet, `OPEN_QUESTIONS.md` A35). It calls `TutorApplication.Suspend(reason, clock)` and
publishes `TutorUnpublished`. This is required to make "suspended tutors excluded" (explicitly
in this phase's test checklist) real and testable, the same way `PublishApplicationTests.cs`
already proves the publish path independent of who ends up calling it in production.

### 3. Favorites: one bulk status endpoint, reusing everything else

`Students` gains exactly one new endpoint: `GET /api/students/me/favorites?tutorUserIds=a,b,c`
→ `{ favoritedTutorUserIds: Guid[] }` (only the subset of the requested IDs that are favorited).
This is the **only** new Favorites code. Add/remove/single-status stay untouched and are reused
as-is from the frontend's existing `tutor-profile/api.ts` — "must not invent a second favorite
system" is satisfied by adding exactly one bulk-read capability to the existing feature, not a
parallel one. The existing `IX_FavoriteTutors_StudentUserId` index already makes this bulk query
efficient (`WHERE StudentUserId = @id AND TutorUserId = ANY(@ids)`); no new index needed.

### 4. `IsAcceptingNewStudents` relay

Added to `TutorPublished`, `TutorSearchDocument` (new migration), `TutorSearchResultResponse`,
and the frontend `TutorSearchResult` type. Cards show a "Not accepting new students" treatment
and disable the Book Trial CTA — **visible, not hidden** — exactly mirroring the Tutor Profile
page's own existing precedent (ADR-0022, `PREPLY_TUTOR_PROFILE_PARITY_SPEC.md` §2/§11). Building
the tutor-facing toggle endpoint itself is out of scope (that's `Tutors` module feature work, not
Search) — this decision only relays data that already exists.

### 5. `TutorResultCard` gains real Favorite / Message / Book Trial CTAs

Wired exactly the way `BookingPanel` (Tutor Profile) already does it — the same shared mechanism
mandated by the Identity/Auth phase, extended to its second call site, not reinvented:

- `FindTutorsView` gets one shared `AuthGateModal` instance + a `requireAuth(action)` callback,
  `returnTo` set to the **current search URL** (pathname + query string) so login/signup returns
  the user to their exact filtered search, not just the homepage — satisfies "preserve
  return-to/search context where practical."
- Real `useAuth()` computes `isStudent = isAuthenticated && roles.includes('Student')`. A
  **pure**-Tutor viewer (Tutor role, no Student capability) sees price/info only, CTAs hidden —
  identical dual-role-safe rule to `BookingPanel`'s `isTutorViewer && !isStudent` fix from the
  Identity/Auth phase, inlined the same way (two lines; not worth extracting a shared helper for
  two call sites doing two lines of work each).
- **Favorite**: guest → auth gate; Student → real toggle via the existing (unmodified)
  add/remove mutations, bulk status fetched once per results page via the new endpoint.
- **Message**: guest → auth gate; Student → navigates to `/messages`, identical to
  `BookingPanel` (no per-conversation deep link exists yet — not inventing one here).
- **Book Trial**: guest → auth gate; not-accepting → disabled with label; Student → navigates to
  `/tutors/{slug}#tutor-availability`, the exact same non-committal destination `BookingPanel`
  already uses (no booking-creation flow exists — out of scope, not touched).

### 6. Caching: deliberately not added this phase

`/api/search/tutors` is not given an output-cache policy. Reasoning, per "evaluate caching based
on actual query characteristics" rather than reflexively applying the Kit's HybridCache default:
the endpoint is highly parameterized (≈15 independent filter dimensions), so cache-key design
(`SetVaryByQuery` across all of them) would be real effort against a hit-rate that's unproven to
be a problem at V1 scale — no current performance complaint exists, and the existing GIN/trigram
indexes already make the direct-Postgres path fast. Revisit if/when real traffic data justifies
it; not speculative infrastructure.

### 7. SEO: deliberately not built

Live Preply research this phase confirmed Preply's actual SEO strategy is thousands of static,
pre-rendered landing pages per subject×specialty×native-speaker×city combination — a scale of
investment with no current TutorFlow business justification. `/find-tutors` stays one
client-rendered, query-string-parameterized page with static page-level metadata. No
robots/canonical/indexing work is added. This is a deliberate, documented scope exclusion, not an
oversight — building it now would be exactly the "speculative SEO architecture" this phase was
explicitly told not to add.

## Consequences

- `TutorPublished`'s shape changes (new `IsAcceptingNewStudents` field) — a breaking change to
  its one existing consumer (`Search`'s `TutorPublishedConsumer`) and to
  `TutorPublishedPipelineTests.cs`, both updated alongside this ADR.
- `Availability` becomes a Wolverine-discovered assembly in `TutorFlow.Host/Program.cs` (joining
  `Search`) — its `AvailabilityDbContext` needs the same `AlwaysUseServiceLocationFor` allow-list
  entry `SearchDbContext` already has, for the same reason (opaque `AddDbContext` factory).
- `TutorPublishedPipelineFixture` (the one Host-level test fixture that boots a real cross-module
  Wolverine pipeline) needs `AvailabilityDb` added to its configured connection strings and
  migrated schemas, or its own existing assertions would start executing against a module that
  now also consumes the event it's testing.
- No new modules, no competing architecture, no new frontend state-management system. Five
  modules touched (`Tutors`, `Availability`, `Search`, `Students`, `Host`) via the exact existing
  cross-module event/query-contract conventions (ADR-0007, ADR-0014, ADR-0021).

### 8. Found and fixed during live verification: every default search request was a 400

Live browser verification against the real backend (not mocks) surfaced a pre-existing, unrelated
bug: the frontend's `SortOption` UI values (`relevance`, `priceAsc`, …) never matched the
backend's PascalCase `SearchTutors.SortOption` enum names (`Relevance`, `PriceAsc`, …), and
ASP.NET's `[AsParameters]` query-string enum binding is case-sensitive. Since `filtersToApiParams`
always sends `sort`, **every** search request — including the default, filter-less page load —
was silently returning `400 Bad Request`, and the frontend's existing error state ("We couldn't
load tutors") was the only user-visible symptom. No unit test caught this because every existing
test either mocks the API client or calls the live endpoint directly with already-correct casing
(e.g. `?sort=PriceAsc`) — none exercised the frontend's actual default value end-to-end. Fixed in
`filterSerialization.ts` with an explicit `API_SORT_VALUES` map; regression-tested. This is the
reason this phase's own instruction to verify against a *real* backend, not just mocks, mattered.

## Open follow-ups (not resolved here)

- `Reviews → Search` and `Lessons/Bookings → Search` producers (rating/review count, lesson
  count) — still not built; out of scope, unrelated to this phase's five gaps.
- `specialtyIds` AND-vs-OR semantics (`OPEN_QUESTIONS.md` #36) — unchanged, still open.
- Professional/Super Tutor eligibility *computation* — unchanged, still explicit future work.

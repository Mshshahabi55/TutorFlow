# Search Module — V1 Tutor Search Contract

**Status:** Implemented (query/filter/sort/pagination logic, tested). Strategy (`ADR-0007`),
spoken-language ownership (`ADR-0020`), and the catalog/identity model + first event pipeline
(`ADR-0021`) are the three ADRs this spec builds on. Supersedes this document's original
string-based version, corrected the same day it was written — see `ADR-0021`'s Context section.

## 1. Scope

This defines the concrete v1 "Find Tutors" search/filter contract: what the UI sends, what the
API accepts, what the read model contains, and how they map to each other — modeling the full
Preply-parity product surface (`docs/design/PREPLY_SEARCH_PARITY_MATRIX.md`), not the accidental
shape of any one module's storage.

## 2. Three distinct shapes — do not conflate

| Layer | What it is | Owner |
|---|---|---|
| **UI filter state** | The frontend's filter panel state (`SearchFilters`) — may include UX-only conveniences (e.g. a debounced free-text box) that don't map 1:1 to API params | Frontend |
| **API query** | The `GET /api/search/tutors` query-string contract below — the wire shape | `Search` (public contract) |
| **Domain data** | `Subject`/`Specialty`/`Language` (reference, `Subjects`), `TutorApplication` (`Tutors`, the source of truth for a tutor's own `SubjectIds`/`SpecialtyIds`/`SpokenLanguages`/`CountryOfBirthCode`/badges), `Review` (`Reviews`), `AvailabilityRule`/`TimeOff` (`Availability`) | `Subjects` / `Tutors` / `Reviews` / `Availability` |
| **Search read model** | `TutorSearchDocument` — `Search`'s own denormalized projection, upserted from `TutorPublished` (`ADR-0021`) | `Search` |

Catalog identity (subject/specialty) is **always** a `Guid`, end to end — the API query, the read
model, and `Tutors`' own domain model all use the same ID, never a display-name string. Display
names for rendering are resolved by the **frontend**, directly against `Subjects`' public catalog
endpoints (`GET /api/subjects`, `/api/specialties`, `/api/languages`) — `Search` does not
denormalize them (that would require consuming a second, not-yet-built event stream from
`Subjects`; see `ADR-0021` §5).

## 3. `TutorSearchDocument` (read model)

```csharp
internal sealed class TutorSearchDocument
{
    public Guid TutorUserId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Headline { get; set; } = "";
    public string? PhotoUrl { get; set; }
    public List<Guid> SubjectIds { get; set; } = [];
    public List<Guid> SpecialtyIds { get; set; } = [];
    public List<string> SpokenLanguageCodes { get; set; } = [];
    public List<string> NativeLanguageCodes { get; set; } = [];
    public string? CountryOfBirthCode { get; set; }
    public bool IsProfessionalTutor { get; set; }
    public bool IsSuperTutor { get; set; }
    public int HourlyRateMinorUnits { get; set; }
    public string Currency { get; set; } = "EUR";
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int LessonCount { get; set; }
    public bool HasUpcomingAvailability { get; set; }
    public bool IsPublished { get; set; }
    public DateTimeOffset PublishedAtUtc { get; set; }
}
```

Populated exclusively by `TutorPublished` today (`Tutors` → `Search`, `ADR-0021`) — `AverageRating`/
`ReviewCount` (`Reviews`), `HasUpcomingAvailability` (`Availability`), and `LessonCount`
(`Lessons`/`Bookings`) stay at their zero/empty default until those three producers are built
(designed, not implemented — `PREPLY_SEARCH_PARITY_MATRIX.md`).

## 4. API query: `GET /api/search/tutors`

| Param | Type | Required | Notes |
|---|---|---|---|
| `q` | `string?` | No | Free-text, matched against `DisplayName`/`Headline` via PostgreSQL trigram/full-text (`ADR-0007`) — keyword search only, never a catalog-identity match |
| `subjectId` | `Guid?` | No | Exactly one subject per search — Preply-parity "I want to learn" pattern; multi-subject filtering is a `P2` enhancement |
| `specialtyIds` | `Guid[]?` | No | Zero or more, comma-separated; `AND`-matched — **approximate**, `[OPEN — Product Owner]`, `OPEN_QUESTIONS.md` #36 |
| `spokenLanguageCodes` | `string[]?` | No | ISO 639-1 codes, comma-separated; `OR`-matched (tutor speaks any one of them) |
| `nativeSpeakerOf` | `string?` | No | Single ISO 639-1 code; matches only `Native`-proficiency entries |
| `countryOfBirthCode` | `string?` | No | ISO 3166-1 alpha-2; exact match. `400` if not exactly 2 characters |
| `professionalTutor` | `bool?` | No | `true` filters to `IsProfessionalTutor` only; omitted/`false` applies no filter |
| `superTutor` | `bool?` | No | Same pattern, `IsSuperTutor` |
| `priceMinMinorUnits` | `int?` | No | EUR minor units (V1); `400` if negative |
| `priceMaxMinorUnits` | `int?` | No | `400` if negative or `< priceMinMinorUnits` |
| `minRating` | `decimal?` | No | `0.0`–`5.0`; `400` outside that range |
| `availableSoon` | `bool?` | No | Maps to `HasUpcomingAvailability` — see §5 for why this is coarse |
| `sort` | `enum?` | No | `Relevance` (default), `RatingDesc`, `PriceAsc`, `PriceDesc`, `Newest`. Every branch has a deterministic `TutorUserId` tie-breaker (hardening-review fix — pagination is not stable without one) |
| `page` | `int?` | No | 1-based, default `1` |
| `pageSize` | `int?` | No | Default `20`, max `50` — `400` above the max, never silently clamped |

Response: `{ items: TutorSearchResult[], page, pageSize, totalCount }`. `TutorSearchResult` is a
projection DTO — `displayName`, `headline`, `photoUrl`, `subjectIds`, `specialtyIds`,
`spokenLanguages` (code+proficiency pairs), `countryOfBirthCode`, `isProfessionalTutor`,
`isSuperTutor`, `hourlyRateMinorUnits`, `currency`, `averageRating`, `reviewCount`, `lessonCount`,
`hasUpcomingAvailability` — never the raw `TutorSearchDocument`.

## 5. Relevance ranking and availability filtering

**Relevance** (the default sort, and Preply's own "Our top picks" default per
`PREPLY_SEARCH_PARITY_SPEC.md`): when `q` is present, ranks by `pg_trgm` trigram similarity to
`DisplayName` (the same extension/index the `q` filter itself uses), then by rating as a
tie-breaker. When `q` is absent, falls back to rating-descending — there's no text to rank
against. This is a V1-reasonable approximation of "top picks," not a claim of parity with
Preply's actual (undisclosed) ranking algorithm.

**Availability** is deliberately coarse: a real "bookable at 3pm Tuesday" filter requires either
a live cross-module query (forbidden — `ADR-0007`) or indexing every tutor's full slot grid
(disproportionate event volume/staleness risk for V1). `HasUpcomingAvailability` (any open slot in
the next N days, exact window `[OPEN — Product Owner]`) is the V1 answer; a precise date/time
filter is `P2`/`P3`.

## 6. Pagination & sorting

Offset-based (`page`/`pageSize`) — consistent with the rest of the codebase's EF Core patterns.
`totalCount` is always returned (a second query) for "Page X of Y" UI. Every sort branch ends in
`.ThenBy(TutorUserId)`: without a deterministic tie-breaker, rows that tie on the primary sort
key (common — many documents can share a rating/price) have no guaranteed stable order, so
`Skip`/`Take` pagination can duplicate or skip rows across pages. Regression-tested in
`SearchEndpointsTests.SearchTutors_PaginationIsStable_AcrossPages_WhenManyRowsTieOnSortKey`.

## 7. States

| State | Trigger | Behavior |
|---|---|---|
| **Loading** | Query in flight | Skeleton cards, filter panel stays interactive |
| **Empty** | Zero matches | Explicit "no tutors match these filters" + suggestion to relax filters (no auto-relaxation) |
| **Page beyond results** | `totalCount > 0` but the requested page has zero items (stale bookmark, or a filter narrowed results after the page was set) | Frontend auto-corrects to the last valid page rather than showing the empty state — a distinct case from genuinely zero results (hardening-review fix) |
| **Invalid filter** | `priceMax < priceMin`, out-of-range `minRating`, `pageSize > 50`, malformed `countryOfBirthCode`/`nativeSpeakerOf` length | `400` with `ProblemDetails`-shaped validation errors — never silently corrected server-side. (Malformed-but-single-value cases like a negative price or an out-of-[0,5]-range rating are clamped to "no filter" client-side before the request is even sent — see `filterSerialization.ts` — a different tier from a genuine cross-field business-rule violation like `priceMax < priceMin`, which the server rejects.) |
| **Error** | Unexpected failure | Raw exception detail currently leaks (no `UseExceptionHandler`/`ProblemDetails` middleware wired anywhere in `TutorFlow.Host` — a pre-existing, cross-cutting gap affecting every module, not Search-specific; tracked as a new Architect item, not fixed here to avoid a one-off, inconsistent per-module workaround) |

## 8. What this spec does not decide

- Exact relevance-ranking formula beyond trigram-similarity-then-rating — tuning detail.
- Multi-subject search, saved searches, search-as-you-type — `P2`/`P3`.
- `HasUpcomingAvailability` lookback/lookahead window — `[OPEN — Product Owner]`.
- `specialtyIds` `AND` vs `OR` semantics — `[OPEN — Product Owner]`, `OPEN_QUESTIONS.md` #36.
- Professional Tutor / Super Tutor eligibility *computation* — explicit future work (ADR-0021 §2); only the badge *state* is implemented.
- The four undone producer pipelines (`Subjects`, `Availability`, `Reviews`, `Lessons`/`Bookings`) — designed, not built (`ADR-0021` §5).

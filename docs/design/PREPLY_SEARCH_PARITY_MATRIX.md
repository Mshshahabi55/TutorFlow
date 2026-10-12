# Preply Search Parity Matrix

Maps every capability confirmed (or partially confirmed) in `PREPLY_SEARCH_PARITY_SPEC.md` to its
TutorFlow implementation. "Status" is honest about what's wired end-to-end today versus what's
specified/designed but waiting on a producer event that doesn't exist yet (ADR-0021).

| Preply capability | TutorFlow requirement | Domain owner | Search field | API field | UI component | Status |
|---|---|---|---|---|---|---|
| "I want to learn" (subject-anchored entry) | Filter by subject | `Subjects` (catalog), `Tutors` (`SubjectIds`) | `SubjectIds: List<Guid>` | `subjectId` | `FiltersPanel` subject `Select` (options from `useSubjects()`) | **Implemented** (filter+index); **designed, not wired** (no real data until Tutors publishes `TutorPublished`) |
| Specialties (Business, exam prep, kids, etc.) | Filter by specialty, AND-matched | `Subjects` (catalog), `Tutors` (`SpecialtyIds`) | `SpecialtyIds: List<Guid>` | `specialtyIds` | `FiltersPanel` specialty `MultiSelect` | **Implemented**; AND-vs-OR semantics still `[OPEN — Product Owner]` (`OPEN_QUESTIONS.md` #36) |
| Price per lesson range | Filter by hourly price range | `Tutors` (`HourlyRate`) | `HourlyRateMinorUnits` | `priceMinMinorUnits`/`priceMaxMinorUnits` | `FiltersPanel` price inputs | **Implemented** end-to-end at the query level; **designed, not wired** for real data |
| Country of birth | Filter by tutor's country of birth | `Tutors` (`CountryOfBirthCode`) | `CountryOfBirthCode` | `countryOfBirthCode` | `FiltersPanel` country `Select` (static reference list, no backend catalog) | **Implemented** |
| "I'm available" (schedule) | Coarse "available soon" signal | `Availability` (future event) | `HasUpcomingAvailability` | `availableSoon` | `FiltersPanel` switch | **Implemented** (filter); exact window `[OPEN — Product Owner]`; **not wired** — `Availability` has no publisher yet |
| "Also speaks" (other languages) | Filter by any spoken language | `Tutors` (`TutorSpokenLanguage`, ADR-0020) | `SpokenLanguageCodes: List<string>` | `spokenLanguageCodes` | `FiltersPanel` languages `MultiSelect` | **Implemented** |
| Native speaker toggle | Filter by native-level language | `Tutors` (`TutorSpokenLanguage.Proficiency`) | `NativeLanguageCodes: List<string>` | `nativeSpeakerOf` | `FiltersPanel` native-speaker `Select` | **Implemented** |
| Professional Tutor badge/filter | Filter + display badge | `Tutors` (`IsProfessionalTutor`) | `IsProfessionalTutor` | `professionalTutor` | `FiltersPanel` switch, `TutorResultCard` badge | **Implemented** (storage/filter/display); eligibility *computation* (Preply's course-or-certificate criteria) is explicit future work, not built |
| Super Tutor badge/filter | Filter + display badge | `Tutors` (`IsSuperTutor`) | `IsSuperTutor` | `superTutor` | `FiltersPanel` switch, `TutorResultCard` badge | **Implemented** (storage/filter/display); the 9-metric rolling-90-day eligibility computation is explicit future work, not built (task instruction: don't implement ranking algorithms) |
| Name/keyword search | Free-text match on name/headline | `Search` (own read model) | `DisplayName`/`Headline` | `q` | `FiltersPanel` search box | **Implemented**, incl. pg_trgm-indexed `ILIKE` and trigram-similarity relevance ranking |
| Sort tutors by (default "Our top picks") | Relevance/rating/price/newest sort | `Search` | n/a (query-time) | `sort` | `SearchResultsList` sort `Select` | **Implemented** — `Relevance` ranks by trigram similarity when `q` is set, falls back to rating; all branches have a deterministic tie-breaker (hardening-review fix) |
| Rating + review count on card | Display | `Reviews` (future event) | `AverageRating`, `ReviewCount` | response fields | `Rating` component on `TutorResultCard` | **Implemented** (field+display); **not wired** — `Reviews` has no publisher yet |
| Price per lesson on card | Display | `Tutors` | `HourlyRateMinorUnits`/`Currency` | response fields | `TutorResultCard` price | **Implemented** |
| Active student / lesson count on card | Display lesson count | `Lessons`/`Bookings` (future event) | `LessonCount` | response field | `TutorResultCard` stats line | **Field exists, always 0** — no producer wired; active-student-count specifically not modeled at all (narrower than lesson count, not required for V1) |
| Spoken languages (native + secondary) on card | Display | `Tutors` | `SpokenLanguageCodes`/`NativeLanguageCodes` | response fields | `TutorResultCard` native-language badges | **Implemented** |
| Descriptor tags (e.g. "Engaging") | Display | — | — | — | — | **Not implemented** — Preply's own mechanism (self-selected vs. algorithmic) was unconfirmed by research; not invented here |
| Bio/headline, "Learn more" | Display | `Tutors` | `Headline` | response field | `TutorResultCard` headline | **Implemented** (headline only; full bio is a Tutor Profile page concern, out of this task's scope) |
| "Book trial lesson" CTA | Primary conversion action | `Bookings` (existing, `ADR-0013`/`ADR-0015`/`ADR-0019`) | — | — | `TutorResultCard` → `/tutors/{id}` (placeholder; real CTA lands with Tutor Profile) | **Deferred to Tutor Profile phase** (explicitly out of scope here) |
| "Send message" CTA | Secondary conversion action | `Messaging` (stub) | — | — | — | **Not implemented** — Messaging is still a stub, out of scope here |
| Favorite/heart icon | Save tutor to a list | `Students` (`FavoriteTutor`, stub) | — | — | — | **Not implemented** — explicitly excluded from this task ("Favorites backend") |
| Numbered pagination | Page through results | `Search` | n/a | `page`/`pageSize` | `Pagination` component | **Implemented**, incl. the stable-tie-break regression fix |
| Shareable/bookmarkable search URL | URL encodes full filter state | Frontend | — | — | `useSearchFilters` | **Implemented** — every filter above is URL-addressable; verified via Back/Forward + refresh tests |
| Mobile filter UX | Filter access on small screens | Frontend | — | — | `Drawer`-based filter sheet | **Implemented** — TutorFlow's own design (Preply's own mechanic unconfirmed, not copied) |

## Summary

- **Fully implemented, wired, and testable today**: subject/specialty/language/native-speaker/country-of-birth/price/rating-threshold/professional-tutor/super-tutor filtering, keyword search with real relevance ranking, sorting, pagination, URL state, mobile filters.
- **Implemented at the contract/filter level but returns no real data yet**: everything depending on a producer event that isn't wired (`Availability`→`HasUpcomingAvailability`, `Reviews`→rating/count, `Lessons`/`Bookings`→lesson count) — the *query* works correctly against an empty/zero-default dataset; it will start returning real values the moment those producers exist, with no API or frontend change required.
- **Deliberately not implemented** (explicitly out of this task's scope, per the task's own exclusion list): Tutor Profile, Booking/Checkout, Favorites, Messaging, descriptor tags (unconfirmed mechanic), badge *eligibility computation* (vs. the badge *state*, which is implemented).

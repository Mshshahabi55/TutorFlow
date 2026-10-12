# ADR-0020: Spoken Language Domain Ownership

**Status:** Accepted

## Context

While resolving the Preply-parity product/architecture gaps (2026-10-04), three distinct "language" concepts turned out to be conflated across existing documents, none of them previously flagged as a conflict:

1. **Taught-subject language** — e.g. "Spanish" as a `Subject` a tutor teaches. `DOMAIN_MODEL.md`'s `Subjects` row already lists a tentative `Language` entity alongside `Subject`/`Specialty`, described as "taxonomy (reference data)," and `NAVIGATION_AND_PAGE_MAP.md`'s Admin page inventory groups `Subjects`, `Languages`, `Specialties` together — both consistent with "Language" there meaning a curated reference list (for admin management and UI display), not a per-tutor attribute.
2. **Spoken/communication language** — e.g. "this tutor speaks English natively and French fluently," independent of what they teach. `TUTOR_JOURNEY.md`'s onboarding wizard lists step 4 "Languages" as distinct from step 5 "Subjects," implying this is a tutor *attribute*, not taxonomy — but no entity anywhere actually models it.
3. **UI-locale language** — which languages the TutorFlow interface itself is translated into. Already tracked separately as `OPEN_QUESTIONS.md` #4 and explicitly called out in `PRODUCT_VISION.md` as "not to be confused with tutoring subject languages." Unaffected by this ADR.

Concepts 1 and 2 were never disambiguated in `DOMAIN_MODEL.md`, which is a genuine gap, independently confirmed by the Preply UI/UX audit (`docs/design/TUTORFLOW_WIZARD_ARCHITECTURE.md` flags the same thing: "TutorFlow's current domain model does not yet have a dedicated place for 'spoken languages' as tutor attributes distinct from `Subjects`"). This ADR resolves concepts 1 and 2 only.

## Decision

### `Subjects` owns the language *reference list* (concept 1, unchanged in substance)

`Subjects` keeps `Language` as reference/taxonomy data — the admin-curated list of language codes TutorFlow recognizes, with localized display names, used to populate pickers across the product (onboarding wizard, search filters, student preferences). This matches `DOMAIN_MODEL.md`'s existing framing and the Admin page grouping; no change to that ownership.

The actual code values are **ISO 639-1** two-letter codes. Because this is a closed, internationally standardized, effectively-static code set — the same shape of problem as ISO 4217 currency codes, which `CLAUDE.md`/`ADR-0004` already treat as a shared primitive rather than owned reference data — `LanguageCode` (a validated ISO 639-1 string) is added to `TutorFlow.Shared` as a primitive, alongside the anticipated `Money` type. This is not business logic: it carries no behavior, only a validated shape. `Subjects`' admin-curated list controls which of the ~180 valid ISO codes are actually *enabled* for the product (display name, enabled/disabled) — it is additive curation on top of the shared primitive, not a redefinition of it.

### `Tutors` owns each tutor's own spoken languages (concept 2, new)

A new `TutorSpokenLanguage` entity, owned by `Tutors`, one row per `(TutorUserId, LanguageCode)`:

```csharp
public sealed class TutorSpokenLanguage
{
    public Guid TutorUserId { get; init; }
    public LanguageCode LanguageCode { get; init; } // TutorFlow.Shared primitive
    public LanguageProficiency Proficiency { get; init; }
}

public enum LanguageProficiency { Basic, Conversational, Fluent, Native }
```

This is populated by onboarding wizard step 4 ("Languages") and editable post-publish, same as any other `Tutors`-owned profile data. `LanguageCode` is a value, not a foreign key — `Tutors` never references `Subjects`' schema directly (`ADR-0001`). The wizard's language picker is populated by the frontend reading `Subjects`' own enabled-languages reference list directly; `Tutors` does not need to synchronously validate the code against `Subjects` at write time, because the code set itself is the shared, stable `TutorFlow.Shared` primitive — only which codes are *offered* in the picker depends on `Subjects`' curation, which is a UI concern, not a data-integrity one.

`Native` proficiency on a `TutorSpokenLanguage` row is what the "native speaker" filter (Preply-parity) matches against — no separate boolean flag is needed.

### Students — not modeled now

Nothing in the current product scope requires a student-side "spoken/preferred language" attribute (no journey document or audit finding calls for it). Not invented here. If a future requirement needs it, `Students` would own its own `StudentPreferredLanguage` using the same `LanguageCode` primitive, independently of `Tutors`' entity — never a shared table.

### `Search` projects spoken languages, doesn't own them

Per `ADR-0007`, `Search`'s `TutorSearchDocument` includes a denormalized `SpokenLanguages: (LanguageCode, Proficiency)[]` field, kept current via the same `Tutors`-published integration events the rest of the search index already depends on (event contract itself still designed at `Tutors` scaffold time, per `ADR-0007`'s existing deferral — not newly decided here).

## Consequences

- `DOMAIN_MODEL.md`'s `Subjects` and `Tutors` rows are updated to reflect this split.
- `TutorFlow.Shared` gains a `LanguageCode` primitive (ISO 639-1), alongside the already-anticipated `Money` type — both are primitives, not business logic, consistent with `CLAUDE.md`'s shared-kernel rule.
- No `ProjectReference` or cross-module FK is introduced; `Tutors` and `Subjects` each own their own data, linked only by a shared value type.
- `OPEN_QUESTIONS.md` #4's language *scope* (which UI locales ship in v1) is explicitly unaffected — that's concept 3, untouched by this ADR.
- This does not widen `ADR-0014`'s read-only query-contract pattern — no new cross-module query was needed to resolve this.

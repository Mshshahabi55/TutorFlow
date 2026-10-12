namespace TutorFlow.Shared;

/// <summary>
/// Published by <c>Tutors</c> whenever a <c>TutorApplication</c> reaches (or is updated while
/// in) <c>Published</c> status — the first real cross-module integration event in this codebase
/// (resolves `OPEN_QUESTIONS.md` A34/A19, ADR-0021). <c>Search</c> consumes it to upsert its own
/// <c>TutorSearchDocument</c> read model (ADR-0007); <c>Availability</c> also consumes it
/// (ADR-0011, wired via ADR-0024) to seed its own bookability read model and compute the first
/// <c>TutorAvailabilitySignalChanged</c> for the newly-published tutor.
///
/// Carries only IDs for Subjects/Specialties — <c>Tutors</c> itself only knows IDs after
/// ADR-0021 (it has no catalog display names to snapshot). Resolving those IDs to display names
/// is the `Subjects` catalog's own job; until `Subjects` publishes its own catalog-changed
/// events (documented, not yet implemented — `PREPLY_SEARCH_PARITY_MATRIX.md`), the frontend
/// resolves names itself by calling `Subjects`' public catalog endpoints directly and joining
/// client-side. Language codes are the one exception: the code itself is the stable identifier
/// (no catalog lookup needed to filter by it), so it travels here as-is.
/// </summary>
public sealed record TutorPublished(
    Guid TutorUserId,
    string Slug,
    string DisplayName,
    string Headline,
    string? PhotoUrl,
    IReadOnlyList<Guid> SubjectIds,
    IReadOnlyList<Guid> SpecialtyIds,
    IReadOnlyList<TutorPublishedSpokenLanguage> SpokenLanguages,
    string? CountryOfBirthCode,
    bool IsProfessionalTutor,
    bool IsSuperTutor,
    int HourlyRateAmountMinorUnits,
    string HourlyRateCurrency,
    DateTimeOffset PublishedAtUtc,
    DateTimeOffset OccurredAtUtc,
    bool IsAcceptingNewStudents = true) : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
}

public sealed record TutorPublishedSpokenLanguage(string LanguageCode, string Proficiency);

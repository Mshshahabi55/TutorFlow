using TutorFlow.Shared;

namespace TutorFlow.Modules.Subjects.Domain;

/// <summary>
/// The admin-curated, enabled-languages reference list (ADR-0020) — distinct from a tutor's own
/// spoken languages (<c>Tutors.TutorSpokenLanguage</c>), which reference this only by the shared
/// <see cref="TutorFlow.Shared.LanguageCode"/> primitive value, never a foreign key.
/// </summary>
public sealed class Language
{
    /// <summary>ISO 639-1 code (lowercase) — the primary key; this entity only curates which
    /// codes are enabled and their display name, it does not redefine the code space itself.</summary>
    public string Code { get; private init; } = "";

    public string DisplayName { get; private set; } = "";

    public bool IsActive { get; private set; } = true;

    public int SortOrder { get; private set; }

    private Language()
    {
    }

    public static Language Create(string code, string displayName, int sortOrder)
    {
        if (!LanguageCode.IsValid(code))
        {
            throw new ArgumentException("Must be a 2-letter ISO 639-1 code.", nameof(code));
        }

        return new Language
        {
            Code = LanguageCode.Normalize(code),
            DisplayName = displayName,
            SortOrder = sortOrder,
            IsActive = true,
        };
    }
}

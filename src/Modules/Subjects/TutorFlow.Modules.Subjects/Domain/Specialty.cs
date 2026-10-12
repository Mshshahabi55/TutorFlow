namespace TutorFlow.Modules.Subjects.Domain;

/// <summary>
/// A specialty within a subject (e.g. "Business English", "Exam Prep" under "English") — the
/// parent relationship Preply's own specialty list implies (`PREPLY_SEARCH_PARITY_SPEC.md`).
/// Same admin-curated/seeded data model as <see cref="Subject"/>.
/// </summary>
public sealed class Specialty
{
    public Guid Id { get; private init; }

    public Guid SubjectId { get; private init; }

    public string Code { get; private init; } = "";

    public string DisplayName { get; private set; } = "";

    public bool IsActive { get; private set; } = true;

    public int SortOrder { get; private set; }

    private Specialty()
    {
    }

    public static Specialty Create(Guid subjectId, string code, string displayName, int sortOrder) => new()
    {
        Id = Guid.NewGuid(),
        SubjectId = subjectId,
        Code = code,
        DisplayName = displayName,
        SortOrder = sortOrder,
        IsActive = true,
    };

    /// <summary>Fixed-ID construction for migration seed data only — see <see cref="Subject.Seed"/>.</summary>
    internal static Specialty Seed(Guid id, Guid subjectId, string code, string displayName, int sortOrder) => new()
    {
        Id = id,
        SubjectId = subjectId,
        Code = code,
        DisplayName = displayName,
        SortOrder = sortOrder,
        IsActive = true,
    };
}

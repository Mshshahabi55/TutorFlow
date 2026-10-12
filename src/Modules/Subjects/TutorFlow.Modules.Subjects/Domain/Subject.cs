namespace TutorFlow.Modules.Subjects.Domain;

/// <summary>
/// Reference/catalog entity — a subject a tutor can teach (e.g. "English", "Mathematics").
/// Admin-curated; no admin CRUD surface exists yet (Identity has no Admin role, A35) — V1
/// content is seeded via migration (see <c>Migrations/</c>), not a CMS. Stable <see cref="Id"/>
/// is the identity other modules reference (<c>Tutors.TutorApplication.SubjectId</c>,
/// <c>Search.TutorSearchDocument</c>) — never the display name.
/// </summary>
public sealed class Subject
{
    public Guid Id { get; private init; }

    /// <summary>Stable, locale-independent slug (e.g. "english") — the localization-ready seam;
    /// <see cref="DisplayName"/> is the only field that would vary per UI locale in the future.</summary>
    public string Code { get; private init; } = "";

    public string DisplayName { get; private set; } = "";

    public bool IsActive { get; private set; } = true;

    public int SortOrder { get; private set; }

    private Subject()
    {
    }

    public static Subject Create(string code, string displayName, int sortOrder) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        DisplayName = displayName,
        SortOrder = sortOrder,
        IsActive = true,
    };

    /// <summary>Fixed-ID construction for migration seed data only — <see cref="Create"/>'s
    /// random ID is wrong for seeding (EF needs a stable key across migration regenerations).</summary>
    internal static Subject Seed(Guid id, string code, string displayName, int sortOrder) => new()
    {
        Id = id,
        Code = code,
        DisplayName = displayName,
        SortOrder = sortOrder,
        IsActive = true,
    };
}

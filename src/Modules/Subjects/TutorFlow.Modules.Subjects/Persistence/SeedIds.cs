namespace TutorFlow.Modules.Subjects.Persistence;

/// <summary>
/// Fixed GUIDs for migration-seeded reference data (<c>Subject</c>/<c>Specialty</c> rows) — not
/// business/user data, a development/V1 starter catalog (no admin CRUD exists yet, A35). Stable
/// across migration regenerations by construction (literal values, not <c>Guid.NewGuid()</c>).
/// </summary>
internal static class SeedIds
{
    public static readonly Guid English = new("00000000-0000-0000-0001-000000000001");
    public static readonly Guid Spanish = new("00000000-0000-0000-0001-000000000002");
    public static readonly Guid French = new("00000000-0000-0000-0001-000000000003");
    public static readonly Guid German = new("00000000-0000-0000-0001-000000000004");
    public static readonly Guid Mathematics = new("00000000-0000-0000-0001-000000000005");

    public static readonly Guid EnglishConversation = new("00000000-0000-0000-0002-000000000001");
    public static readonly Guid EnglishExamPrep = new("00000000-0000-0000-0002-000000000002");
    public static readonly Guid EnglishBusiness = new("00000000-0000-0000-0002-000000000003");
    public static readonly Guid EnglishKidsAndTeens = new("00000000-0000-0000-0002-000000000004");
    public static readonly Guid SpanishConversation = new("00000000-0000-0000-0002-000000000005");
    public static readonly Guid MathExamPrep = new("00000000-0000-0000-0002-000000000006");
}
